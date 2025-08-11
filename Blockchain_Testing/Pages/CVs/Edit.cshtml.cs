using System.Linq;
using System.Threading.Tasks;
using Blockchain_Testing.Data;
using Blockchain_Testing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Blockchain_Testing.Pages.CVs
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _db;

        public EditModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public CV CV { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Tải CV và các đối tượng con từ database
            CV = await _db.CVs
                .Include(c => c.Experiences)
                .Include(c => c.Educations)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (CV == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Lấy CV hiện tại từ database để đảm bảo chỉ cập nhật các trường được gửi từ form
            var existingCV = await _db.CVs
                .Include(c => c.Experiences)
                .Include(c => c.Educations)
                .FirstOrDefaultAsync(c => c.Id == CV.Id);

            if (existingCV == null)
            {
                return NotFound();
            }

            // Cập nhật các trường chính của CV
            existingCV.Title = CV.Title;

            // Xóa các danh sách cũ
            _db.Experiences.RemoveRange(existingCV.Experiences);
            _db.Educations.RemoveRange(existingCV.Educations);

            // Thêm các danh sách mới từ form
            existingCV.Experiences = CV.Experiences;
            existingCV.Educations = CV.Educations;

            // Đánh dấu CV là đã được chỉnh sửa
            existingCV.ModifiedAt = DateTime.UtcNow;

            // Lưu thay đổi vào database
            // CHÚ Ý: Không cập nhật hash hoặc thêm block mới vào blockchain
            await _db.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}