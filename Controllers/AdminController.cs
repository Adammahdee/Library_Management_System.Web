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
    [Authorize(Roles = "Admin,Librarian")]
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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            // Section 1: Base statistics
            var totalBooks = await _context.Books.CountAsync();
            var totalUsers = await _userManager.Users.CountAsync();

            var now = DateTime.Now;

            var borrowedBooks = await _context.BorrowTransactions.CountAsync(t => t.Status == "Borrowed");
            var returnedBooks = await _context.BorrowTransactions.CountAsync(t => t.Status == "Returned");
            var pendingFinesCount = await _context.Fines.CountAsync(f => !f.IsPaid);
            var totalFineAmount = await _context.Fines.Where(f => !f.IsPaid).SumAsync(f => (decimal?)f.Amount) ?? 0;

            // Section 2: Overdue transactions (fetch first, compute in memory to avoid provider coercion issues)
            var overdueTransactionRows = await _context.BorrowTransactions
                .Include(t => t.User)
                .Include(t => t.Book)
                .Where(t => t.ReturnDate == null && t.DueDate < now)
                .OrderBy(t => t.DueDate)
                .Take(5)
                .ToListAsync();

            var overdueTransactions = overdueTransactionRows
                .Select(t =>
                {
                    var daysOverdue = (now.Date - t.DueDate.Date).Days;
                    if (daysOverdue < 0) daysOverdue = 0;

                    return new OverdueTransactionViewModel
                    {
                        BorrowerName = t.User?.FullName ?? "N/A",
                        BookTitle = t.Book?.Title ?? "N/A",
                        DueDate = t.DueDate,
                        DaysOverdue = daysOverdue
                    };
                })
                .ToList();

            var overdueBooks = await _context.BorrowTransactions.CountAsync(t => t.ReturnDate == null && t.DueDate < now);

            // Section 3: Fine revenue
            var lastSixMonths = now.AddMonths(-5);
            var monthlyRevenueRows = await _context.Fines
                .Where(f => f.IsPaid && f.PaidAt != null && f.PaidAt >= new DateTime(lastSixMonths.Year, lastSixMonths.Month, 1))
                .GroupBy(f => new { Year = f.PaidAt!.Value.Year, Month = f.PaidAt!.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(f => f.Amount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var revenueData = monthlyRevenueRows.Select(x => new MonthlyRevenueViewModel
            {
                Month = new DateTime(x.Year, x.Month, 1).ToString("MMM"),
                Revenue = x.Revenue
            }).ToList();

            // Section 4: Users/fines aggregation
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

            // Section 5: Recent category activity
            var thirtyDaysAgo = now.AddDays(-30);
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

            var mostBorrowedBooks = await _context.BorrowTransactions
                .Where(t => t.BorrowDate >= thirtyDaysAgo && t.Book != null)
                .GroupBy(t => t.Book!.Title)
                .Select(g => new MostBorrowedBookViewModel
                {
                    Title = g.Key ?? "Unknown",
                    BorrowCount = g.Count()
                })
                .OrderByDescending(x => x.BorrowCount)
                .Take(5)
                .ToListAsync();

            var mostActiveUsers = await _context.BorrowTransactions
                .Where(t => t.BorrowDate >= thirtyDaysAgo && t.User != null)
                .GroupBy(t => new { t.User!.FullName, t.User!.Email })
                .Select(g => new ActiveUserViewModel
                {
                    FullName = g.Key.FullName ?? "N/A",
                    Email = g.Key.Email ?? "N/A",
                    BorrowCount = g.Count()
                })
                .OrderByDescending(x => x.BorrowCount)
                .Take(5)
                .ToListAsync();

            var overdueTrendRaw = await _context.BorrowTransactions
                .Where(t => t.ReturnDate == null && t.DueDate < now && t.DueDate >= now.AddDays(-13))
                .Select(t => t.DueDate.Date)
                .ToListAsync();

            var overdueTrend = Enumerable.Range(0, 14)
                .Select(i => now.Date.AddDays(-13 + i))
                .Select(date => new OverdueTrendPointViewModel
                {
                    DateLabel = date.ToString("MM-dd"),
                    OverdueCount = overdueTrendRaw.Count(d => d <= date)
                })
                .ToList();

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
                TopCategories = topCategories,
                MostBorrowedBooks = mostBorrowedBooks,
                MostActiveUsers = mostActiveUsers,
                OverdueTrend = overdueTrend
            };

            return View(model);
        }

        public async Task<IActionResult> FineManagement(
            string? search,
            string status = "unpaid",
            decimal? minAmount = null,
            decimal? maxAmount = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string sort = "created_desc",
            int page = 1,
            int pageSize = 10)
        {
            var fines = status == "paid"
                ? await _fineService.GetPaidFinesAsync()
                : await _fineService.GetUnpaidFinesAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                fines = fines.Where(f =>
                        (f.BorrowTransaction.User?.FullName ?? string.Empty).ToLower().Contains(s) ||
                        (f.BorrowTransaction.User?.Email ?? string.Empty).ToLower().Contains(s) ||
                        (f.BorrowTransaction.Book?.Title ?? string.Empty).ToLower().Contains(s))
                    .ToList();
            }

            if (minAmount.HasValue) fines = fines.Where(f => f.Amount >= minAmount.Value).ToList();
            if (maxAmount.HasValue) fines = fines.Where(f => f.Amount <= maxAmount.Value).ToList();
            if (fromDate.HasValue) fines = fines.Where(f => f.CreatedAt.Date >= fromDate.Value.Date).ToList();
            if (toDate.HasValue) fines = fines.Where(f => f.CreatedAt.Date <= toDate.Value.Date).ToList();

            fines = sort switch
            {
                "amount_asc" => fines.OrderBy(f => f.Amount).ToList(),
                "amount_desc" => fines.OrderByDescending(f => f.Amount).ToList(),
                "created_asc" => fines.OrderBy(f => f.CreatedAt).ToList(),
                "paid_asc" => fines.OrderBy(f => f.PaidAt).ToList(),
                "paid_desc" => fines.OrderByDescending(f => f.PaidAt).ToList(),
                _ when status == "paid" && sort == "created_desc" => fines.OrderByDescending(f => f.PaidAt).ToList(),
                _ => fines.OrderByDescending(f => f.CreatedAt).ToList()
            };

            var totalItems = fines.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);
            var paged = fines.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var model = new FineManagementViewModel
            {
                Fines = paged,
                Search = search,
                Status = status,
                MinAmount = minAmount,
                MaxAmount = maxAmount,
                FromDate = fromDate,
                ToDate = toDate,
                Sort = sort,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            return View(model);
        }

        public async Task<IActionResult> ReceiptHistory()
        {
            var paidFines = await _fineService.GetPaidFinesAsync();
            return View(paidFines);
        }

        [Authorize(Roles = "Admin")]
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
