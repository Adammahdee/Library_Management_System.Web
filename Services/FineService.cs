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
            if (transaction.ReturnDate != null)
                return;

            if (DateTime.Now.Date <= transaction.DueDate.Date)
                return;

            bool fineExists = await _context.Fines
                .AnyAsync(f => f.TransactionId == transaction.TransactionId);

            if (fineExists)
                return;

            int overdueDays =
                (DateTime.Now.Date - transaction.DueDate.Date).Days;

            decimal amount = overdueDays * 1.00m;

            var fine = new Fine
            {
                TransactionId = transaction.TransactionId,
                Amount = amount,
                IsPaid = false
            };

            _context.Fines.Add(fine);

            transaction.Status = "Overdue";

            await _context.SaveChangesAsync();
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

        public async Task PayFineAsync(int fineId)
        {
            var fine = await _context.Fines.FindAsync(fineId);

            if (fine == null)
                return;

            fine.IsPaid = true;

            await _context.SaveChangesAsync();
        }
    }
}