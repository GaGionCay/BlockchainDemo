using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainCore.Models
{
    public class Message
    {
        public string Type { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
    }
}
