using Blockchain_Testing.Models;
using Microsoft.EntityFrameworkCore;

namespace Blockchain_Testing.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<CV> CVs { get; set; } // Thêm DbSet cho CV
        public DbSet<Experience> Experiences { get; set; } // Thêm DbSet cho Experience
        public DbSet<Education> Educations { get; set; } // Thêm DbSet cho Education

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình mối quan hệ 1-nhiều giữa User và CV
            modelBuilder.Entity<CV>()
                .HasOne(c => c.User)
                .WithMany(u => u.CVs) // Mối quan hệ với thuộc tính ICollection<CV> trong User
                .HasForeignKey(c => c.UserId);

            // Cấu hình mối quan hệ 1-nhiều giữa CV và Experience
            modelBuilder.Entity<Experience>()
                .HasOne(e => e.CV)
                .WithMany(c => c.Experiences)
                .HasForeignKey(e => e.CVId)
                .OnDelete(DeleteBehavior.Cascade); // Tự động xóa Experience khi CV bị xóa

            // Cấu hình mối quan hệ 1-nhiều giữa CV và Education
            modelBuilder.Entity<Education>()
                .HasOne(e => e.CV)
                .WithMany(c => c.Educations)
                .HasForeignKey(e => e.CVId)
                .OnDelete(DeleteBehavior.Cascade); // Tự động xóa Education khi CV bị xóa

            // Cấu hình cho thuộc tính BlockchainHash trong User để cho phép null
            modelBuilder.Entity<User>()
                .Property(u => u.BlockchainHash)
                .IsRequired(false);
        }
    }
}
