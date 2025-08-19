using BlockchainCore.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace Blockchain_Testing.Pages
{
    // Cập nhật lớp BlockchainModel để kiểm tra tính hợp lệ của chuỗi
    public class BlockchainModel : PageModel
    {
        // Sử dụng dịch vụ từ BlockchainCore
        private readonly BlockchainCore.BlockchainCore _blockchain;

        // Thuộc tính để lưu trữ chuỗi blockchain
        public List<Block> Chain { get; set; }

        // Thuộc tính để lưu trữ tất cả các giao dịch
        public List<Transaction> AllTransactions { get; set; }

        // Thuộc tính mới để lưu trạng thái hợp lệ của chuỗi
        // Thuộc tính này sẽ được sử dụng để hiển thị thông báo trên file .cshtml
        public bool IsChainValid { get; set; }

        // Tiêm dịch vụ từ BlockchainCore
        public BlockchainModel(BlockchainCore.BlockchainCore blockchain)
        {
            _blockchain = blockchain;
        }

        public void OnGet()
        {
            // Lấy chuỗi blockchain từ dịch vụ
            Chain = _blockchain.Chain;

            // Gọi phương thức kiểm tra tính hợp lệ của chuỗi.
            // Phương thức này sẽ duyệt qua từng block và kiểm tra xem
            // `PreviousHash` của block hiện tại có khớp với `Hash` của block trước đó không.
            IsChainValid = _blockchain.IsChainValid();

            // Khởi tạo danh sách tất cả các giao dịch
            AllTransactions = new List<Transaction>();

            // Lấy tất cả các giao dịch từ mỗi block trong chuỗi
            foreach (var block in Chain)
            {
                AllTransactions.AddRange(block.Transactions);
            }
        }
    }
}
