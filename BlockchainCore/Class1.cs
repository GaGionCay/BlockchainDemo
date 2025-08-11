using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace BlockchainCore
{
    public class Block
    {
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public string Data { get; set; } = string.Empty;
        public string PreviousHash { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public int Nonce { get; set; }
    }

    public class BlockchainService
    {
        public List<Block> Chain { get; set; } = new List<Block>();
        private int _difficulty = 4;

        public BlockchainService()
        {
        }

        public Block GetLatestBlock()
        {
            if (Chain.Count == 0)
            {
                throw new InvalidOperationException("Chain is empty.");
            }
            return Chain.Last();
        }

        public Block AddBlock(string data)
        {
            Block newBlock;
            if (Chain.Count == 0)
            {
                newBlock = new Block
                {
                    Index = 0,
                    Timestamp = DateTime.UtcNow,
                    Data = data,
                    PreviousHash = "0",
                };
            }
            else
            {
                var latestBlock = GetLatestBlock();
                newBlock = new Block
                {
                    Index = latestBlock.Index + 1,
                    Timestamp = DateTime.UtcNow,
                    Data = data,
                    PreviousHash = latestBlock.Hash
                };
            }

            newBlock.Hash = MineBlock(newBlock);
            Chain.Add(newBlock);
            return newBlock;
        }

        private string MineBlock(Block block)
        {
            var difficultyString = new string('0', _difficulty);
            while (true)
            {
                var hash = CalculateHash(block.Index, block.Timestamp, block.Data, block.PreviousHash, block.Nonce);
                if (hash.StartsWith(difficultyString))
                {
                    block.Hash = hash;
                    return hash;
                }
                block.Nonce++;
            }
        }

        public string CalculateHash(int index, DateTime timestamp, string data, string previousHash, int nonce)
        {
            string rawData = $"{index}-{timestamp.ToString("o")}-{data}-{previousHash}-{nonce}";
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public bool AddBlockFromPeer(Block newBlock)
        {
            if (Chain.Any(b => b.Hash == newBlock.Hash))
            {
                Console.WriteLine("--> Block đã tồn tại trong chuỗi, không cần thêm.");
                return false;
            }

            if (IsValidNewBlock(newBlock))
            {
                Chain.Add(newBlock);
                return true;
            }
            return false;
        }

        public bool IsValidNewBlock(Block newBlock)
        {
            try
            {
                var latestBlock = GetLatestBlock();
                if (latestBlock.Index + 1 != newBlock.Index)
                {
                    return false;
                }
                if (latestBlock.Hash != newBlock.PreviousHash)
                {
                    return false;
                }
                if (CalculateHash(newBlock.Index, newBlock.Timestamp, newBlock.Data, newBlock.PreviousHash, newBlock.Nonce) != newBlock.Hash)
                {
                    return false;
                }
                return true;
            }
            catch (InvalidOperationException)
            {
                if (newBlock.Index == 0 && newBlock.PreviousHash == "0")
                {
                    return true;
                }
                return false;
            }
        }

        public void ReplaceChain(List<Block> newChain)
        {
            if (newChain.Count > Chain.Count && IsValidChain(newChain))
            {
                Chain = newChain;
            }
        }

        public bool IsValidChain(List<Block> chain)
        {
            if (chain.Count == 0) return false;
            if (chain[0].PreviousHash != "0") return false;

            for (int i = 1; i < chain.Count; i++)
            {
                var currentBlock = chain[i];
                var previousBlock = chain[i - 1];

                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    return false;
                }
                if (CalculateHash(currentBlock.Index, currentBlock.Timestamp, currentBlock.Data, currentBlock.PreviousHash, currentBlock.Nonce) != currentBlock.Hash)
                {
                    return false;
                }
            }
            return true;
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

        public P2PNode(BlockchainService blockchain, string nodeName)
        {
            _blockchain = blockchain;
            _nodeName = nodeName;
        }

        public void Start(int port, List<string> seedNodes)
        {
            _server = new TcpListener(IPAddress.Any, port);
            _server.Start();
            Console.WriteLine($"Node listening on port {port}...");

            Task.Run(() => ListenForConnections());
            Task.Run(() => HandlePeerMessages());

            if (seedNodes.Count == 0 && _blockchain.Chain.Count == 0)
            {
                _blockchain.AddBlock("Genesis Block");
                Console.WriteLine($"Node {_nodeName} đã tạo block gốc.");
            }

            ConnectToSeedNodes(seedNodes);
        }

        private void ListenForConnections()
        {
            while (true)
            {
                var client = _server.AcceptTcpClient();
                lock (_peersLock)
                {
                    _peers.Add(client);
                }
                Console.WriteLine($"New peer connected from {((IPEndPoint)client.Client.RemoteEndPoint).Address}!");
            }
        }

        private void ConnectToSeedNodes(List<string> seedNodes)
        {
            foreach (var seed in seedNodes)
            {
                try
                {
                    var parts = seed.Split(':');
                    var client = new TcpClient();
                    client.Connect(parts[0], int.Parse(parts[1]));
                    lock (_peersLock)
                    {
                        _peers.Add(client);
                    }
                    Console.WriteLine($"Connected to seed node {seed}!");

                    var syncMessage = new Message { Type = "SYNC_CHAIN", Data = "", Sender = _nodeName };
                    SendMessage(client, syncMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not connect to seed node {seed}: {ex.Message}");
                }
            }
        }

        private void HandlePeerMessages()
        {
            while (true)
            {
                List<TcpClient> disconnectedPeers = new List<TcpClient>();
                List<TcpClient> peersToProcess;
                lock (_peersLock)
                {
                    peersToProcess = new List<TcpClient>(_peers);
                }

                foreach (var peer in peersToProcess)
                {
                    try
                    {
                        if (peer.Available > 0)
                        {
                            byte[] buffer = new byte[peer.Available];
                            peer.GetStream().Read(buffer, 0, buffer.Length);
                            var messageJson = Encoding.UTF8.GetString(buffer);
                            var message = JsonSerializer.Deserialize<Message>(messageJson);
                            ProcessMessage(peer, message);
                        }
                    }
                    catch
                    {
                        disconnectedPeers.Add(peer);
                    }
                }

                if (disconnectedPeers.Any())
                {
                    lock (_peersLock)
                    {
                        foreach (var peer in disconnectedPeers)
                        {
                            _peers.Remove(peer);
                            Console.WriteLine($"Peer disconnected.");
                        }
                    }
                }
            }
        }

        private void ProcessMessage(TcpClient sender, Message? message)
        {
            if (message == null) return;

            // Generate a unique message ID to prevent reprocessing
            var messageId = $"{message.Type}-{message.Data}-{message.Sender}";
            if (_processedMessageIds.Contains(messageId))
            {
                Console.WriteLine($"--> Thông điệp đã được xử lý trước đó, bỏ qua.");
                return;
            }
            _processedMessageIds.Add(messageId);

            switch (message.Type)
            {
                case "NEW_BLOCK":
                    var newBlock = JsonSerializer.Deserialize<Block>(message.Data);
                    Console.WriteLine($"--> Đã nhận block mới từ {message.Sender}. Đang xác thực...");

                    try
                    {
                        if (newBlock.Index > _blockchain.GetLatestBlock().Index)
                        {
                            if (_blockchain.AddBlockFromPeer(newBlock))
                            {
                                Console.WriteLine($"--> Block từ {message.Sender} đã được xác thực và thêm thành công vào chuỗi!");
                                // Broadcast block đã nhận tới các peer khác để đảm bảo sự đồng bộ
                                BroadcastBlock(newBlock, _nodeName);
                            }
                            else
                            {
                                Console.WriteLine($"--> Block từ {message.Sender} không hợp lệ và đã bị từ chối.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"--> Block từ {message.Sender} đã cũ hoặc đã tồn tại. Bỏ qua.");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        if (newBlock.Index == 0)
                        {
                            if (_blockchain.AddBlockFromPeer(newBlock))
                            {
                                Console.WriteLine($"--> Block từ {message.Sender} đã được xác thực và thêm thành công vào chuỗi!");
                            }
                        }
                    }

                    break;
                case "SYNC_CHAIN":
                    var serializedChain = JsonSerializer.Serialize(_blockchain.Chain);
                    var syncResponse = new Message { Type = "CHAIN_RESPONSE", Data = serializedChain, Sender = _nodeName };
                    SendMessage(sender, syncResponse);
                    break;
                case "CHAIN_RESPONSE":
                    var peerChain = JsonSerializer.Deserialize<List<Block>>(message.Data);
                    if (peerChain.Count > _blockchain.Chain.Count)
                    {
                        _blockchain.ReplaceChain(peerChain);
                        Console.WriteLine($"--> Chuỗi blockchain của tôi đã được cập nhật từ {message.Sender}!");
                    }
                    else if (peerChain.Count < _blockchain.Chain.Count)
                    {
                        Console.WriteLine($"--> Chuỗi của {message.Sender} ngắn hơn, không cập nhật.");
                    }
                    break;
                default:
                    Console.WriteLine($"--> Received unknown message type: {message.Type}");
                    break;
            }
        }

        private void SendMessage(TcpClient peer, Message message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            peer.GetStream().Write(messageBytes, 0, messageBytes.Length);
        }

        public void BroadcastBlock(Block block, string senderName)
        {
            var newBlockMessage = new Message { Type = "NEW_BLOCK", Data = JsonSerializer.Serialize(block), Sender = senderName };
            lock (_peersLock)
            {
                foreach (var peer in _peers)
                {
                    SendMessage(peer, newBlockMessage);
                }
            }
        }
    }
}
