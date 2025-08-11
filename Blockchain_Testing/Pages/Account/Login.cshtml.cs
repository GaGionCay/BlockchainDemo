using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Blockchain_Testing.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace Blockchain_Testing.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public LoginModel(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; } = new LoginInputModel();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == Input.Email);

            if (user == null || HashPassword(Input.Password) != user.PasswordHash)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            // Tạo danh sách Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            // Tạo ClaimsIdentity và ClaimsPrincipal
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Đăng nhập người dùng bằng cookie authentication
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            return RedirectToPage("/CVs/Index");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        public class LoginInputModel
        {
            [Required, EmailAddress, MaxLength(100)]
            public string Email { get; set; } = string.Empty;

            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }
    }
}