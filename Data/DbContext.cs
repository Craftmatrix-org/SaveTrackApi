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
        public DbSet<CategoryDto> Categories { get; set; }
        public DbSet<TransactionDto> Transactions { get; set; }
        public DbSet<BudgetDto> Budgets { get; set; }
        public DbSet<BudgetItemDto> BudgetItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountDto>()
                .HasOne<UserDto>()
                .WithMany()
                .HasForeignKey(a => a.UserID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CategoryDto>()
                .HasOne<UserDto>()
                .WithMany()
                .HasForeignKey(c => c.UserID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionDto>()
                .HasOne<UserDto>()
                .WithMany()
                .HasForeignKey(c => c.UserID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionDto>()
                .HasOne<AccountDto>()
                .WithMany()
                .HasForeignKey(u => u.AccountID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionDto>()
                .HasOne<CategoryDto>()
                .WithMany()
                .HasForeignKey(u => u.CategoryID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BudgetDto>()
                .HasOne<UserDto>()
                .WithMany()
                .HasForeignKey(b => b.UserID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BudgetItemDto>()
                .HasOne<UserDto>()
                .WithMany()
                .HasForeignKey(b => b.UserID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BudgetItemDto>()
                .HasOne<BudgetDto>()
                .WithMany()
                .HasForeignKey(b => b.BudgetID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
