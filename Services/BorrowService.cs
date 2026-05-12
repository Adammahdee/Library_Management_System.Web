using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Library_Management_System.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Services
{
    public class BorrowService : IBorrowService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFineService _fineService;

        public BorrowService(ApplicationDbContext context, IFineService fineService)
        {
            _context = context;
            _fineService = fineService;
        }

        public async Task<List<BorrowTransaction>> GetTransactionHistoryAsync()
        {
            return await _context.BorrowTransactions
                .Include(bt => bt.User)
                .Include(bt => bt.Book)
                .Include(bt => bt.Fines)
                .OrderByDescending(bt => bt.BorrowDate)
                .ToListAsync();
        }

        public async Task<BorrowTransaction?> GetTransactionByIdAsync(int transactionId)
        {
            return await _context.BorrowTransactions
                .Include(bt => bt.User)
                .Include(bt => bt.Book)
                .Include(bt => bt.Fines)
                .FirstOrDefaultAsync(bt => bt.TransactionId == transactionId);
        }

        public async Task<bool> ProcessReturnAsync(int transactionId)
        {
            var transaction = await GetTransactionByIdAsync(transactionId);

            if (transaction == null || transaction.ReturnDate != null)
            {
                return false;
            }

            var wasReserved = transaction.Status == "Reserved";
            transaction.ReturnDate = DateTime.Now;
            transaction.Status = wasReserved ? "Cancelled" : "Returned";

            if (transaction.Book != null && transaction.Book.AvailableCopies < transaction.Book.TotalCopies)
            {
                transaction.Book.AvailableCopies++;
            }

            if (!wasReserved && transaction.DueDate.Date < transaction.ReturnDate.Value.Date)
            {
                await _fineService.EnsureOverdueFineAsync(transaction, transaction.ReturnDate.Value, updateOpenLoanStatus: false);
                transaction.Status = "Returned (Overdue)";
            }

            return await _context.SaveChangesAsync() > 0;
        }

        public decimal CalculateOverdueFine(BorrowTransaction transaction)
        {
            var effectiveReturnDate = (transaction.ReturnDate ?? DateTime.Now).Date;
            if (effectiveReturnDate > transaction.DueDate.Date)
            {
                return (effectiveReturnDate - transaction.DueDate.Date).Days * 1.00m;
            }

            return 0m;
        }

        public async Task<bool> CheckoutAsync(BorrowTransaction transaction)
        {
            var book = await _context.Books.FindAsync(transaction.BookId);
            if (book == null || book.AvailableCopies <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(transaction.Status))
            {
                transaction.Status = "Borrowed";
            }

            book.AvailableCopies--;
            _context.BorrowTransactions.Add(transaction);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ConfirmCheckoutAsync(int transactionId)
        {
            var transaction = await _context.BorrowTransactions
                .FirstOrDefaultAsync(bt => bt.TransactionId == transactionId);

            if (transaction == null || transaction.Status != "Reserved")
            {
                return false;
            }

            transaction.Status = "Borrowed";
            transaction.BorrowDate = DateTime.Now;
            transaction.DueDate = DateTime.Now.AddDays(14);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<BorrowTransaction>> GetActiveLoansAsync(string userId)
        {
            return await _context.BorrowTransactions
                .Include(bt => bt.Book)
                .Where(bt => bt.UserId == userId && bt.ReturnDate == null && (bt.Status == "Borrowed" || bt.Status == "Overdue"))
                .OrderByDescending(bt => bt.BorrowDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateTransactionAsync(BorrowTransaction transaction)
        {
            var existing = await _context.BorrowTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(bt => bt.TransactionId == transaction.TransactionId);

            if (existing == null)
            {
                return false;
            }

            _context.BorrowTransactions.Update(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> RunOverdueAutomationAsync()
        {
            var now = DateTime.Now;
            var overdueTransactions = await _context.BorrowTransactions
                .Include(bt => bt.Book)
                .Where(bt => bt.ReturnDate == null && bt.DueDate < now && bt.Status != "Overdue")
                .ToListAsync();

            foreach (var transaction in overdueTransactions)
            {
                await _fineService.EnsureOverdueFineAsync(transaction, now, updateOpenLoanStatus: true);
                var alreadyNotified = await _context.Notifications.AnyAsync(n =>
                    n.UserId == transaction.UserId &&
                    n.Title == "Overdue Reminder" &&
                    n.CreatedAt.Date == now.Date);

                if (!alreadyNotified)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = transaction.UserId,
                        Title = "Overdue Reminder",
                        Message = $"Your borrowed book '{transaction.Book?.Title}' is overdue. Please return it as soon as possible.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            var dueSoon = await _context.BorrowTransactions
                .Include(bt => bt.Book)
                .Where(bt => bt.ReturnDate == null && bt.DueDate.Date == now.Date.AddDays(1))
                .ToListAsync();

            foreach (var transaction in dueSoon)
            {
                var alreadyNotified = await _context.Notifications.AnyAsync(n =>
                    n.UserId == transaction.UserId &&
                    n.Title == "Return Reminder" &&
                    n.CreatedAt.Date == now.Date);

                if (!alreadyNotified)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = transaction.UserId,
                        Title = "Return Reminder",
                        Message = $"Reminder: '{transaction.Book?.Title}' is due tomorrow ({transaction.DueDate:yyyy-MM-dd}).",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            return overdueTransactions.Count;
        }
    }
}
