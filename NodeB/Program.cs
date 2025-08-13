using BlockchainCore;
using System.Text;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.Json;

// Đặt encoding để hiển thị tiếng Việt chính xác
Console.OutputEncoding = Encoding.UTF8;

Console.Title = "Node B";

// Tạo một instance của BlockchainService.
// Chuỗi blockchain sẽ được khởi tạo mới với một genesis block mỗi khi ứng dụng khởi động.
var blockchain = new BlockchainService();

// Lỗi 1: Constructor của P2PNode yêu cầu một tham số List<string> seedNodes.
// Sửa: Thêm tham số seedNodes vào constructor.
var seedNodes = new List<string> { "127.0.0.1:8888", "127.0.0.1:8890" };
var p2pNode = new P2PNode(blockchain, "Node B", seedNodes);

// Lỗi 2: Phương thức Start của P2PNode chỉ nhận một tham số là port.
// Sửa: Bỏ tham số thứ hai khi gọi hàm Start.
p2pNode.Start(8889);

Console.WriteLine("Node B đã khởi động và kết nối tới Node A và Node C.");
Console.WriteLine("---------------------------------------------");
Console.WriteLine("Nhập 'mine' để đào các giao dịch đang chờ và phát tán block mới.");
// Thêm hướng dẫn cho lệnh mới để tạo giao dịch
Console.WriteLine("Nhập 'add' để tạo một giao dịch mới.");
Console.WriteLine("---------------------------------------------");

while (true)
{
    var input = Console.ReadLine();
    if (input != null)
    {
        switch (input.ToLower())
        {
            case "mine":
                try
                {
                    // Đào một block mới từ các giao dịch đang chờ
                    var newBlock = blockchain.MinePendingTransactions();
                    if (newBlock != null)
                    {
                        // Phát tán block này tới các node khác trong mạng lưới và gửi kèm tên của node
                        p2pNode.BroadcastBlock(newBlock);
                        Console.WriteLine($"[Node B] Đã đào thành công một block mới và phát tán tới các node còn lại.");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"[Node B] Lỗi: {ex.Message}");
                }
                break;
            case "add":
                // Logic mới: Tạo một giao dịch giả lập để thêm vào chuỗi
                Console.WriteLine("Nhập dữ liệu cho giao dịch mới (ví dụ: 'Hello World!'):");
                var transactionData = Console.ReadLine();
                if (!string.IsNullOrEmpty(transactionData))
                {
                    var newTransaction = new Transaction
                    {
                        TransactionId = Guid.NewGuid().ToString(), // Tạo một ID duy nhất
                        FromAddress = "UserB", // Địa chỉ giả lập
                        ToAddress = "UserC",    // Địa chỉ giả lập
                        Data = transactionData,
                        Timestamp = DateTime.UtcNow,
                        Signature = "DummySignature" // Giả lập chữ ký để qua bước xác thực đơn giản
                    };
                    p2pNode.CreateAndBroadcastTransaction(newTransaction);
                    Console.WriteLine($"[Node B] Đã tạo và phát tán giao dịch mới với ID: {newTransaction.TransactionId}. Bây giờ bạn có thể gõ 'mine' để đào block mới.");
                }
                break;
            default:
                Console.WriteLine("Lệnh không hợp lệ. Vui lòng nhập 'mine' hoặc 'add'.");
                break;
        }
    }
}
