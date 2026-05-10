using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library_Management_System.Web.Services
{
    public class TransactionService
    {
        private readonly ApplicationDbContext _context;

        public TransactionService(ApplicationDbContext context)
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
    }
}