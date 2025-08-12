using BlockchainCore;
using System.Text;

// Đặt encoding để hiển thị tiếng Việt chính xác
Console.OutputEncoding = Encoding.UTF8;

Console.Title = "Node C";

// Tạo một instance của BlockchainService.
var blockchain = new BlockchainService();

// Tạo một instance của P2PNode với tên "Node C", kết nối tới Node A và Node B
var p2pNode = new P2PNode(blockchain, "Node C");
p2pNode.Start(8890, new List<string> { "127.0.0.1:8888", "127.0.0.1:8889" });

Console.WriteLine("Node C đã khởi động và kết nối tới Node A và Node B.");
Console.WriteLine("Nhập 'mine' để đào các giao dịch đang chờ và phát tán block mới.");
Console.WriteLine("---------------------------------------------");

while (true)
{
    var input = Console.ReadLine();
    if (input != null && input.ToLower() == "mine")
    {
        // Gọi phương thức MinePendingTransactions, giờ có thể trả về null
        var newBlock = blockchain.MinePendingTransactions();

        if (newBlock != null)
        {
            // Chỉ phát tán block nếu việc đào thành công
            p2pNode.BroadcastBlock(newBlock, "Node C");
            Console.WriteLine($"[Node C] Đã đào thành công một block mới và phát tán tới các node còn lại.");
        }
        else
        {
            // Hiển thị thông báo nếu không có giao dịch nào
            Console.WriteLine("[Node C] Lỗi: Không có giao dịch nào đang chờ để đào.");
        }
    }
}
