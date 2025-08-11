using Blockchain_Testing.Data;
using Blockchain_Testing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Blockchain_Testing.Pages.CVs
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;

        public IndexModel(AppDbContext db)
        {
            _db = db;
        }

        public List<CV> CVs { get; set; } = new List<CV>();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                CVs = await _db.CVs
                               .Where(c => c.UserId == int.Parse(userId))
                               .ToListAsync();
            }
        }

        // Thêm phương thức này để xử lý việc xóa CV
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // Lấy ID người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var cv = await _db.CVs
                              .FirstOrDefaultAsync(c => c.Id == id && c.UserId == int.Parse(userId));

            if (cv != null)
            {
                _db.CVs.Remove(cv);
                await _db.SaveChangesAsync();
            }

            // Chuyển hướng về trang Index sau khi xóa
            return RedirectToPage();
        }
    }
}