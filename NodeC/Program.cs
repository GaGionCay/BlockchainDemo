using BlockchainCore;
using System.Text;

// Đặt encoding để hiển thị tiếng Việt chính xác
Console.OutputEncoding = Encoding.UTF8;

Console.Title = "Node C";

// Tạo một instance của BlockchainService.
// Chuỗi blockchain sẽ được khởi tạo mới với một genesis block mỗi khi ứng dụng khởi động.
var blockchain = new BlockchainService();

// Tạo một instance của P2PNode với tên "Node C", kết nối tới Node A và Node B
var p2pNode = new P2PNode(blockchain, "Node C");
p2pNode.Start(8890, new List<string> { "127.0.0.1:8888", "127.0.0.1:8889" });

Console.WriteLine("Node C đã khởi động và kết nối tới Node A và Node B.");
Console.WriteLine("Nhập 'mine' để tạo một block mới và phát tán.");
Console.WriteLine("---------------------------------------------");

while (true)
{
    var input = Console.ReadLine();
    if (input != null && input.ToLower() == "mine")
    {
        // Tạo một block mới với dữ liệu giả lập từ Node C
        var newBlock = blockchain.AddBlock("Dữ liệu CV mới từ Node C");
        // Phát tán block này tới các node khác trong mạng lưới và gửi kèm tên của node
        p2pNode.BroadcastBlock(newBlock, "Node C");
        Console.WriteLine($"[Node C] Đã tạo 1 block mới và send broadcast tới các node còn lại.");
    }
}
