using Library_Management_System.Web.Data;
using Library_Management_System.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    public class FinesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFineService _fineService;

        public FinesController(
            ApplicationDbContext context,
            IFineService fineService)
        {
            _context = context;
            _fineService = fineService;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var fines = _context.Fines
                .Include(f => f.BorrowTransaction)
                    .ThenInclude(bt => bt.Book)
                .Include(f => f.BorrowTransaction)
                    .ThenInclude(bt => bt.User)
                .AsQueryable();

            if (status == "paid")
            {
                fines = fines.Where(f => f.IsPaid);
            }
            else if (status == "unpaid")
            {
                fines = fines.Where(f => !f.IsPaid);
            }

            ViewBag.CurrentStatus = status;

            return View(await fines
                .OrderBy(f => f.IsPaid)
                .ThenByDescending(f => f.CreatedAt)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var fine = await _context.Fines
                .Include(f => f.BorrowTransaction)
                    .ThenInclude(bt => bt.Book)
                .Include(f => f.BorrowTransaction)
                    .ThenInclude(bt => bt.User)
                .FirstOrDefaultAsync(f => f.FineId == id);

            if (fine == null)
            {
                return NotFound();
            }

            return View(fine);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id)
        {
            var success = await _fineService.PayFineAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] =
                    "Fine marked as paid successfully.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Unable to process fine payment.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}