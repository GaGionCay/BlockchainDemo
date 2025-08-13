using Blockchain_Testing.Data;
using Blockchain_Testing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;
using Blockchain_Testing.Models;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic; // Thêm thư viện này để sử dụng List

namespace Blockchain_Testing.Pages.CVs
{
    public class VerifyModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly BlockchainService _blockchain;

        public VerifyModel(AppDbContext db, BlockchainService blockchain)
        {
            _db = db;
            _blockchain = blockchain;
        }

        [BindProperty(SupportsGet = true)]
        public int id { get; set; }

        public CV? CV { get; set; }
        public bool IsVerified { get; set; }
        public string? Message { get; set; }
        public string? RecalculatedHash { get; set; }
        public Block? BlockFromBlockchain { get; set; }

        public async Task OnGetAsync()
        {
            CV = await _db.CVs
                            .Include(c => c.Experiences)
                            .Include(c => c.Educations)
                            .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            CV = await _db.CVs
                            .Include(c => c.Experiences)
                            .Include(c => c.Educations)
                            .FirstOrDefaultAsync(c => c.Id == id);

            if (CV == null)
            {
                Message = "CV not found.";
                IsVerified = false;
                return Page();
            }

            // Tìm block tương ứng trên blockchain bằng hash được lưu trong CV.
            BlockFromBlockchain = _blockchain.Chain.FirstOrDefault(b => b.Hash == CV.BlockchainHash);
            if (BlockFromBlockchain == null)
            {
                Message = "CV hash not found on blockchain. System integrity may be compromised.";
                IsVerified = false;
                return Page();
            }

            // Khôi phục lại dữ liệu CV ban đầu (dạng Transaction) từ block đã lưu
            // Giả sử block.Transactions là danh sách các giao dịch liên quan đến CV
            // Note: Cấu trúc này cần được định nghĩa rõ ràng khi bạn tạo block
            // Hiện tại, chúng ta sẽ tạo lại transaction từ dữ liệu CV.
            var cvTransaction = new Transaction(
                fromAddress: CV.UserId.ToString(), // Hoặc một địa chỉ ví cụ thể
                toAddress: "CV_HASH_STORE",
                amount: 1m // Tùy thuộc vào logic của bạn
            );

            // Giả sử block chỉ chứa một transaction duy nhất liên quan đến CV này.
            // Điều này cần được đảm bảo trong logic tạo block của bạn.
            var tempTransactions = new List<Transaction> { cvTransaction };

            // Bước 1: Kiểm tra tính toàn vẹn của Hash
            // Tạo một đối tượng block tạm thời với dữ liệu CV từ cơ sở dữ liệu
            // Gán dữ liệu này vào một Transaction để khớp với cấu trúc Block mới
            var tempBlock = new Block(
                BlockFromBlockchain.Index,
                tempTransactions, // Sử dụng danh sách transaction đã tạo
                BlockFromBlockchain.PreviousHash)
            {
                Nonce = BlockFromBlockchain.Nonce,
                Difficulty = BlockFromBlockchain.Difficulty
            };

            // Tính toán lại hash bằng phương thức của chính đối tượng block
            RecalculatedHash = tempBlock.CalculateHash();

            bool isHashValid = RecalculatedHash == CV.BlockchainHash;

            // Bước 2: Kiểm tra tính xác thực của chữ ký số
            bool isSignatureValid = false;
            // Lưu ý: Logic kiểm tra chữ ký này không phụ thuộc vào cấu trúc của Block,
            // nó chỉ phụ thuộc vào dữ liệu được ký (serializedCV).
            // Dòng này cần phải được lấy từ nơi bạn đã ký dữ liệu CV.
            var serializedCV = JsonSerializer.Serialize(CV, new JsonSerializerOptions { WriteIndented = false });

            try
            {
                if (!string.IsNullOrWhiteSpace(CV.PublicKey) && !string.IsNullOrWhiteSpace(CV.Signature))
                {
                    using var rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(CV.PublicKey);

                    var dataBytes = Encoding.UTF8.GetBytes(serializedCV);
                    var signatureBytes = Convert.FromBase64String(CV.Signature);

                    using var sha = SHA256.Create();
                    isSignatureValid = rsa.VerifyData(dataBytes, sha, signatureBytes);
                }
                else
                {
                    isSignatureValid = false;
                }
            }
            catch
            {
                isSignatureValid = false;
            }

            if (isHashValid && isSignatureValid)
            {
                IsVerified = true;
                Message = "Verification successful! The CV has not been tampered with and the signature is valid.";
            }
            else if (isHashValid && !isSignatureValid)
            {
                IsVerified = false;
                Message = "Verification failed. The CV's hash is valid, but the digital signature is not. System integrity may be compromised.";
            }
            else
            {
                IsVerified = false;
                Message = "Verification failed. The CV's data has been tampered with.";
            }

            return Page();
        }
    }
}
