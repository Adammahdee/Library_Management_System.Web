using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library_Management_System.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Library_Management_System.Web.Models.ViewModels;

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
        public async Task<IActionResult> Index(string? search, string? role, string? status, int page = 1, int pageSize = 10)
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

            if (!string.IsNullOrWhiteSpace(search))
            {
                var needle = search.Trim().ToLowerInvariant();
                users = users.Where(u =>
                        (u.FullName ?? string.Empty).ToLower().Contains(needle) ||
                        (u.Email ?? string.Empty).ToLower().Contains(needle) ||
                        (u.UserName ?? string.Empty).ToLower().Contains(needle))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                users = users.Where(u => userRoles.TryGetValue(u.Id, out var roles) && roles.Contains(role)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                users = status switch
                {
                    "locked" => users.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow).ToList(),
                    "active" => users.Where(u => u.IsActive && (!u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow)).ToList(),
                    "inactive" => users.Where(u => !u.IsActive).ToList(),
                    _ => users
                };
            }

            var totalItems = users.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);
            var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var model = pagedUsers.Select(user => new UserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                DepartmentName = user.Department?.DepartmentName ?? "N/A",
                Roles = userRoles.TryGetValue(user.Id, out var roles) ? roles.ToList() : new List<string>(),
                IsActive = user.IsActive,
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                CreatedAt = user.CreatedAt
            }).ToList();

            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.AvailableRoles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name!)
                .ToListAsync();

            return View(model);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            var model = new CreateUserViewModel();
            await PopulateUserFormOptionsAsync(model);
            return View(model);
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!await _roleManager.RoleExistsAsync(model.Role) || !GetAllowedRoles().Contains(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Please choose a valid role.");
            }

            if (await _userManager.FindByEmailAsync(model.Email.Trim()) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already in use.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUserFormOptionsAsync(model);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                FullName = model.FullName.Trim(),
                DepartmentId = model.DepartmentId,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                LockoutEnabled = true
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                await PopulateUserFormOptionsAsync(model);
                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!roleResult.Succeeded)
            {
                foreach (var err in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                await PopulateUserFormOptionsAsync(model);
                return View(model);
            }

            TempData["SuccessMessage"] = $"User {user.FullName} created successfully.";
            return RedirectToAction(nameof(Index));
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
            ViewBag.AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).Select(r => r.Name!).ToListAsync();
            ViewBag.BorrowHistory = user.BorrowTransactions
                .OrderByDescending(bt => bt.BorrowDate)
                .ToList();
            ViewBag.ActivityHistory = await _context.AuditLogs
                .Where(a => a.UserId == id)
                .OrderByDescending(a => a.LogDate)
                .Take(50)
                .ToListAsync();

            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                DepartmentId = user.DepartmentId,
                Role = roles.FirstOrDefault() ?? "Student",
                IsActive = user.IsActive
            };

            await PopulateUserFormOptionsAsync(model);
            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            if (!await _roleManager.RoleExistsAsync(model.Role) || !GetAllowedRoles().Contains(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Please choose a valid role.");
            }

            var emailOwner = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (emailOwner != null && emailOwner.Id != model.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already in use.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUserFormOptionsAsync(model);
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.UserName = model.Email.Trim();
            user.DepartmentId = model.DepartmentId;
            user.IsActive = model.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                await PopulateUserFormOptionsAsync(model);
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains("Admin") && model.Role != "Admin")
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1)
                {
                    TempData["ErrorMessage"] = "Cannot remove the last remaining Admin role.";
                    return RedirectToAction(nameof(Edit), new { id = model.Id });
                }
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!roleResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", roleResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Edit), new { id = model.Id });
            }

            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
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
        public async Task<IActionResult> SyncRoles(string id, List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var targetRoles = roles?.Distinct().ToList() ?? new List<string>();

            if (!targetRoles.Contains("Admin") && currentRoles.Contains("Admin"))
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1)
                {
                    TempData["ErrorMessage"] = "Cannot remove the last remaining Admin role.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(targetRoles));
            if (!removeResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", removeResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id });
            }

            var addResult = await _userManager.AddToRolesAsync(user, targetRoles.Except(currentRoles));
            if (!addResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", addResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = "Roles updated successfully.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                TempData["ErrorMessage"] = "Password must be at least 8 characters.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Password confirmation does not match.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = $"Password reset successfully for {user.FullName}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));

            var currentUserId = _userManager.GetUserId(User);
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1)
                {
                    TempData["ErrorMessage"] = "Cannot delete the last admin account.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var result = await _userManager.DeleteAsync(user);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "User deleted successfully." : string.Join(" ", result.Errors.Select(e => e.Description));
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

        private static IReadOnlyList<string> GetAllowedRoles()
        {
            return new[] { "Admin", "Librarian", "Student" };
        }

        private async Task PopulateUserFormOptionsAsync(CreateUserViewModel model)
        {
            model.Departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = d.DepartmentName })
                .ToListAsync();
            model.Roles = GetAllowedRoles()
                .Select(r => new SelectListItem { Value = r, Text = r })
                .ToList();
        }

        private async Task PopulateUserFormOptionsAsync(EditUserViewModel model)
        {
            model.Departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = d.DepartmentName })
                .ToListAsync();
            model.Roles = GetAllowedRoles()
                .Select(r => new SelectListItem { Value = r, Text = r })
                .ToList();
        }
    }
}
