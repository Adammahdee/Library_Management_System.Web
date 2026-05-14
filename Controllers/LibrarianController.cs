using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library_Management_System.Web.Data;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Library_Management_System.Web.Controllers
{
    [Authorize(Roles = "Librarian,Admin")]
    public class LibrarianController : Controller
    {
        // Implementation for Librarian Dashboard
        private readonly ApplicationDbContext _context;

        public LibrarianController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            
            ViewBag.TotalBooks = await _context.Books.CountAsync();
            ViewBag.BorrowedBooks = await _context.BorrowTransactions.CountAsync(t => t.Status == "Borrowed");
            ViewBag.OverdueBooks = await _context.BorrowTransactions.CountAsync(t => t.ReturnDate == null && t.DueDate < now);
            ViewBag.PendingFines = await _context.Fines.CountAsync(f => !f.IsPaid);
            ViewBag.TotalFineAmount = await _context.Fines.Where(f => !f.IsPaid).SumAsync(f => (decimal?)f.Amount) ?? 0;

            // Recent operational activities
            var recentTransactions = await _context.BorrowTransactions
                .Include(t => t.Book)
                .Include(t => t.User)
                .OrderByDescending(t => t.BorrowDate)
                .Take(10)
                .ToListAsync();

            return View(recentTransactions);
        }
    }
}