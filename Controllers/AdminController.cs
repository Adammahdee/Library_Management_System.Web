using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Library_Management_System.Web.Models.ViewModels;
using Library_Management_System.Web.Services.Interfaces;

namespace Library_Management_System.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFineService _fineService;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IFineService fineService)
        {
            _context = context;
            _userManager = userManager;
            _fineService = fineService;
        }

        public async Task<IActionResult> Index()
        {
            var totalBooks = await _context.Books.CountAsync();
            var totalUsers = await _userManager.Users.CountAsync();
            
            var borrowedBooks = await _context.BorrowTransactions
                .CountAsync(t => t.Status == "Borrowed");

            var returnedBooks = await _context.BorrowTransactions
                .CountAsync(t => t.Status == "Returned");

            var overdueBooks = await _context.BorrowTransactions
                .CountAsync(t => t.ReturnDate == null && t.DueDate < DateTime.Now);

            var pendingFinesCount = await _context.Fines
                .CountAsync(f => !f.IsPaid);

            var totalFineAmount = await _context.Fines
                .Where(f => !f.IsPaid)
                .SumAsync(f => (decimal?)f.Amount) ?? 0;

            // Calculate monthly revenue for the last 6 months
            var lastSixMonths = DateTime.Now.AddMonths(-5);
            var monthlyRevenueQuery = await _context.Fines
                .Where(f => f.IsPaid && f.CreatedAt >= new DateTime(lastSixMonths.Year, lastSixMonths.Month, 1))
                .GroupBy(f => new { f.CreatedAt.Year, f.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(f => f.Amount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var revenueData = monthlyRevenueQuery.Select(x => new MonthlyRevenueViewModel
            {
                Month = new DateTime(x.Year, x.Month, 1).ToString("MMM"),
                Revenue = x.Revenue
            }).ToList();

            var topFineUsers = await _context.Fines
                .Where(f => !f.IsPaid)
                .GroupBy(f => new { f.BorrowTransaction.User.FullName, f.BorrowTransaction.User.Email })
                .Select(g => new UserFineSummaryViewModel
                {
                    FullName = g.Key.FullName ?? "N/A",
                    Email = g.Key.Email ?? "N/A",
                    TotalFineAmount = g.Sum(f => f.Amount)
                })
                .OrderByDescending(x => x.TotalFineAmount)
                .Take(5)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalBooks = totalBooks,
                TotalUsers = totalUsers,
                TotalBorrowedBooks = borrowedBooks,
                ReturnedBooks = returnedBooks,
                OverdueBooks = overdueBooks,
                PendingFines = pendingFinesCount,
                TotalFineAmount = totalFineAmount,
                MonthlyFineRevenue = revenueData,
                TopFineUsers = topFineUsers
            };

            return View(model);
        }

        public async Task<IActionResult> FineManagement()
        {
            var unpaidFines = await _context.Fines
                .Include(f => f.BorrowTransaction)
                    .ThenInclude(bt => bt.User)
                .Include(f => f.BorrowTransaction)
                    .ThenInclude(bt => bt.Book)
                .Where(f => !f.IsPaid)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(unpaidFines);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            await _fineService.PayFineAsync(id);
            TempData["SuccessMessage"] = "Fine marked as paid successfully.";
            return RedirectToAction(nameof(FineManagement));
        }
    }
}