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
Console.WriteLine("Nhập 'mine' để tạo một block mới và phát tán.");
Console.WriteLine("---------------------------------------------");

while (true)
{
    var input = Console.ReadLine();
    if (input != null && input.ToLower() == "mine")
    {
        // Tạo một block mới với dữ liệu giả lập từ Node B
        var newBlock = blockchain.AddBlock("Dữ liệu CV mới từ Node B");
        // Phát tán block này tới các node khác trong mạng lưới và gửi kèm tên của node
        p2pNode.BroadcastBlock(newBlock, "Node B");
        Console.WriteLine($"[Node B] Đã tạo 1 block mới và send broadcast tới các node còn lại.");
    }
}
