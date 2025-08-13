using Blockchain_Testing.Data;
using Blockchain_Testing.Models;
using Blockchain_Testing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Blockchain_Testing.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly Blockchain_Testing.Services.BlockchainService _blockchain;

        public RegisterModel(AppDbContext db, BlockchainService blockchain)
        {
            _db = db;
            _blockchain = blockchain;
        }

        [BindProperty]
        public RegisterInputModel Input { get; set; } = new();

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid || Input is null)
                return Page();

            if (string.IsNullOrWhiteSpace(Input.Username) ||
                string.IsNullOrWhiteSpace(Input.Email) ||
                string.IsNullOrWhiteSpace(Input.Password) ||
                string.IsNullOrWhiteSpace(Input.ConfirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Please fill in all required fields.");
                return Page();
            }

            if (_db.Users.Any(u => u.Username == Input.Username))
            {
                ModelState.AddModelError("Input.Username", "Tên người dùng đã tồn tại.");
                return Page();
            }

            if (_db.Users.Any(u => u.Email == Input.Email))
            {
                ModelState.AddModelError("Input.Email", "Email đã tồn tại.");
                return Page();
            }

            var hashedPassword = HashPassword(Input.Password);

            var newUser = new User
            {
                Username = Input.Username,
                Email = Input.Email,
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow
            };

            // Serialize đối tượng User thành chuỗi JSON
            var userDataAsJson = JsonSerializer.Serialize(newUser);

            // Tạo một đối tượng giao dịch (Transaction) mới, cung cấp đủ 3 tham số bắt buộc.
            // Sử dụng các giá trị placeholder cho fromAddress và amount.
            var newTransaction = new Transaction(
                fromAddress: "SYSTEM_REGISTRATION", // Địa chỉ đặc biệt cho giao dịch đăng ký
                toAddress: newUser.Username,        // Địa chỉ của người dùng mới
                amount: 0.0m,                       // Không có giá trị tiền tệ
                data: userDataAsJson                // Dữ liệu đăng ký người dùng
            );

            _blockchain.AddTransaction(newTransaction);
            var minedBlock = _blockchain.MinePendingTransactions();
            newUser.BlockchainHash = minedBlock.Hash;

            _db.Users.Add(newUser);
            _db.SaveChanges();

            return RedirectToPage("/Account/Login");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        public class RegisterInputModel
        {
            public int Id { get; set; }

            [Required, MaxLength(50)]
            public string Username { get; set; } = string.Empty;

            [Required, EmailAddress, MaxLength(100)]
            public string Email { get; set; } = string.Empty;

            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
