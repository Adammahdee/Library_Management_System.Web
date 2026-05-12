using Library_Management_System.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library_Management_System.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Publisher> Publishers => Set<Publisher>();
        public DbSet<Author> Authors => Set<Author>();
        public DbSet<Book> Books => Set<Book>();
        public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
        public DbSet<BorrowTransaction> BorrowTransactions => Set<BorrowTransaction>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<Fine> Fines => Set<Fine>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in entries)
            {
                if (entry.Entity is AuditLog) continue;

                var log = new AuditLog
                {
                    TableName = entry.Entity.GetType().Name,
                    ActionType = entry.State.ToString(),
                    LogDate = DateTime.UtcNow,
                    UserId = userId,
                    Description = $"Changed {entry.Entity.GetType().Name}"
                };

                // Optionally capture primary key or specific field changes here
                AuditLogs.Add(log);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FullName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(u => u.StudentNumber)
                    .HasMaxLength(50);

                entity.Property(u => u.StaffNumber)
                    .HasMaxLength(50);

                entity.Property(u => u.CreatedAt)
                    .IsRequired();

                entity.HasOne(u => u.Department)
                    .WithMany(d => d.Users)
                    .HasForeignKey(u => u.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.Property(d => d.DepartmentName)
                    .HasMaxLength(150)
                    .IsRequired();
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(c => c.CategoryName)
                    .HasMaxLength(150)
                    .IsRequired();
            });

            modelBuilder.Entity<Publisher>(entity =>
            {
                entity.Property(p => p.PublisherName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(p => p.Address)
                    .HasMaxLength(255);

                entity.Property(p => p.PhoneNumber)
                    .HasMaxLength(30);

                entity.Property(p => p.Email)
                    .HasMaxLength(256);
            });

            modelBuilder.Entity<Author>(entity =>
            {
                entity.Property(a => a.AuthorName)
                    .HasMaxLength(150)
                    .IsRequired();
            });

            modelBuilder.Entity<Book>(entity =>
            {
                entity.Property(b => b.Title)
                    .HasMaxLength(250)
                    .IsRequired();

                entity.Property(b => b.ISBN)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasIndex(b => b.ISBN)
                    .IsUnique();

                entity.HasOne(b => b.Category)
                    .WithMany(c => c.Books)
                    .HasForeignKey(b => b.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Publisher)
                    .WithMany(p => p.Books)
                    .HasForeignKey(b => b.PublisherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<BookAuthor>(entity =>
            {
                entity.HasKey(ba => new { ba.BookId, ba.AuthorId });

                entity.HasOne(ba => ba.Book)
                    .WithMany(b => b.BookAuthors)
                    .HasForeignKey(ba => ba.BookId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ba => ba.Author)
                    .WithMany(a => a.BookAuthors)
                    .HasForeignKey(ba => ba.AuthorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BorrowTransaction>(entity =>
            {
                entity.HasKey(bt => bt.TransactionId);

                entity.Property(bt => bt.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasOne(bt => bt.User)
                    .WithMany(u => u.BorrowTransactions)
                    .HasForeignKey(bt => bt.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(bt => bt.Book)
                    .WithMany(b => b.BorrowTransactions)
                    .HasForeignKey(bt => bt.BookId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.Property(r => r.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasOne(r => r.User)
                    .WithMany(u => u.Reservations)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Book)
                    .WithMany(b => b.Reservations)
                    .HasForeignKey(r => r.BookId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Fine>(entity =>
            {
                entity.Property(f => f.Amount)
                    .HasPrecision(10, 2);

                entity.Property(e => e.IsPaid)
                    .HasDefaultValue(false);

                entity.HasOne(f => f.BorrowTransaction)
                    .WithMany(bt => bt.Fines)
                    .HasForeignKey(f => f.TransactionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(n => n.Title)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(n => n.Message)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.Property(al => al.ActionType)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(al => al.TableName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(al => al.LogDate)
                    .IsRequired();

                entity.Property(al => al.Description)
                    .HasMaxLength(1000);

                entity.HasOne(al => al.User)
                    .WithMany()
                    .HasForeignKey(al => al.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
