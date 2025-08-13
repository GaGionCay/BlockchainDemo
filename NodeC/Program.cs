using BlockchainCore;
using System.Text;

// Đặt encoding để hiển thị tiếng Việt chính xác
Console.OutputEncoding = Encoding.UTF8;

Console.Title = "Node C";

// Tạo một instance của BlockchainService.
var blockchain = new BlockchainService();

// Danh sách các seed nodes mà Node C sẽ kết nối tới
var seedNodes = new List<string> { "127.0.0.1:8888", "127.0.0.1:8889" };

// Tạo một instance của P2PNode với tên "Node C" và danh sách các seed nodes.
// Đã sửa lỗi: Thêm seedNodes vào hàm khởi tạo
var p2pNode = new P2PNode(blockchain, "Node C", seedNodes);

// Bắt đầu node, chỉ truyền port.
// Đã sửa lỗi: Loại bỏ danh sách seedNodes khỏi phương thức Start
p2pNode.Start(8890);

Console.WriteLine("Node C đã khởi động và kết nối tới Node A và Node B.");
Console.WriteLine("---------------------------------------------");
Console.WriteLine("Nhập 'mine' để đào các giao dịch đang chờ và phát tán block mới.");
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
                        // Phát tán block này tới các node khác trong mạng lưới
                        // Đã sửa lỗi: Chỉ truyền block, không truyền tên node
                        p2pNode.BroadcastBlock(newBlock);
                        Console.WriteLine($"[Node C] Đã đào thành công một block mới và phát tán tới các node còn lại.");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"[Node C] Lỗi: {ex.Message}");
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
                        FromAddress = "UserC", // Địa chỉ giả lập
                        ToAddress = "UserA",    // Địa chỉ giả lập
                        Data = transactionData,
                        Timestamp = DateTime.UtcNow,
                        Signature = "DummySignature" // Giả lập chữ ký để qua bước xác thực đơn giản
                    };
                    p2pNode.CreateAndBroadcastTransaction(newTransaction);
                    Console.WriteLine($"[Node C] Đã tạo và phát tán giao dịch mới với ID: {newTransaction.TransactionId}. Bây giờ bạn có thể gõ 'mine' để đào block mới.");
                }
                break;
            default:
                Console.WriteLine("Lệnh không hợp lệ. Vui lòng nhập 'mine' hoặc 'add'.");
                break;
        }
    }
}
