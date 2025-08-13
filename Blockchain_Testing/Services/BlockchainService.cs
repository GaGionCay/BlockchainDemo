using Blockchain_Testing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; // Thêm namespace này để serialize dữ liệu

namespace Blockchain_Testing.Services
{
    public class BlockchainService
    {
        public List<Block> Chain { get; set; }
        // Một danh sách để lưu trữ các giao dịch đang chờ được khai thác
        public List<Transaction> PendingTransactions { get; set; }
        private readonly int _difficulty = 2; // Độ khó của việc khai thác

        public BlockchainService()
        {
            // Khởi tạo blockchain với một genesis block duy nhất
            PendingTransactions = new List<Transaction>();
            Chain = new List<Block> { CreateGenesisBlock() };
        }

        /// <summary>
        /// Tạo block đầu tiên (genesis block) của blockchain.
        /// </summary>
        private Block CreateGenesisBlock()
        {
            // Genesis block chứa một danh sách giao dịch rỗng
            var genesisBlock = new Block(0, new List<Transaction>(), "0");
            // Khai thác genesis block với độ khó đã định
            return MineBlock(genesisBlock);
        }

        /// <summary>
        /// Lấy block mới nhất trong chuỗi.
        /// </summary>
        public Block GetLatestBlock() => Chain.Last();

        /// <summary>
        /// Thêm một giao dịch mới vào danh sách chờ xử lý.
        /// </summary>
        /// <param name="transaction">Đối tượng giao dịch cần thêm.</param>
        public void AddTransaction(Transaction transaction)
        {
            // Thêm giao dịch vào danh sách chờ
            PendingTransactions.Add(transaction);
        }

        /// <summary>
        /// Thực hiện quá trình khai thác (mining) các giao dịch đang chờ xử lý.
        /// </summary>
        public Block MinePendingTransactions()
        {
            // Lấy block trước đó để tạo liên kết
            var previousBlock = GetLatestBlock();

            // Tạo một block mới từ các giao dịch đang chờ
            var newBlock = new Block(previousBlock.Index + 1, PendingTransactions, previousBlock.Hash);

            Console.WriteLine("Bắt đầu khai thác block mới...");

            // Thực hiện quá trình đào để tìm hash hợp lệ
            var minedBlock = MineBlock(newBlock);

            Console.WriteLine("Khai thác thành công!");

            // Thêm block đã được khai thác vào chuỗi
            Chain.Add(minedBlock);

            // Xóa danh sách giao dịch đang chờ để chuẩn bị cho block tiếp theo
            PendingTransactions = new List<Transaction>();
            return minedBlock;
        }

        /// <summary>
        /// Thực hiện thuật toán "Proof-of-Work" để tìm hash hợp lệ.
        /// </summary>
        /// <param name="block">Block cần đào.</param>
        public Block MineBlock(Block block)
        {
            block.Difficulty = _difficulty;
            var leadingZeros = new string('0', _difficulty);

            do
            {
                block.Nonce++;
                // Sử dụng phương thức CalculateHash() mới của block
                block.Hash = block.CalculateHash();
            } while (block.Hash == null || !block.Hash.StartsWith(leadingZeros));

            return block;
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của toàn bộ chuỗi blockchain.
        /// </summary>
        public bool IsChainValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                var currentBlock = Chain[i];
                var previousBlock = Chain[i - 1];

                // Tính toán lại hash của block hiện tại và so sánh
                var calculatedHash = currentBlock.CalculateHash();
                if (currentBlock.Hash != calculatedHash)
                {
                    Console.WriteLine($"Hash không hợp lệ ở block index {currentBlock.Index}");
                    return false;
                }

                // Kiểm tra PreviousHash có khớp với Hash của block trước đó không
                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    Console.WriteLine($"PreviousHash không khớp ở block index {currentBlock.Index}");
                    return false;
                }
            }
            return true;
        }
    }
}
