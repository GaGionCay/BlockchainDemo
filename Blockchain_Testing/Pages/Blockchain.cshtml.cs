using Blockchain_Testing.Models;
using Blockchain_Testing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blockchain_Testing.Pages
{
    public class BlockchainModel : PageModel
    {
        private readonly BlockchainService _blockchain;
        public List<Block> Chain { get; set; }

        public BlockchainModel(BlockchainService blockchain)
        {
            _blockchain = blockchain;
        }

        public void OnGet()
        {
            Chain = _blockchain.Chain;
        }
    }
}
