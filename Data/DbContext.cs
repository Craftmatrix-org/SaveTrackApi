using Microsoft.EntityFrameworkCore;
using Craftmatrix.org.Model;

namespace Craftmatrix.org.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UserDto> Users { get; set; }
        public DbSet<AccountDto> Accounts { get; set; }
        // Add DbSet properties for your entities here
        // public DbSet<YourEntity> YourEntities { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountDto>()
                .HasOne<UserDto>()
                .WithMany()
                .HasForeignKey(a => a.UserID)
                .HasPrincipalKey(u => u.Id);

            modelBuilder.Entity<CategoryDto>()
                .HasOne<UserDto>()
                .WithMany()
                .HasForeignKey(c => c.UserID)
                .HasPrincipalKey(u => u.Id);

            modelBuilder.Entity<TransactionDto>()
                .HasOne<UserDto>()
                .WithMany()
                .HasForeignKey(c => c.UserID)
                .HasPrincipalKey(u => u.Id);

            modelBuilder.Entity<TransactionDto>()
                .HasOne<AccountDto>()
                .WithMany()
                .HasForeignKey(u => u.AccountID)
                .HasPrincipalKey(u => u.Id);

            modelBuilder.Entity<TransactionDto>()
                .HasOne<CategoryDto>()
                .WithMany()
                .HasForeignKey(u => u.CategoryID)
                .HasPrincipalKey(u => u.Id);
        }
    }
}
