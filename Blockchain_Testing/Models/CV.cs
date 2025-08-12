using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blockchain_Testing.Models
{
    public class CV
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        // Hash của toàn bộ CV, được lưu trên blockchain
        public string BlockchainHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Thêm thuộc tính ModifiedAt để theo dõi lần chỉnh sửa gần nhất
        public DateTime? ModifiedAt { get; set; }

        // Khóa ngoại liên kết với User
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        // Danh sách các mục kinh nghiệm làm việc
        public List<Experience> Experiences { get; set; } = new List<Experience>();

        // Danh sách các mục học vấn
        public List<Education> Educations { get; set; } = new List<Education>();

        // Thêm trường Public Key để xác minh chữ ký số
        public string? PublicKey { get; set; }

        // Thêm trường Signature để lưu chữ ký của block
        public string? Signature { get; set; }

        // Thêm trường để liên kết với giao dịch trên blockchain
        public string? TransactionId { get; set; }

        // Thêm trường để theo dõi trạng thái của CV (ví dụ: Pending, Mined)
        public string? Status { get; set; }
    }
}
