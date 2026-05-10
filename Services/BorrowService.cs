using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Services
{
    public class BorrowService : IBorrowService
    {
        private readonly ApplicationDbContext _context;
        private const decimal DailyFineRate = 1.00m; // Example: $1.00 per overdue day

        public BorrowService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Transaction>> GetAllTransactionsAsync()
        {
            return await _context.Transactions
                .Include(t => t.Book)
                .Include(t => t.User)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task RecordTransactionAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ProcessReturnAsync(int transactionId)
        {
            var transaction = await _context.BorrowTransactions
                .Include(bt => bt.Book)
                .Include(bt => bt.Fines)
                .FirstOrDefaultAsync(bt => bt.TransactionId == transactionId);

            if (transaction == null || transaction.ReturnDate != null)
            {
                return false;
            }

            // 1. Mark as returned
            var wasReserved = transaction.Status == "Reserved";
            transaction.ReturnDate = DateTime.Now;
            transaction.Status = wasReserved ? "Cancelled" : "Returned";

            // 2. Update Book Availability
            if (transaction.Book != null && transaction.Book.AvailableCopies < transaction.Book.TotalCopies)
            {
                transaction.Book.AvailableCopies++;
            }

            // 3. Calculate and apply fines
            var fineAmount = wasReserved ? 0 : CalculateOverdueFine(transaction);
            
            string logDescription = $"Book '{transaction.Book?.Title}' was {(wasReserved ? "cancelled" : "returned")}.";

            if (fineAmount > 0 && !transaction.Fines.Any(f => !f.IsPaid))
            {
                logDescription += $" Overdue fine of {fineAmount:C} was assessed.";

                var fine = new Fine
                {
                    TransactionId = transaction.TransactionId,
                    Amount = fineAmount,
                    IsPaid = false,
                    CreatedAt = DateTime.Now
                };
                _context.Fines.Add(fine);
                transaction.Status = "Returned (Overdue)";
            }
            else if (transaction.DueDate.Date < transaction.ReturnDate.Value.Date)
            {
                transaction.Status = "Returned (Overdue)";
            }

            // 4. Record System Transaction
            var log = new Transaction
            {
                BookId = transaction.BookId,
                UserId = transaction.UserId,
                TransactionType = wasReserved ? "Cancellation" : "Return",
                TransactionDate = DateTime.Now,
                Amount = fineAmount > 0 ? fineAmount : null,
                Description = logDescription
            };
            _context.Transactions.Add(log);

            return await _context.SaveChangesAsync() > 0;
        }

        public decimal CalculateOverdueFine(BorrowTransaction transaction)
        {
            var effectiveReturnDate = (transaction.ReturnDate ?? DateTime.Now).Date;
            
            if (effectiveReturnDate > transaction.DueDate.Date)
            {
                return (decimal)(effectiveReturnDate - transaction.DueDate.Date).Days * DailyFineRate;
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

            if (string.IsNullOrEmpty(transaction.Status))
            {
                transaction.Status = "Borrowed";
            }

            book.AvailableCopies--;
            _context.BorrowTransactions.Add(transaction);

            // Record System Transaction
            _context.Transactions.Add(new Transaction
            {
                BookId = transaction.BookId,
                UserId = transaction.UserId,
                TransactionType = transaction.Status == "Reserved" ? "Reservation" : "Borrow",
                TransactionDate = DateTime.Now,
                Description = $"Book '{book.Title}' was {(transaction.Status == "Reserved" ? "reserved" : "checked out")}."
            });

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ConfirmCheckoutAsync(int transactionId)
        {
            var transaction = await _context.BorrowTransactions
                .Include(bt => bt.Book)
                .FirstOrDefaultAsync(bt => bt.TransactionId == transactionId);

            if (transaction == null || transaction.Status != "Reserved")
            {
                return false;
            }

            transaction.Status = "Borrowed";
            transaction.BorrowDate = DateTime.Now;
            transaction.DueDate = DateTime.Now.AddDays(14); // Set standard loan period

            _context.Transactions.Add(new Transaction
            {
                BookId = transaction.BookId,
                UserId = transaction.UserId,
                TransactionType = "Borrow",
                TransactionDate = DateTime.Now,
                Description = $"Reservation for '{transaction.Book?.Title}' was collected/checked out."
            });

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
    }
}