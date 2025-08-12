using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Blockchain_Testing.Data;
using Blockchain_Testing.Models;
using BlockchainCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Linq;

namespace Blockchain_Testing.Pages.CVs
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly P2PNode _p2pNode;

        public CreateModel(AppDbContext db, P2PNode p2pNode)
        {
            _db = db;
            _p2pNode = p2pNode;
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

            // Bước 1: Tạo cặp khóa RSA và gán Public Key vào CV
            using var rsa = new RSACryptoServiceProvider(2048);
            var privateKey = rsa.ToXmlString(true);
            var publicKey = rsa.ToXmlString(false);
            CV.PublicKey = publicKey;

            // Bước 2: Lưu CV vào database để có một ID duy nhất
            // Gán trạng thái ban đầu là "Pending"
            CV.Status = "Pending";
            _db.CVs.Add(CV);
            await _db.SaveChangesAsync();

            // Tải lại các đối tượng con sau khi đã có ID từ database
            await _db.Entry(CV).Collection(c => c.Experiences).LoadAsync();
            await _db.Entry(CV).Collection(c => c.Educations).LoadAsync();

            // Sắp xếp các danh sách để đảm bảo chuỗi serialized luôn nhất quán
            var cvData = new
            {
                CV.Id,
                CV.Title,
                CreatedAt = CV.CreatedAt.ToString("o"), // Sử dụng định dạng ISO 8601 để nhất quán
                CV.UserId,
                CV.PublicKey,
                Experiences = CV.Experiences.OrderBy(e => e.Id).ToList(),
                Educations = CV.Educations.OrderBy(e => e.Id).ToList()
            };

            var serializedData = JsonSerializer.Serialize(cvData, new JsonSerializerOptions { WriteIndented = false });

            // Bước 3: Ký dữ liệu đã serialized bằng khóa bí mật
            var dataBytes = Encoding.UTF8.GetBytes(serializedData);
            var signedDataBytes = rsa.SignData(dataBytes, SHA256.Create());
            var signature = Convert.ToBase64String(signedDataBytes);

            // Lưu chữ ký và tạo TransactionId
            CV.Signature = signature;
            CV.TransactionId = $"cv_{CV.Id}";

            // Bước 4: Cập nhật chữ ký và TransactionId vào database
            _db.CVs.Update(CV);
            await _db.SaveChangesAsync();

            // Bước 5: Đóng gói toàn bộ dữ liệu đã ký vào một đối tượng giao dịch
            var transaction = new Transaction
            {
                Sender = CV.UserId.ToString(),
                Data = serializedData,
                Signature = signature,
                Timestamp = DateTime.UtcNow,
                TransactionId = CV.TransactionId
            };

            // Bước 6: Phát tán giao dịch tới các node khác trong mạng lưới
            _p2pNode.BroadcastTransaction(transaction);

            return RedirectToPage("/CVs/Index");
        }
    }
}
