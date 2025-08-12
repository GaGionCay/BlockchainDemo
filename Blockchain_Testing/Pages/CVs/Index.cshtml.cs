using Blockchain_Testing.Data;
using Blockchain_Testing.Models;
using Blockchain_Testing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace Blockchain_Testing.Pages.CVs
{
    public class CVViewModel
    {
        public CV CV { get; set; } = null!;
        public Block? BlockchainBlock { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly BlockchainService _blockchain;

        public IndexModel(AppDbContext db, BlockchainService blockchain)
        {
            _db = db;
            _blockchain = blockchain;
        }

        public List<CVViewModel> CVs { get; set; } = new List<CVViewModel>();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var userCVs = await _db.CVs
                                       .Where(c => c.UserId == int.Parse(userId))
                                       .ToListAsync();

                foreach (var cv in userCVs)
                {
                    Block? block = null;
                    if (!string.IsNullOrEmpty(cv.BlockchainHash))
                    {
                        block = _blockchain.Chain.FirstOrDefault(b => b.Hash == cv.BlockchainHash);
                    }

                    CVs.Add(new CVViewModel
                    {
                        CV = cv,
                        BlockchainBlock = block
                    });
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
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

            return RedirectToPage();
        }
    }
}
