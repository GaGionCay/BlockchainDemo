using Blockchain_Testing.Models;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Blockchain_Testing.Services
{
    public class BlockchainService
    {
        // Danh sách các block tạo nên blockchain
        public List<Block> Chain { get; set; }

        public BlockchainService()
        {
            // Khởi tạo blockchain với một genesis block duy nhất
            Chain = new List<Block> { CreateGenesisBlock() };
        }

        /// <summary>
        /// Tạo block đầu tiên (genesis block) của blockchain.
        /// </summary>
        private Block CreateGenesisBlock()
        {
            var genesisBlock = new Block
            {
                Index = 0,
                Timestamp = DateTime.UtcNow,
                Data = "Genesis Block",
                PreviousHash = "0",
                Nonce = 0 // Khởi tạo nonce
            };

            // "Đào" genesis block với độ khó thấp
            return MineBlock(genesisBlock, 2);
        }

        /// <summary>
        /// Lấy block mới nhất trong chuỗi.
        /// </summary>
        public Block GetLatestBlock() => Chain.Last();

        /// <summary>
        /// Thêm một block mới vào chuỗi.
        /// </summary>
        /// <param name="data">Dữ liệu của block.</param>
        public Block AddBlock(string data)
        {
            var previousBlock = GetLatestBlock();
            var newBlock = new Block
            {
                Index = previousBlock.Index + 1,
                Timestamp = DateTime.UtcNow,
                Data = data,
                PreviousHash = previousBlock.Hash,
                Nonce = 0 // Khởi tạo nonce trước khi đào
            };

            // Thực hiện quá trình đào để tìm hash hợp lệ
            var difficulty = 2; // Độ khó của việc đào, có thể điều chỉnh
            newBlock = MineBlock(newBlock, difficulty);

            Chain.Add(newBlock);
            return newBlock;
        }

        /// <summary>
        /// Thực hiện thuật toán "Proof-of-Work" để tìm hash hợp lệ.
        /// </summary>
        /// <param name="block">Block cần đào.</param>
        /// <param name="difficulty">Số lượng số 0 đứng đầu hash yêu cầu.</param>
        public Block MineBlock(Block block, int difficulty)
        {
            var leadingZeros = new string('0', difficulty);
            int nonce = 0;
            string hash;

            // Lặp cho đến khi tìm được hash bắt đầu bằng số 0 mong muốn
            do
            {
                nonce++;
                hash = CalculateHash(block.Index, block.Timestamp, block.Data, block.PreviousHash, nonce);
            } while (!hash.StartsWith(leadingZeros));

            block.Hash = hash;
            block.Nonce = nonce;
            return block;
        }

        /// <summary>
        /// Tính toán hash cho một block.
        /// </summary>
        public string CalculateHash(int index, DateTime timestamp, string? data, string? previousHash, int nonce)
        {
            using var sha256 = SHA256.Create();
            // Đảm bảo tất cả các tham số được dùng để tạo hash
            var rawData = $"{index}{timestamp}{data}{previousHash}{nonce}";
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToHexString(bytes);
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

                // Kiểm tra hash của block hiện tại
                var calculatedHash = CalculateHash(currentBlock.Index, currentBlock.Timestamp, currentBlock.Data, currentBlock.PreviousHash, currentBlock.Nonce);
                if (currentBlock.Hash != calculatedHash)
                {
                    return false;
                }

                // Kiểm tra PreviousHash có khớp với Hash của block trước đó không
                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
