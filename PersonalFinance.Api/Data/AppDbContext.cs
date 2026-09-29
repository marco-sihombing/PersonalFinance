using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Entities;

namespace PersonalFinance.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<FinancialGoal> FinancialGoals => Set<FinancialGoal>();
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(u => u.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired(false);

            entity.Property(u => u.GoogleId)
                .HasColumnName("google_id")
                .HasMaxLength(255)
                .IsRequired(false);

            entity.HasIndex(u => u.GoogleId)
                .IsUnique();

            entity.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(c => c.UserId)
                .HasColumnName("user_id");

            entity.Property(c => c.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.CategoryType)
                .HasColumnName("category_type")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(c => c.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(c => c.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Category>()
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.UserId);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(a => a.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(a => a.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(a => a.AccountType)
                .HasColumnName("account_type")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(a => a.Balance)
                .HasColumnName("balance")
                .HasPrecision(15, 2)
                .HasDefaultValue(0);

            entity.Property(a => a.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(a => a.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => a.UserId);
        });
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions", table =>
            {
            table.HasCheckConstraint(
                "chk_transaction_type",
                "transaction_type IN ('income', 'expense')"
            );

            table.HasCheckConstraint(
                "chk_transaction_amount",
                "amount > 0"
            );
        });

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(t => t.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(t => t.AccountId)
                .HasColumnName("account_id")
                .IsRequired();

            entity.Property(t => t.CategoryId)
                .HasColumnName("category_id")
                .IsRequired();

            entity.Property(t => t.TransactionType)
                .HasColumnName("transaction_type")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(t => t.Amount)
                .HasColumnName("amount")
                .HasPrecision(15, 2)
                .IsRequired();

            entity.Property(t => t.TransactionDate)
                .HasColumnName("transaction_date")
                .IsRequired();

            entity.Property(t => t.Description)
                .HasColumnName("description")
                .IsRequired(false);

            entity.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(t => t.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // User relationship
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Account relationship
            entity.HasOne<Account>()
                .WithMany()
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // Category relationship
            entity.HasOne<Category>()
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(t => t.UserId);
            entity.HasIndex(t => t.AccountId);
            entity.HasIndex(t => t.CategoryId);
            entity.HasIndex(t => t.TransactionDate);
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable("budgets", table =>
            {
                table.HasCheckConstraint(
                    "chk_budget_amount",
                    "amount > 0"
                );

                table.HasCheckConstraint(
                    "chk_budget_period",
                    "period_end >= period_start"
                );
            });

            entity.HasKey(b => b.Id);

            entity.Property(b => b.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(b => b.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(b => b.CategoryId)
                .HasColumnName("category_id")
                .IsRequired();

            entity.Property(b => b.Amount)
                .HasColumnName("amount")
                .HasPrecision(15, 2)
                .IsRequired();

            entity.Property(b => b.PeriodStart)
                .HasColumnName("period_start")
                .HasColumnType("date")
                .IsRequired();

            entity.Property(b => b.PeriodEnd)
                .HasColumnName("period_end")
                .HasColumnType("date")
                .IsRequired();

            entity.Property(b => b.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // User
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Category
            entity.HasOne(b => b.Category)
                .WithMany()
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(b => b.UserId);
            entity.HasIndex(b => b.CategoryId);

            entity.HasIndex(b => new
            {
                b.UserId,
                b.PeriodStart,
                b.PeriodEnd
            });
        });

        modelBuilder.Entity<FinancialGoal>(entity =>
        {
            entity.ToTable("financial_goals", table =>
            {
                table.HasCheckConstraint(
                    "chk_goal_target",
                    "target_amount > 0"
                );

                table.HasCheckConstraint(
                    "chk_goal_current",
                    "current_amount >= 0"
                );
            });

            entity.HasKey(g => g.Id);

            entity.Property(g => g.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(g => g.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(g => g.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(g => g.TargetAmount)
                .HasColumnName("target_amount")
                .HasPrecision(15, 2)
                .IsRequired();

            entity.Property(g => g.CurrentAmount)
                .HasColumnName("current_amount")
                .HasPrecision(15, 2)
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(g => g.TargetDate)
                .HasColumnName("target_date")
                .HasColumnType("date")
                .IsRequired(false);

            entity.Property(g => g.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(g => g.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(g => g.UserId);

            entity.HasIndex(g => g.TargetDate);
        }); 
    }
}