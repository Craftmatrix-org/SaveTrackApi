using Microsoft.EntityFrameworkCore;
using Craftmatrix.org.Model;

namespace Craftmatrix.org.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UserDto> Users { get; set; } = null!;
        public DbSet<AccountDto> Accounts { get; set; } = null!;
        public DbSet<CategoryDto> Categories { get; set; } = null!;
        public DbSet<TransactionDto> Transactions { get; set; } = null!;
        public DbSet<BudgetDto> Budgets { get; set; } = null!;
        public DbSet<BudgetItemDto> BudgetItems { get; set; } = null!;
        public DbSet<TransferDto> Transfers { get; set; } = null!;
        public DbSet<ReportDto> Reports { get; set; } = null!;
        public DbSet<WishListParentDto> WishListParents { get; set; } = null!;
        public DbSet<WishListDto> WishLists { get; set; } = null!;

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

            modelBuilder.Entity<TransferDto>()
                .HasOne<UserDto>()
                .WithMany()
                .HasForeignKey(t => t.UserID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferDto>()
                .HasOne<AccountDto>()
                .WithMany()
                .HasForeignKey(t => t.AccountID_A)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferDto>()
                .HasOne<AccountDto>()
                .WithMany()
                .HasForeignKey(t => t.AccountID_B)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReportDto>()
                .HasOne<AccountDto>()
                .WithMany()
                .HasForeignKey(t => t.UserID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WishListParentDto>()
                .HasOne<AccountDto>()
                .WithMany()
                .HasForeignKey(t => t.UserID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WishListDto>()
                .HasOne<AccountDto>()
                .WithMany()
                .HasForeignKey(t => t.UserID)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WishListDto>()
                .HasOne<WishListParentDto>()
                .WithMany()
                .HasForeignKey(t => t.ParentId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
