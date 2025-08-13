using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Blockchain_Testing.Data;
using Blockchain_Testing.Models;
using BlockchainCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;

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

        [BindProperty]
        public List<Experience> Experiences { get; set; } = new List<Experience>();

        [BindProperty]
        public List<Education> Educations { get; set; } = new List<Education>();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            // Lấy ID người dùng hiện tại từ Claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }
            var userId = int.Parse(userIdString);

            // Gán các thuộc tính cơ bản cho CV từ form và người dùng
            CV.UserId = userId;
            CV.CreatedAt = DateTime.UtcNow;

            // Gán các danh sách con (Kinh nghiệm, Học vấn) từ form vào đối tượng CV
            CV.Experiences = Experiences;
            CV.Educations = Educations;

            // Bước 1: Tạo cặp khóa RSA và gán Public Key vào CV
            // Khóa bí mật (privateKey) cần được lưu trữ ở đâu đó an toàn (ví dụ: một ví tiền ảo hoặc database riêng)
            using var rsa = new RSACryptoServiceProvider(2048);
            var privateKey = rsa.ToXmlString(true);
            var publicKey = rsa.ToXmlString(false);
            CV.PublicKey = publicKey;

            // Bước 2: Lưu CV vào database để có một ID duy nhất
            // Ban đầu, CV được lưu với trạng thái "Pending"
            CV.Status = "Pending";
            _db.CVs.Add(CV);
            await _db.SaveChangesAsync();

            // Sắp xếp các danh sách con để đảm bảo chuỗi serialized luôn nhất quán
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

            // Bước 3: Serialize toàn bộ dữ liệu CV thành một chuỗi JSON
            var serializedData = JsonSerializer.Serialize(cvData, new JsonSerializerOptions { WriteIndented = false });

            // Bước 4: Ký dữ liệu đã serialized bằng khóa bí mật
            var dataBytes = Encoding.UTF8.GetBytes(serializedData);
            var signedDataBytes = rsa.SignData(dataBytes, SHA256.Create());
            var signature = Convert.ToBase64String(signedDataBytes);

            // Gán chữ ký và tạo ID giao dịch
            CV.Signature = signature;
            CV.TransactionId = $"cv_{CV.Id}";

            // Bước 5: Tạo đối tượng giao dịch (Transaction)
            var transaction = new BlockchainCore.Transaction
            {
                FromAddress = CV.UserId.ToString(),
                ToAddress = "CV_Storage_Wallet",
                Data = serializedData,
                Signature = signature,
                Timestamp = DateTime.UtcNow,
                TransactionId = CV.TransactionId
            };

            // Bước 6: Phát tán giao dịch tới các node khác trong mạng lưới P2P.
            // Sửa lỗi ở đây: Thay vì gọi _p2pNode.BroadcastTransaction (phương thức không tồn tại),
            // chúng ta gọi phương thức chính xác là CreateAndBroadcastTransaction.
            _p2pNode.CreateAndBroadcastTransaction(transaction);

            // Bước 7: Cập nhật CV trong database với chữ ký và ID giao dịch
            _db.CVs.Update(CV);
            await _db.SaveChangesAsync();

            return RedirectToPage("/CVs/Index");
        }
    }
}
