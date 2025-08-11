using Blockchain_Testing.Data;
using Blockchain_Testing.Models;
using Blockchain_Testing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace Blockchain_Testing.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly BlockchainService _blockchain;

        public RegisterModel(AppDbContext db, BlockchainService blockchain)
        {
            _db = db;
            _blockchain = blockchain;
        }

        [BindProperty]
        public RegisterInputModel Input { get; set; }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            // Đảm bảo bạn mở lại dòng này
            //if (!ModelState.IsValid) return Page();

            // Kiểm tra username và email đã tồn tại chưa
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
                // Xóa Input.Id
                Username = Input.Username,
                Email = Input.Email,
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow
            };

            var userData = $"{newUser.Username}-{newUser.Email}-{newUser.CreatedAt}";
            var block = _blockchain.AddBlock(userData);
            newUser.BlockchainHash = block.Hash;

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
            public string Username { get; set; }

            [Required, EmailAddress, MaxLength(100)]
            public string Email { get; set; }

            [Required, DataType(DataType.Password)]
            public string Password { get; set; }

            [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; }
        }
    }
}
