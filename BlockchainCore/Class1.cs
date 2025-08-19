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
using BlockchainCore.Models;

namespace BlockchainCore
{
    public class BlockchainCore
    {
        public List<Block> Chain { get; set; } = new List<Block>();
        public List<Transaction> PendingTransactions { get; set; } = new List<Transaction>();
        private int _difficulty = 4;
        private readonly object _blockchainLock = new object();

        public BlockchainCore()
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
}
