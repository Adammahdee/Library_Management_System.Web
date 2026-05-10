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
            transaction.ReturnDate = DateTime.UtcNow;
            transaction.Status = "Returned";

            // 2. Update Book Availability
            if (transaction.Book != null)
            {
                transaction.Book.AvailableCopies++;
            }

            // 3. Calculate and apply fines
            var fineAmount = CalculateOverdueFine(transaction);
            
            // Only create a new fine if one doesn't exist for this overdue return
            if (fineAmount > 0 && !transaction.Fines.Any(f => !f.IsPaid))
            {
                var fine = new Fine
                {
                    TransactionId = transaction.TransactionId,
                    Amount = fineAmount,
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Fines.Add(fine);
                transaction.Status = "Returned (Overdue)";
            }
            else if (transaction.DueDate < transaction.ReturnDate)
            {
                transaction.Status = "Returned (Overdue)";
            }

            return await _context.SaveChangesAsync() > 0;
        }

        public decimal CalculateOverdueFine(BorrowTransaction transaction)
        {
            var effectiveReturnDate = transaction.ReturnDate ?? DateTime.UtcNow;
            
            if (effectiveReturnDate > transaction.DueDate)
            {
                return (decimal)(effectiveReturnDate - transaction.DueDate).Days * DailyFineRate;
            }
            return 0m;
        }
    }
}