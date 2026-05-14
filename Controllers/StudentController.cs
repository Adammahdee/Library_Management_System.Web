using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Student/Index (Student Portal)
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var now = DateTime.Now;

            // Summary Statistics for the Student Dashboard
            ViewBag.ActiveLoansCount = await _context.BorrowTransactions
                .CountAsync(t => t.UserId == userId && t.ReturnDate == null && (t.Status == "Borrowed" || t.Status == "Overdue"));

            ViewBag.OverdueCount = await _context.BorrowTransactions
                .CountAsync(t => t.UserId == userId && t.ReturnDate == null && t.DueDate < now);

            ViewBag.UnpaidFines = await _context.Fines
                .Include(f => f.BorrowTransaction)
                .Where(f => f.BorrowTransaction.UserId == userId && !f.IsPaid)
                .SumAsync(f => (decimal?)f.Amount) ?? 0;

            // Recent activity specific to the student
            var recentActivity = await _context.BorrowTransactions
                .Include(t => t.Book)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.BorrowDate)
                .Take(5)
                .ToListAsync();

            return View(recentActivity);
        }

        // GET: Student/MyBooks
        public async Task<IActionResult> MyBooks()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var activeLoans = await _context.BorrowTransactions
                .Include(t => t.Book)
                .Where(t => t.UserId == userId && t.ReturnDate == null)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return View(activeLoans);
        }

        // GET: Student/History
        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var history = await _context.BorrowTransactions
                .Include(t => t.Book)
                .Include(t => t.Fines)
                .Where(t => t.UserId == userId && t.ReturnDate != null)
                .OrderByDescending(t => t.ReturnDate)
                .ToListAsync();

            return View(history);
        }

        // GET: Student/Fines
        public async Task<IActionResult> Fines()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var fines = await _context.Fines
                .Include(f => f.BorrowTransaction)
                    .ThenInclude(bt => bt.Book)
                .Where(f => f.BorrowTransaction.UserId == userId)
                .OrderBy(f => f.IsPaid)
                .ThenByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(fines);
        }
    }
}