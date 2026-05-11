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

            var overdueTransactions = await _context.BorrowTransactions
                .Include(t => t.User)
                .Include(t => t.Book)
                .Where(t => t.ReturnDate == null && t.DueDate < DateTime.Now)
                .OrderBy(t => t.DueDate)
                .Take(5)
                .Select(t => new OverdueTransactionViewModel
                {
                    BorrowerName = t.User.FullName ?? "N/A",
                    BookTitle = t.Book.Title,
                    DueDate = t.DueDate,
                    DaysOverdue = (DateTime.Now - t.DueDate).Days
                }).ToListAsync();

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var topCategories = await _context.BorrowTransactions
                .Where(t => t.BorrowDate >= thirtyDaysAgo && t.Book != null && t.Book.Category != null)
                .GroupBy(t => t.Book!.Category!.CategoryName)
                .Select(g => new MostBorrowedCategoryViewModel
                {
                    CategoryName = g.Key ?? "Uncategorized",
                    BorrowCount = g.Count()
                })
                .OrderByDescending(x => x.BorrowCount)
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
                TopFineUsers = topFineUsers,
                OverdueTransactions = overdueTransactions,
                TopCategories = topCategories
            };

            return View(model);
        }

        public async Task<IActionResult> FineManagement()
        {
            var unpaidFines = await _fineService.GetUnpaidFinesAsync();

            return View(unpaidFines);
        }

        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.LogDate)
                .Take(200)
                .Select(a => new AuditLogViewModel
                {
                    AuditLogId = a.AuditLogId,
                    TableName = a.TableName,
                    ActionType = a.ActionType,
                    UserId = a.UserId,
                    UserEmail = a.User != null ? a.User.Email : null,
                    LogDate = a.LogDate,
                    Description = a.Description
                })
                .ToListAsync();

            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var paid = await _fineService.PayFineAsync(id);
            TempData[paid ? "SuccessMessage" : "ErrorMessage"] =
                paid ? "Fine marked as paid successfully." : "Fine was not found.";
            return RedirectToAction(nameof(FineManagement));
        }
    }
}
