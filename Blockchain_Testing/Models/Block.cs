namespace Blockchain_Testing.Models
{
    public class Block
    {
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Data { get; set; }
        public string? PreviousHash { get; set; }
        public string? Hash { get; set; }
        public int Nonce { get; set; }
    }
}
