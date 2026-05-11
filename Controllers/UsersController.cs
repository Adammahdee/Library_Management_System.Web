using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library_Management_System.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library_Management_System.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IBorrowService _borrowService;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IBorrowService borrowService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _borrowService = borrowService;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Department)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);
            }

            ViewBag.UserRoles = userRoles;
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

            // Retrieve active loans using the consolidated service method
            ViewBag.ActiveLoans = await _borrowService.GetActiveLoansAsync(id);
            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);
            ViewBag.AvailableRoles = new SelectList(
                await _roleManager.Roles.OrderBy(r => r.Name).Select(r => r.Name!).ToListAsync());
            ViewBag.BorrowHistory = user.BorrowTransactions
                .OrderByDescending(bt => bt.BorrowDate)
                .ToList();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string id, string roleName)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(roleName))
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["ErrorMessage"] = $"Role '{roleName}' does not exist.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            TempData["SuccessMessage"] = $"Assigned role '{roleName}' to {user.FullName}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string id, string roleName)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(roleName))
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (roleName == "Admin")
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1 && adminUsers.Any(u => u.Id == id))
                {
                    TempData["ErrorMessage"] = "Cannot remove the last remaining Admin role.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            TempData["SuccessMessage"] = $"Removed role '{roleName}' from {user.FullName}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.IsActive = false;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = $"{user.FullName} was locked successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.LockoutEnd = null;
            user.IsActive = true;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = $"{user.FullName} was unlocked successfully.";
            return RedirectToAction(nameof(Details), new { id });
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
