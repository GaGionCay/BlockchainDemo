using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainCore.Models
{
    public class Transaction
    {
        public string FromAddress { get; set; } = string.Empty;
        public string ToAddress { get; set; } = string.Empty;
        public string TransactionId { get; set; } = Guid.NewGuid().ToString();
        public string Data { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
