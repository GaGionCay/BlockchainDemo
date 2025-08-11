using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Blockchain_Testing.Data;
using Blockchain_Testing.Models;
using Blockchain_Testing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Blockchain_Testing.Pages.CVs
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly BlockchainService _blockchain;

        public CreateModel(AppDbContext db, BlockchainService blockchain)
        {
            _db = db;
            _blockchain = blockchain;
        }

        [BindProperty]
        public CV CV { get; set; } = new CV();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            CV.UserId = int.Parse(userId);
            CV.CreatedAt = DateTime.UtcNow;

            // Bước 1: Lưu CV vào database trước để các ID được gán
            _db.CVs.Add(CV);
            await _db.SaveChangesAsync();

            // Tải lại các đối tượng con sau khi đã có ID từ database
            await _db.Entry(CV).Collection(c => c.Experiences).LoadAsync();
            await _db.Entry(CV).Collection(c => c.Educations).LoadAsync();

            // Sử dụng chuỗi DateTime đã được định dạng để đảm bảo tính nhất quán
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

            // Bước 2: Tạo cặp khóa và ký dữ liệu
            using var rsa = new RSACryptoServiceProvider(2048);
            var privateKey = rsa.ToXmlString(true);
            var publicKey = rsa.ToXmlString(false);

            var dataBytes = Encoding.UTF8.GetBytes(serializedCV);
            var signedDataBytes = rsa.SignData(dataBytes, SHA256.Create());
            var signature = Convert.ToBase64String(signedDataBytes);

            // Bước 3: Thêm block vào blockchain và gán hash
            var block = _blockchain.AddBlock(serializedCV);
            CV.BlockchainHash = block.Hash;

            // Lưu public key và chữ ký vào CV
            CV.PublicKey = publicKey;
            CV.Signature = signature;

            // Bước 4: Cập nhật hash, public key và signature vào database
            _db.CVs.Update(CV);
            await _db.SaveChangesAsync();

            return RedirectToPage("/CVs/Index");
        }
    }
}
