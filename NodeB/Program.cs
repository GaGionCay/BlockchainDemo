using BlockchainCore;
using System.Text;

// Đặt encoding để hiển thị tiếng Việt chính xác
Console.OutputEncoding = Encoding.UTF8;
Console.Title = "Node B";

var blockchain = new BlockchainService();
var seedNodes = new List<string> { "127.0.0.1:8888", "127.0.0.1:8890" };
var p2pNode = new P2PNode(blockchain, "Node B", seedNodes);
p2pNode.Start(8889);

Console.WriteLine("Node B đã khởi động và kết nối tới Node A và Node C.");
Console.WriteLine("---------------------------------------------");
// Cập nhật hướng dẫn: Không còn lệnh 'mine' nữa
Console.WriteLine("Hệ thống sẽ tự động đào khi có giao dịch mới.");
Console.WriteLine("Nhập 'add' để tạo một giao dịch mới.");
Console.WriteLine("---------------------------------------------");

while (true)
{
    var input = Console.ReadLine();
    if (input != null)
    {
        switch (input.ToLower())
        {
            // LOẠI BỎ case "mine" VÌ ĐÃ TỰ ĐỘNG HÓA

            case "add":
                // Giữ lại logic tạo giao dịch để kiểm thử
                Console.WriteLine("Nhập dữ liệu cho giao dịch mới (ví dụ: 'Node B gửi 5 coins'):");
                var transactionData = Console.ReadLine();
                if (!string.IsNullOrEmpty(transactionData))
                {
                    var newTransaction = new Transaction
                    {
                        TransactionId = Guid.NewGuid().ToString(),
                        FromAddress = "UserB",
                        ToAddress = "UserC",
                        Data = transactionData,
                        Timestamp = DateTime.UtcNow,
                        Signature = "DummySignature"
                    };
                    p2pNode.CreateAndBroadcastTransaction(newTransaction);
                    Console.WriteLine($"[Node B] Đã tạo và phát tán giao dịch mới. Chờ hệ thống tự động đào...");
                }
                break;

            default:
                Console.WriteLine("Lệnh không hợp lệ. Vui lòng nhập 'add'.");
                break;
        }
    }
}