using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;

namespace BlockchainCore
{
    /// <summary>
    /// Lớp tiện ích để quản lý việc ghi log.
    /// Có thể bật hoặc tắt các loại thông báo khác nhau.
    /// </summary>
    public static class Logger
    {
        // Điều khiển việc hiển thị các thông báo cấp độ thông tin
        public static bool EnableInfoLogs = true;
        // Điều khiển việc hiển thị các thông báo cấp độ gỡ lỗi (debug)
        public static bool EnableDebugLogs = false;

        public static void LogInfo(string message)
        {
            if (EnableInfoLogs)
            {
                Console.WriteLine($"[INFO] {message}");
            }
        }

        public static void LogWarning(string message)
        {
            Console.WriteLine($"[WARNING] {message}");
        }

        public static void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
            {
                Console.WriteLine($"[ERROR] {message} - {ex.Message}");
            }
            else
            {
                Console.WriteLine($"[ERROR] {message}");
            }
        }

        public static void LogDebug(string message)
        {
            if (EnableDebugLogs)
            {
                Console.WriteLine($"[DEBUG] {message}");
            }
        }
    }

    // Lớp đại diện cho một giao dịch trong blockchain.
    public class Transaction
    {
        // Địa chỉ công khai của người gửi.
        public string FromAddress { get; set; } = string.Empty;

        // Địa chỉ công khai của người nhận.
        public string ToAddress { get; set; } = string.Empty;

        // Mã định danh duy nhất cho giao dịch.
        public string TransactionId { get; set; } = Guid.NewGuid().ToString();

        // Dữ liệu của giao dịch, được lưu dưới dạng chuỗi serialized JSON.
        public string Data { get; set; } = string.Empty;

        // Chữ ký điện tử của người gửi, dùng để xác minh tính toàn vẹn của dữ liệu.
        public string Signature { get; set; } = string.Empty;

        // Thời điểm giao dịch được tạo.
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Lớp Block được cập nhật để chứa danh sách các giao dịch.
    public class Block
    {
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public string PreviousHash { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public int Nonce { get; set; }
    }

    // Lớp BlockchainService quản lý chuỗi blockchain và các giao dịch.
    public class BlockchainService
    {
        public List<Block> Chain { get; set; } = new List<Block>();
        public List<Transaction> PendingTransactions { get; set; } = new List<Transaction>();
        private int _difficulty = 4;
        private readonly object _blockchainLock = new object();

        public BlockchainService()
        {
            CreateGenesisBlock();
        }

        public void CreateGenesisBlock()
        {
            lock (_blockchainLock)
            {
                if (Chain.Count == 0)
                {
                    Block genesisBlock = new Block
                    {
                        Index = 0,
                        Timestamp = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        Transactions = new List<Transaction>(),
                        PreviousHash = "0",
                        Nonce = 0
                    };
                    genesisBlock.Hash = CalculateHash(genesisBlock);
                    Chain.Add(genesisBlock);
                    Logger.LogInfo("Đã tạo block gốc.");
                }
            }
        }

        public Block? GetLatestBlock()
        {
            lock (_blockchainLock)
            {
                return Chain.LastOrDefault();
            }
        }

        public void AddTransactionToPending(Transaction transaction)
        {
            lock (_blockchainLock)
            {
                if (string.IsNullOrEmpty(transaction.FromAddress) || string.IsNullOrEmpty(transaction.Signature) || string.IsNullOrEmpty(transaction.Data))
                {
                    Logger.LogWarning("Lỗi: Giao dịch không hợp lệ, thiếu thông tin.");
                    return;
                }

                if (PendingTransactions.Any(t => t.TransactionId == transaction.TransactionId) ||
                    Chain.Any(b => b.Transactions.Any(t => t.TransactionId == transaction.TransactionId)))
                {
                    Logger.LogDebug($"Giao dịch ID {transaction.TransactionId} đã tồn tại. Bỏ qua.");
                    return;
                }

                PendingTransactions.Add(transaction);
                Logger.LogInfo($"Giao dịch mới đã được thêm vào danh sách chờ: ID {transaction.TransactionId}");
            }
        }

        public Block? MinePendingTransactions()
        {
            lock (_blockchainLock)
            {
                if (PendingTransactions.Count == 0)
                {
                    Logger.LogWarning("Không có giao dịch đang chờ xử lý để khai thác.");
                    return null;
                }

                var latestBlock = GetLatestBlock();
                if (latestBlock == null)
                {
                    Logger.LogError("Không tìm thấy block cuối cùng để khai thác.");
                    return null;
                }

                var newBlock = new Block
                {
                    Index = latestBlock.Index + 1,
                    Timestamp = DateTime.UtcNow,
                    Transactions = new List<Transaction>(PendingTransactions),
                    PreviousHash = latestBlock.Hash,
                    Nonce = 0
                };

                Logger.LogInfo("Bắt đầu khai thác block mới...");
                newBlock.Hash = MineBlock(newBlock);
                Logger.LogInfo($"Block mới đã được khai thác thành công! Hash: {newBlock.Hash}");

                Chain.Add(newBlock);
                PendingTransactions.Clear();
                return newBlock;
            }
        }

        private string MineBlock(Block block)
        {
            var difficultyString = new string('0', _difficulty);
            while (true)
            {
                var hash = CalculateHash(block);
                if (hash.StartsWith(difficultyString))
                {
                    block.Hash = hash;
                    return hash;
                }
                block.Nonce++;
            }
        }

        public string CalculateHash(Block block)
        {
            var sortedTransactions = block.Transactions.OrderBy(t => t.TransactionId).ToList();
            string rawData = $"{block.Index}-{block.Timestamp.ToString("o")}-{JsonSerializer.Serialize(sortedTransactions)}-{block.PreviousHash}-{block.Nonce}";
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public bool AddBlockFromPeer(Block? newBlock)
        {
            lock (_blockchainLock)
            {
                if (newBlock == null)
                {
                    Logger.LogError("Block mới nhận được là null và không thể thêm vào chuỗi.");
                    return false;
                }

                var latestBlock = GetLatestBlock();
                if (latestBlock == null)
                {
                    if (newBlock.Index == 0 && newBlock.PreviousHash == "0" && IsValidBlock(newBlock))
                    {
                        Chain.Add(newBlock);
                        RemoveTransactionsFromPending(newBlock.Transactions);
                        return true;
                    }
                    Logger.LogError("Chuỗi hiện tại rỗng và block mới không phải là block gốc hợp lệ.");
                    return false;
                }

                if (Chain.Any(b => b.Hash == newBlock.Hash))
                {
                    Logger.LogDebug("Block đã tồn tại trong chuỗi, không cần thêm.");
                    return false;
                }

                if (newBlock.PreviousHash == latestBlock.Hash && IsValidBlock(newBlock))
                {
                    Chain.Add(newBlock);
                    RemoveTransactionsFromPending(newBlock.Transactions);
                    return true;
                }
                else
                {
                    Logger.LogWarning($"Block mới không hợp lệ hoặc không phải block tiếp theo. Index: {newBlock.Index}, PreviousHash: {newBlock.PreviousHash}");
                    return false;
                }
            }
        }

        private void RemoveTransactionsFromPending(List<Transaction> transactionsInBlock)
        {
            lock (_blockchainLock)
            {
                var transactionIdsInBlock = new HashSet<string>(transactionsInBlock.Select(t => t.TransactionId));
                PendingTransactions.RemoveAll(t => transactionIdsInBlock.Contains(t.TransactionId));
            }
        }

        public bool IsValidBlock(Block block)
        {
            try
            {
                var calculatedHash = CalculateHash(block);
                if (calculatedHash != block.Hash)
                {
                    Logger.LogWarning($"Hash của block không hợp lệ. Tính toán: {calculatedHash}, Block: {block.Hash}");
                    return false;
                }
                var difficultyString = new string('0', _difficulty);
                if (!block.Hash.StartsWith(difficultyString))
                {
                    Logger.LogWarning("Proof of Work không hợp lệ.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Lỗi khi kiểm tra tính hợp lệ của block: {ex.Message}");
                return false;
            }
        }

        public bool IsValidChain(List<Block> chain)
        {
            if (chain == null || chain.Count == 0) return false;

            var genesisBlock = chain[0];
            if (genesisBlock.Index != 0 || genesisBlock.PreviousHash != "0" || CalculateHash(genesisBlock) != genesisBlock.Hash)
            {
                Logger.LogWarning("Block gốc của chuỗi không hợp lệ.");
                return false;
            }

            for (int i = 1; i < chain.Count; i++)
            {
                Block currentBlock = chain[i];
                Block previousBlock = chain[i - 1];

                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    Logger.LogWarning($"PreviousHash của block #{i} không khớp với hash của block trước đó.");
                    return false;
                }

                if (!IsValidBlock(currentBlock))
                {
                    Logger.LogWarning($"Block #{i} không hợp lệ.");
                    return false;
                }
            }
            return true;
        }

        public bool IsChainValid()
        {
            return IsValidChain(this.Chain);
        }

        public void CheckChainAndNotify()
        {
            lock (_blockchainLock)
            {
                Logger.LogInfo("Đang kiểm tra tính hợp lệ của chuỗi blockchain...");
                if (IsValidChain(this.Chain))
                {
                    Logger.LogInfo("Chuỗi blockchain hợp lệ và toàn vẹn!");
                }
                else
                {
                    Logger.LogWarning("CẢNH BÁO: Chuỗi blockchain không hợp lệ!");
                }
            }
        }

        public bool ReplaceChain(List<Block> newChain)
        {
            lock (_blockchainLock)
            {
                if (newChain.Count > Chain.Count && IsValidChain(newChain))
                {
                    Chain = newChain;
                    var newChainTransactionIds = new HashSet<string>(newChain.SelectMany(b => b.Transactions.Select(t => t.TransactionId)));
                    PendingTransactions.RemoveAll(t => newChainTransactionIds.Contains(t.TransactionId));

                    Logger.LogInfo("Chuỗi blockchain của tôi đã được cập nhật thành công!");
                    return true;
                }
                else
                {
                    Logger.LogDebug("Chuỗi mới không dài hơn hoặc không hợp lệ. Không thay thế.");
                    return false;
                }
            }
        }
    }

    public class Message
    {
        public string Type { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
    }

    public class P2PNode
    {
        private TcpListener? _server;
        private List<TcpClient> _peers = new List<TcpClient>();
        private BlockchainService _blockchain;
        private readonly object _peersLock = new object();
        private readonly string _nodeName;
        private HashSet<string> _processedMessageIds = new HashSet<string>();
        private readonly List<string> _seedNodes;
        private ConcurrentQueue<Tuple<TcpClient, Message>> _messageQueue = new ConcurrentQueue<Tuple<TcpClient, Message>>();

        public P2PNode(BlockchainService blockchain, string nodeName, List<string> seedNodes)
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
    }
}
