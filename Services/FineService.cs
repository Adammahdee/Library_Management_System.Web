using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Library_Management_System.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Services
{
    public class FineService : IFineService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly decimal _dailyRate;
        private readonly int _graceDays;

        public FineService(ApplicationDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _dailyRate = configuration.GetValue<decimal?>("FineRules:DailyRate") ?? 1.0m;
            _graceDays = configuration.GetValue<int?>("FineRules:GraceDays") ?? 0;
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
            var effectiveDueDate = transaction.DueDate.Date.AddDays(_graceDays);
            if (effectiveDate.Date <= effectiveDueDate)
            {
                return 0m;
            }

            var overdueDays = (effectiveDate.Date - effectiveDueDate).Days;
            var computedAmount = overdueDays * _dailyRate;

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
            _context.AuditLogs.Add(new AuditLog
            {
                TableName = nameof(Fine),
                ActionType = "FinePaid",
                LogDate = DateTime.UtcNow,
                UserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                Description = $"Fine receipt generated for FineId={fine.FineId}, Amount={fine.Amount:0.00}, TransactionId={fine.TransactionId}"
            });

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
