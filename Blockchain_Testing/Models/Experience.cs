using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Blockchain_Testing.Models
{
    public class Experience
    {
        [Key]
        public int Id { get; set; }

        public string Company { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; } = string.Empty;

        // Khóa ngoại liên kết với CV
        public int CVId { get; set; }

        [ForeignKey("CVId")]
        [JsonIgnore]
        public CV CV { get; set; } = null!;
    }
}
