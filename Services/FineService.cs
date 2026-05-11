using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Library_Management_System.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Services
{
    public class FineService : IFineService
    {
        private readonly ApplicationDbContext _context;

        public FineService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task GenerateFineAsync(BorrowTransaction transaction)
        {
            await EnsureOverdueFineAsync(transaction, DateTime.Now, updateOpenLoanStatus: true);
        }

        public async Task<List<Fine>> GetAllFinesAsync()
        {
            return await _context.Fines
                .Include(f => f.BorrowTransaction)
                .ThenInclude(t => t.Book)
                .Include(f => f.BorrowTransaction)
                .ThenInclude(t => t.User)
                .ToListAsync();
        }

        public async Task<List<Fine>> GetUnpaidFinesAsync()
        {
            return await _context.Fines
                .Include(f => f.BorrowTransaction)
                    .ThenInclude(bt => bt.User)
                .Include(f => f.BorrowTransaction)
                    .ThenInclude(bt => bt.Book)
                .Where(f => !f.IsPaid)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> EnsureOverdueFineAsync(BorrowTransaction transaction, DateTime effectiveDate, bool updateOpenLoanStatus)
        {
            if (effectiveDate.Date <= transaction.DueDate.Date)
            {
                return 0m;
            }

            var overdueDays = (effectiveDate.Date - transaction.DueDate.Date).Days;
            var computedAmount = overdueDays * 1.00m;

            var existingUnpaidFine = await _context.Fines
                .FirstOrDefaultAsync(f => f.TransactionId == transaction.TransactionId && !f.IsPaid);

            if (existingUnpaidFine == null)
            {
                _context.Fines.Add(new Fine
                {
                    TransactionId = transaction.TransactionId,
                    Amount = computedAmount,
                    IsPaid = false,
                    CreatedAt = DateTime.Now
                });
            }

            if (updateOpenLoanStatus && transaction.ReturnDate == null)
            {
                transaction.Status = "Overdue";
            }

            await _context.SaveChangesAsync();
            return existingUnpaidFine?.Amount ?? computedAmount;
        }

        public async Task<bool> PayFineAsync(int fineId)
        {
            var fine = await _context.Fines.FindAsync(fineId);

            if (fine == null)
                return false;

            fine.IsPaid = true;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
