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
                return Page();
            }

            var block = _blockchain.Chain.FirstOrDefault(b => b.Hash == CV.BlockchainHash);

            if (block == null)
            {
                Message = "CV hash not found on blockchain. System integrity may be compromised.";
                IsVerified = false;
                return Page();
            }

            var cvData = new
            {
                CV.Id,
                CV.Title,
                CreatedAt = CV.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                CV.UserId,
                Experiences = CV.Experiences.OrderBy(e => e.Id).ToList(),
                Educations = CV.Educations.OrderBy(e => e.Id).ToList()
            };

            var serializedCV = JsonSerializer.Serialize(cvData, new JsonSerializerOptions { WriteIndented = false });

            // Bước 1: Xác thực Hash (kiểm tra tính toàn vẹn của dữ liệu)
            RecalculatedHash = _blockchain.CalculateHash(
                block.Index,
                block.Timestamp,
                serializedCV,
                block.PreviousHash,
                block.Nonce
            );

            bool isHashValid = RecalculatedHash == CV.BlockchainHash;

            // Bước 2: Xác thực Chữ ký số (kiểm tra tính xác thực của người tạo)
            bool isSignatureValid = false;
            try
            {
                using var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(CV.PublicKey);
                var dataBytes = Encoding.UTF8.GetBytes(serializedCV);
                var signatureBytes = Convert.FromBase64String(CV.Signature);
                isSignatureValid = rsa.VerifyData(dataBytes, SHA256.Create(), signatureBytes);
            }
            catch
            {
                // Xử lý lỗi nếu public key hoặc signature không hợp lệ
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
