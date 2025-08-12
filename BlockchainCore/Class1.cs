using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace BlockchainCore
{
    // Lớp đại diện cho một giao dịch trong blockchain.
    public class Transaction
    {
        public string Sender { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
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

        public BlockchainService()
        {
            // Tạo block gốc khi khởi tạo chuỗi.
            CreateGenesisBlock();
        }

        public void CreateGenesisBlock()
        {
            if (Chain.Count == 0)
            {
                // Sử dụng các giá trị tĩnh để đảm bảo block gốc là giống hệt nhau trên mọi node
                Block genesisBlock = new Block
                {
                    Index = 0,
                    Timestamp = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Transactions = new List<Transaction>(),
                    PreviousHash = "0",
                    Nonce = 0
                };
                genesisBlock.Hash = CalculateHash(genesisBlock.Index, genesisBlock.Timestamp, genesisBlock.Transactions, genesisBlock.PreviousHash, genesisBlock.Nonce);
                Chain.Add(genesisBlock);
            }
        }

        public Block GetLatestBlock()
        {
            if (Chain.Count == 0)
            {
                throw new InvalidOperationException("Chain is empty.");
            }
            return Chain.Last();
        }

        // Phương thức để thêm một giao dịch mới vào danh sách chờ.
        public void AddTransactionToPending(Transaction transaction)
        {
            if (string.IsNullOrEmpty(transaction.Sender) || string.IsNullOrEmpty(transaction.Signature) || string.IsNullOrEmpty(transaction.Data))
            {
                Console.WriteLine("Lỗi: Giao dịch không hợp lệ.");
                return;
            }

            // Kiểm tra xem giao dịch đã tồn tại trong danh sách chờ hoặc trong chuỗi chưa
            if (PendingTransactions.Any(t => t.TransactionId == transaction.TransactionId) ||
                Chain.Any(b => b.Transactions.Any(t => t.TransactionId == transaction.TransactionId)))
            {
                Console.WriteLine($"Giao dịch ID {transaction.TransactionId} đã tồn tại. Bỏ qua.");
                return;
            }

            PendingTransactions.Add(transaction);
            Console.WriteLine($"Giao dịch mới đã được thêm vào danh sách chờ: ID {transaction.TransactionId}");
        }

        // Phương thức khai thác các giao dịch đang chờ xử lý để tạo một block mới.
        public Block MinePendingTransactions()
        {
            if (PendingTransactions.Count == 0)
            {
                Console.WriteLine("Không có giao dịch đang chờ xử lý để khai thác.");
                return null;
            }

            var latestBlock = GetLatestBlock();
            var newBlock = new Block
            {
                Index = latestBlock.Index + 1,
                Timestamp = DateTime.UtcNow,
                Transactions = new List<Transaction>(PendingTransactions),
                PreviousHash = latestBlock.Hash,
                Nonce = 0
            };

            Console.WriteLine("Bắt đầu khai thác block mới...");
            newBlock.Hash = MineBlock(newBlock);
            Console.WriteLine($"Block mới đã được khai thác thành công! Hash: {newBlock.Hash}");

            Chain.Add(newBlock);
            PendingTransactions.Clear();
            return newBlock;
        }

        // Phương thức thực hiện Proof of Work để tìm hash hợp lệ.
        private string MineBlock(Block block)
        {
            var difficultyString = new string('0', _difficulty);
            while (true)
            {
                var hash = CalculateHash(block.Index, block.Timestamp, block.Transactions, block.PreviousHash, block.Nonce);
                if (hash.StartsWith(difficultyString))
                {
                    block.Hash = hash;
                    return hash;
                }
                block.Nonce++;
            }
        }

        // Phương thức tính toán hash của block, bao gồm cả các giao dịch.
        public string CalculateHash(int index, DateTime timestamp, List<Transaction> transactions, string previousHash, int nonce)
        {
            // Sắp xếp các giao dịch trước khi băm để đảm bảo tính nhất quán
            var sortedTransactions = transactions.OrderBy(t => t.TransactionId).ToList();
            string rawData = $"{index}-{timestamp.ToString("o")}-{JsonSerializer.Serialize(sortedTransactions)}-{previousHash}-{nonce}";
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        // Phương thức mới với logic xử lý xung đột được cải thiện
        public bool AddBlockFromPeer(Block newBlock)
        {
            // 1. Kiểm tra nếu block đã tồn tại
            if (Chain.Any(b => b.Hash == newBlock.Hash))
            {
                Console.WriteLine("--> Block đã tồn tại trong chuỗi, không cần thêm.");
                return false;
            }

            // 2. Kiểm tra nếu block là block tiếp theo hợp lệ (trường hợp thông thường)
            var latestBlock = GetLatestBlock();
            if (newBlock.PreviousHash == latestBlock.Hash)
            {
                if (IsValidNewBlock(newBlock))
                {
                    Chain.Add(newBlock);
                    RemoveTransactionsFromPending(newBlock.Transactions);
                    return true;
                }
            }
            // 3. Nếu không phải là block tiếp theo, có thể chuỗi của node này đang bị lạc hậu
            // Trong trường hợp này, chúng ta sẽ trả về false và yêu cầu đồng bộ chuỗi
            else
            {
                Console.WriteLine("--> Lỗi: PreviousHash của block mới không khớp với block cuối cùng của tôi. Có thể chuỗi của tôi đã bị lạc hậu. Cần đồng bộ hóa!");
                return false;
            }
            return false;
        }

        private void RemoveTransactionsFromPending(List<Transaction> transactionsInBlock)
        {
            foreach (var transactionInBlock in transactionsInBlock)
            {
                var pendingTransaction = PendingTransactions.FirstOrDefault(t => t.TransactionId == transactionInBlock.TransactionId);
                if (pendingTransaction != null)
                {
                    PendingTransactions.Remove(pendingTransaction);
                }
            }
        }

        public bool IsValidNewBlock(Block newBlock)
        {
            try
            {
                var latestBlock = GetLatestBlock();
                if (latestBlock.Index + 1 != newBlock.Index)
                {
                    Console.WriteLine("--> Lỗi: Chỉ số block mới không hợp lệ.");
                    return false;
                }
                if (latestBlock.Hash != newBlock.PreviousHash)
                {
                    // Lỗi này giờ được xử lý ở AddBlockFromPeer, nhưng vẫn giữ lại để kiểm tra
                    // các block nối tiếp nhau trong IsValidChain
                    Console.WriteLine("--> Lỗi: Hash của block trước đó không khớp.");
                    return false;
                }
                var calculatedHash = CalculateHash(newBlock.Index, newBlock.Timestamp, newBlock.Transactions, newBlock.PreviousHash, newBlock.Nonce);
                if (calculatedHash != newBlock.Hash)
                {
                    Console.WriteLine($"--> Lỗi: Hash của block không hợp lệ. Hash tính toán: {calculatedHash}, Hash của block: {newBlock.Hash}");
                    return false;
                }
                // Thêm kiểm tra Proof of Work
                var difficultyString = new string('0', _difficulty);
                if (!newBlock.Hash.StartsWith(difficultyString))
                {
                    Console.WriteLine("--> Lỗi: Proof of Work không hợp lệ.");
                    return false;
                }
                return true;
            }
            catch (InvalidOperationException)
            {
                // Xử lý trường hợp chuỗi rỗng
                if (newBlock.Index == 0 && newBlock.PreviousHash == "0")
                {
                    return true;
                }
                Console.WriteLine("--> Lỗi: Block gốc không hợp lệ.");
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
            // Block đầu tiên phải có PreviousHash là "0"
            if (chain[0].PreviousHash != "0")
            {
                return false;
            }

            // Tái tính toán hash cho tất cả các block để đảm bảo tính hợp lệ
            for (int i = 1; i < chain.Count; i++)
            {
                var currentBlock = chain[i];
                var previousBlock = chain[i - 1];

                // Kiểm tra liên kết chuỗi
                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    return false;
                }
                // Tái tính toán và kiểm tra hash
                if (CalculateHash(currentBlock.Index, currentBlock.Timestamp, currentBlock.Transactions, currentBlock.PreviousHash, currentBlock.Nonce) != currentBlock.Hash)
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

            if (seedNodes.Count == 0 && _blockchain.Chain.Count <= 1)
            {
                _blockchain.CreateGenesisBlock();
                Console.WriteLine($"Node {_nodeName} đã tạo block gốc.");
            }

            ConnectToSeedNodes(seedNodes);
        }

        private void ListenForConnections()
        {
            while (true)
            {
                try
                {
                    var client = _server.AcceptTcpClient();
                    lock (_peersLock)
                    {
                        _peers.Add(client);
                    }
                    Console.WriteLine($"New peer connected from {((IPEndPoint)client.Client.RemoteEndPoint).Address}!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi chấp nhận kết nối: {ex.Message}");
                }
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
                Thread.Sleep(100);
            }
        }

        private void ProcessMessage(TcpClient sender, Message? message)
        {
            if (message == null) return;

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
                        if (_blockchain.AddBlockFromPeer(newBlock))
                        {
                            Console.WriteLine($"--> Block từ {message.Sender} đã được xác thực và thêm thành công vào chuỗi!");
                            // Sau khi thêm thành công, phát tán block này tới các peer khác để đảm bảo đồng bộ
                            BroadcastBlock(newBlock, _nodeName);
                        }
                        else
                        {
                            Console.WriteLine($"--> Block từ {message.Sender} không hợp lệ và đã bị từ chối.");
                            // Nếu block không hợp lệ, yêu cầu đồng bộ chuỗi
                            var syncMessage = new Message { Type = "SYNC_CHAIN", Data = "", Sender = _nodeName };
                            SendMessage(sender, syncMessage);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("--> Lỗi khi xử lý block mới. Yêu cầu đồng bộ chuỗi...");
                        var syncMessage = new Message { Type = "SYNC_CHAIN", Data = "", Sender = _nodeName };
                        SendMessage(sender, syncMessage);
                    }
                    break;
                case "NEW_TRANSACTION":
                    var newTransaction = JsonSerializer.Deserialize<Transaction>(message.Data);
                    Console.WriteLine($"--> Đã nhận giao dịch mới từ {message.Sender}. Đang thêm vào danh sách chờ...");
                    _blockchain.AddTransactionToPending(newTransaction);
                    break;

                case "SYNC_CHAIN":
                    var serializedChain = JsonSerializer.Serialize(_blockchain.Chain);
                    var syncResponse = new Message { Type = "CHAIN_RESPONSE", Data = serializedChain, Sender = _nodeName };
                    SendMessage(sender, syncResponse);
                    break;

                case "CHAIN_RESPONSE":
                    var peerChain = JsonSerializer.Deserialize<List<Block>>(message.Data);
                    _blockchain.ReplaceChain(peerChain);
                    Console.WriteLine($"--> Chuỗi blockchain của tôi đã được cập nhật từ {message.Sender}!");
                    break;

                default:
                    Console.WriteLine($"--> Received unknown message type: {message.Type}");
                    break;
            }
        }

        private void SendMessage(TcpClient peer, Message message)
        {
            try
            {
                var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                peer.GetStream().Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi gửi tin nhắn tới peer: {ex.Message}");
            }
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

        public void BroadcastTransaction(Transaction transaction)
        {
            var newTransactionMessage = new Message { Type = "NEW_TRANSACTION", Data = JsonSerializer.Serialize(transaction), Sender = _nodeName };
            lock (_peersLock)
            {
                foreach (var peer in _peers)
                {
                    SendMessage(peer, newTransactionMessage);
                }
            }
        }
    }
}
