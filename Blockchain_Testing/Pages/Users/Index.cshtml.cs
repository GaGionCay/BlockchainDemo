using Blockchain_Testing.Data;
using Blockchain_Testing.Models;
using Blockchain_Testing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Blockchain_Testing.Pages.Users
{
    public class IndexModel : PageModel
    {
    //    private readonly AppDbContext _db;
    //    private readonly BlockchainService _blockchain;

    //    public IndexModel(AppDbContext db, BlockchainService blockchain)
    //    {
    //        _db = db;
    //        _blockchain = blockchain;
    //    }

    //    public List<User> Users { get; set; }
    //    public bool IsChainValid { get; set; }
    //    public string ChainValidationMessage { get; set; }

    //    public async Task OnGetAsync()
    //    {
    //        Users = await _db.Users.ToListAsync();
    //        // Thay đổi logic kiểm tra ở đây
    //        IsChainValid = _blockchain.IsChainValid() && _blockchain.IsDatabaseInSync(Users);
    //        ChainValidationMessage = IsChainValid ? "Blockchain and database are in sync!" : "System integrity compromised! Data may have been tampered with.";
    //    }

    //    public async Task<IActionResult> OnPostDeleteAsync(int id)
    //    {
    //        var user = await _db.Users.FindAsync(id);

    //        if (user != null)
    //        {
    //            _db.Users.Remove(user);
    //            await _db.SaveChangesAsync();
    //        }

    //        return RedirectToPage();
    //    }
    }
}
