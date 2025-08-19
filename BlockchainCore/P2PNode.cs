using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using BlockchainCore.Models;

namespace BlockchainCore
{
    public class P2PNode
    {
        private TcpListener? _server;
        private List<TcpClient> _peers = new List<TcpClient>();
        private BlockchainCore _blockchain;
        private readonly object _peersLock = new object();
        private readonly string _nodeName;
        private HashSet<string> _processedMessageIds = new HashSet<string>();
        private readonly List<string> _seedNodes;
        private ConcurrentQueue<Tuple<TcpClient, Message>> _messageQueue = new ConcurrentQueue<Tuple<TcpClient, Message>>();

        public P2PNode(BlockchainCore blockchain, string nodeName, List<string> seedNodes)
        {
            _blockchain = blockchain;
            _nodeName = nodeName;
            _seedNodes = seedNodes;
        }

        public void Start(int port)
        {
            _server = new TcpListener(IPAddress.Any, port);
            _server.Start();
            Logger.LogInfo($"Node listening on port {port}...");

            Task.Run(() => ListenForConnections());
            Task.Run(() => HandlePeerMessages());
            Task.Run(() => MonitorPeersAndReconnect()); 
            Task.Run(() => StartMiningLoop());

            if (_seedNodes.Count == 0 && _blockchain.Chain.Count <= 1)
            {
                _blockchain.CreateGenesisBlock();
            }

            ConnectToSeedNodes();
        }

        private void ListenForConnections()
        {
            while (true)
            {
                try
                {
                    var client = _server!.AcceptTcpClient();
                    lock (_peersLock)
                    {
                        if (!_peers.Any(p => p.Client.RemoteEndPoint!.ToString() == client.Client.RemoteEndPoint!.ToString()))
                        {
                            _peers.Add(client);
                            Logger.LogInfo($"New peer connected from {((IPEndPoint)client.Client.RemoteEndPoint).Address}!");
                            Task.Run(() => ListenForMessagesFromPeer(client));
                        }
                        else
                        {
                            client.Close();
                        }
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Lỗi khi chấp nhận kết nối: {ex.Message}");
                }
            }
        }

        private void ListenForMessagesFromPeer(TcpClient peer)
        {
            var stream = peer.GetStream();
            while (peer.Connected)
            {
                try
                {
                    var lengthBuffer = new byte[4];
                    int bytesRead = stream.Read(lengthBuffer, 0, 4);
                    if (bytesRead == 0) break;
                    if (bytesRead != 4)
                    {
                        Logger.LogWarning("Kích thước header không hợp lệ.");
                        continue;
                    }

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    var messageBuffer = new byte[messageLength];
                    int totalBytesRead = 0;
                    while (totalBytesRead < messageLength)
                    {
                        bytesRead = stream.Read(messageBuffer, totalBytesRead, messageLength - totalBytesRead);
                        if (bytesRead == 0) break;
                        totalBytesRead += bytesRead;
                    }
                    if (totalBytesRead != messageLength) continue;

                    var messageJson = Encoding.UTF8.GetString(messageBuffer);
                    var message = JsonSerializer.Deserialize<Message>(messageJson);
                    if (message != null)
                    {
                        _messageQueue.Enqueue(new Tuple<TcpClient, Message>(peer, message));
                    }
                }
                catch (IOException ex) when (ex.InnerException is SocketException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Lỗi khi đọc tin nhắn từ peer: {ex.Message}");
                    break;
                }
            }
            lock (_peersLock)
            {
                int index = _peers.FindIndex(p => p == peer);
                if (index != -1)
                {
                    _peers.RemoveAt(index);
                }
                Logger.LogInfo($"Peer disconnected. Tổng số peer: {_peers.Count}");
            }
            peer.Close();
        }

        private void ConnectToSeedNodes()
        {
            foreach (var seed in _seedNodes)
            {
                Task.Run(() => ConnectToPeer(seed));
            }
        }

        private void ConnectToPeer(string peerAddress)
        {
            try
            {
                var parts = peerAddress.Split(':');
                var client = new TcpClient();
                client.Connect(parts[0], int.Parse(parts[1]));
                lock (_peersLock)
                {
                    if (!_peers.Any(p => p.Client.RemoteEndPoint!.ToString().Contains(peerAddress)))
                    {
                        _peers.Add(client);
                        Logger.LogInfo($"Connected to seed node {peerAddress}!");
                        Task.Run(() => ListenForMessagesFromPeer(client));

                        var syncMessage = new Message { Type = "SYNC_CHAIN", Data = "", Sender = _nodeName };
                        SendMessage(client, syncMessage);
                    }
                    else
                    {
                        client.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Không thể kết nối tới seed node {peerAddress}: {ex.Message}");
            }
        }

        private void HandlePeerMessages()
        {
            while (true)
            {
                if (_messageQueue.TryDequeue(out var messageTuple))
                {
                    var sender = messageTuple.Item1;
                    var message = messageTuple.Item2;
                    ProcessMessage(sender, message);
                }
                else
                {
                    if (_processedMessageIds.Count > 1000)
                    {
                        _processedMessageIds.Clear();
                    }
                    Thread.Sleep(10);
                }
            }
        }

        private void MonitorPeersAndReconnect()
        {
            while (true)
            {
                Thread.Sleep(5000);
                lock (_peersLock)
                {
                    foreach (var seed in _seedNodes)
                    {
                        var parts = seed.Split(':');
                        if (!_peers.Any(p =>
                        {
                            if (p.Client.RemoteEndPoint is IPEndPoint ipEndPoint)
                            {
                                return ipEndPoint.Address.ToString() == parts[0] && ipEndPoint.Port == int.Parse(parts[1]);
                            }
                            return false;
                        }))
                        {
                            Logger.LogDebug($"Cố gắng kết nối lại tới seed node {seed}...");
                            ConnectToPeer(seed);
                        }
                    }
                }
            }
        }

        private void ProcessMessage(TcpClient sender, Message? message)
        {
            if (message == null) return;
            if (message.Sender == _nodeName) return;

            var messageId = $"{message.Type}-{message.Data}-{message.Sender}";
            if (_processedMessageIds.Contains(messageId))
            {
                Logger.LogDebug("Thông điệp đã được xử lý trước đó, bỏ qua.");
                return;
            }
            _processedMessageIds.Add(messageId);

            switch (message.Type)
            {
                case "NEW_BLOCK":
                    var newBlock = JsonSerializer.Deserialize<Block>(message.Data);
                    Logger.LogInfo($"Đã nhận block mới từ {message.Sender}.");

                    if (_blockchain.AddBlockFromPeer(newBlock))
                    {
                        Logger.LogInfo($"Block từ {message.Sender} đã được xác thực và thêm thành công!");
                        BroadcastBlock(newBlock, sender);
                    }
                    else
                    {
                        Logger.LogWarning($"Block từ {message.Sender} không hợp lệ và bị từ chối. Yêu cầu đồng bộ chuỗi...");
                        var syncMessage = new Message { Type = "SYNC_CHAIN", Data = "", Sender = _nodeName };
                        SendMessage(sender, syncMessage);
                    }
                    break;
                case "NEW_TRANSACTION":
                    var newTransaction = JsonSerializer.Deserialize<Transaction>(message.Data);
                    _blockchain.AddTransactionToPending(newTransaction);
                    BroadcastTransaction(newTransaction, sender);
                    break;

                case "SYNC_CHAIN":
                    var serializedChain = JsonSerializer.Serialize(_blockchain.Chain);
                    var syncResponse = new Message { Type = "CHAIN_RESPONSE", Data = serializedChain, Sender = _nodeName };
                    SendMessage(sender, syncResponse);
                    break;

                case "CHAIN_RESPONSE":
                    var peerChain = JsonSerializer.Deserialize<List<Block>>(message.Data);
                    if (peerChain != null && _blockchain.ReplaceChain(peerChain))
                    {
                        Logger.LogInfo($"Chuỗi blockchain của tôi đã được cập nhật từ {message.Sender}!");
                    }
                    break;

                default:
                    Logger.LogDebug($"Received unknown message type: {message.Type}");
                    break;
            }
        }

        private void SendMessage(TcpClient peer, Message message)
        {
            try
            {
                if (!peer.Connected)
                {
                    Logger.LogWarning("Peer is not connected, cannot send message.");
                    return;
                }

                var messageJson = JsonSerializer.Serialize(message);
                var messageBytes = Encoding.UTF8.GetBytes(messageJson);
                var lengthBytes = BitConverter.GetBytes(messageBytes.Length);

                var stream = peer.GetStream();
                stream.Write(lengthBytes, 0, 4);
                stream.Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Lỗi khi gửi tin nhắn loại '{message.Type}' tới peer: {ex.Message}");
            }
        }

        public void BroadcastBlock(Block block, TcpClient? excludePeer = null)
        {
            var newBlockMessage = new Message { Type = "NEW_BLOCK", Data = JsonSerializer.Serialize(block), Sender = _nodeName };
            Logger.LogInfo("Đang phát tán block mới tới các peer...");
            lock (_peersLock)
            {
                foreach (var peer in _peers)
                {
                    if (excludePeer == null || peer.Client.RemoteEndPoint!.ToString() != excludePeer.Client.RemoteEndPoint!.ToString())
                    {
                        SendMessage(peer, newBlockMessage);
                    }
                }
            }
        }

        public void BroadcastTransaction(Transaction transaction, TcpClient? sender)
        {
            var newTransactionMessage = new Message { Type = "NEW_TRANSACTION", Data = JsonSerializer.Serialize(transaction), Sender = _nodeName };
            Logger.LogDebug($"Đang phát tán giao dịch ID {transaction.TransactionId} tới các peer...");
            lock (_peersLock)
            {
                foreach (var peer in _peers)
                {
                    if (sender == null || peer.Client.RemoteEndPoint!.ToString() != sender.Client.RemoteEndPoint!.ToString())
                    {
                        SendMessage(peer, newTransactionMessage);
                    }
                }
            }
        }

        public void CreateAndBroadcastTransaction(Transaction transaction)
        {
            _blockchain.AddTransactionToPending(transaction);
            BroadcastTransaction(transaction, null);
        } 
        private void StartMiningLoop()
        {
            Logger.LogInfo("Vòng lặp đào tự động đã được bắt đầu.");
            while (true)
            { 
                if (_blockchain.PendingTransactions.Any())
                {
                    Logger.LogInfo($"Phát hiện có {_blockchain.PendingTransactions.Count} giao dịch đang chờ. Bắt đầu đào block mới...");
                     
                    Block? newBlock = _blockchain.MinePendingTransactions();

                    if (newBlock != null)
                    {
                        Logger.LogInfo($"Đào thành công block #{newBlock.Index}. Đang phát tán tới mạng lưới..."); 
                        BroadcastBlock(newBlock);
                    }
                } 
                Thread.Sleep(10000);  
            }
        }
    }
}
