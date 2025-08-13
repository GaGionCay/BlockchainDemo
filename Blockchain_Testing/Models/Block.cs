// File: Blockchain_Testing.Models/Block.cs
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json; // Thêm namespace này để serialize đối tượng Transaction

namespace Blockchain_Testing.Models
{
    // Cần một class Transaction để lưu trữ các giao dịch
    public class Transaction
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }

        public string Data { get; set; } // Thuộc tính mới để lưu trữ dữ liệu tùy chỉnh

        // Cập nhật constructor để chấp nhận tham số 'data'
        public Transaction(string fromAddress, string toAddress, decimal amount, string data = null)
        {
            FromAddress = fromAddress;
            ToAddress = toAddress;
            Amount = amount;
            Data = data;
        }
    }

    // Lớp này định nghĩa cấu trúc của một Block
    public class Block
    {
        // Thuộc tính để lưu trữ chỉ số của block
        public int Index { get; set; }
        // Thuộc tính để lưu trữ thời gian tạo block
        public DateTime Timestamp { get; set; }

        // Cập nhật: Dữ liệu được lưu trữ trong block là một danh sách các giao dịch
        public List<Transaction> Transactions { get; set; }

        // Thuộc tính để lưu trữ hash của block trước đó
        public string? PreviousHash { get; set; }
        // Thuộc tính để lưu trữ hash hiện tại của block
        public string? Hash { get; set; }
        // Thuộc tính Nonce, được sử dụng trong quá trình Proof-of-Work
        public int Nonce { get; set; }
        // Thuộc tính Difficulty, được sử dụng để xác định độ khó của quá trình mining
        public int Difficulty { get; set; }

        // Cập nhật constructor để nhận danh sách Transaction
        public Block(int index, List<Transaction> transactions, string previousHash)
        {
            this.Index = index;
            this.Timestamp = DateTime.Now;
            this.Transactions = transactions;
            this.PreviousHash = previousHash;
            this.Nonce = 0;
            this.Difficulty = 0;
            this.Hash = CalculateHash();
        }

        public string CalculateHash()
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Serializer danh sách giao dịch thành chuỗi JSON để tính hash
                string transactionsAsJson = JsonSerializer.Serialize(Transactions);

                string rawData = $"{Index}{Timestamp}{transactionsAsJson}{PreviousHash}{Nonce}{Difficulty}";
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return Convert.ToHexString(bytes);
            }
        }
    }
}
