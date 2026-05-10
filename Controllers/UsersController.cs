using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Department)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.BorrowTransactions)
                    .ThenInclude(bt => bt.Book)
                .Include(u => u.BorrowTransactions)
                    .ThenInclude(bt => bt.Fines)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Users/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/PayAllFines/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAllFines(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var unpaidFines = await _context.Fines
                .Include(f => f.BorrowTransaction)
                .Where(f => f.BorrowTransaction.UserId == id && !f.IsPaid)
                .ToListAsync();

            if (unpaidFines.Any())
            {
                foreach (var fine in unpaidFines)
                {
                    fine.IsPaid = true;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "All unpaid fines for this user have been marked as paid.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}