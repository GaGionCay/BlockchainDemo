using BlockchainCore;
using System.Text;

// Đặt encoding để hiển thị tiếng Việt chính xác
Console.OutputEncoding = Encoding.UTF8;

Console.Title = "Node B";

// Tạo một instance của BlockchainService.
// Chuỗi blockchain sẽ được khởi tạo mới với một genesis block mỗi khi ứng dụng khởi động.
var blockchain = new BlockchainService();

// Tạo một instance của P2PNode với tên là "Node B", kết nối tới Node A và Node C
var p2pNode = new P2PNode(blockchain, "Node B");
p2pNode.Start(8889, new List<string> { "127.0.0.1:8888", "127.0.0.1:8890" });

Console.WriteLine("Node B đã khởi động và kết nối tới Node A và Node C.");
Console.WriteLine("Nhập 'mine' để đào các giao dịch đang chờ và phát tán block mới.");
Console.WriteLine("---------------------------------------------");

while (true)
{
    var input = Console.ReadLine();
    if (input != null && input.ToLower() == "mine")
    {
        try
        {
            // Đào một block mới từ các giao dịch đang chờ
            var newBlock = blockchain.MinePendingTransactions();
            // Phát tán block này tới các node khác trong mạng lưới và gửi kèm tên của node
            p2pNode.BroadcastBlock(newBlock, "Node B");
            Console.WriteLine($"[Node B] Đã đào thành công một block mới và phát tán tới các node còn lại.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[Node B] Lỗi: {ex.Message}");
        }
    }
}
