using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Library_Management_System.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    [Route("Transaction")]
    [Route("Transactions")]
    [Route("BorrowTransaction")]
    public class BorrowTransactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBorrowService _borrowService;

        public BorrowTransactionController(ApplicationDbContext context, IBorrowService borrowService)
        {
            _context = context;
            _borrowService = borrowService;
        }

        // GET: BorrowTransaction
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var transactions = await _context.BorrowTransactions
                .Include(bt => bt.User)
                .Include(bt => bt.Book)
                .OrderByDescending(bt => bt.BorrowDate)
                .ToListAsync();
            return View(transactions);
        }

        // GET: BorrowTransaction/Details/5
        [Route("Details/{id?}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var transaction = await _context.BorrowTransactions
                .Include(bt => bt.User)
                .Include(bt => bt.Book)
                .Include(bt => bt.Fines)
                .FirstOrDefaultAsync(m => m.TransactionId == id);

            if (transaction == null) return NotFound();

            return View(transaction);
        }

        // GET: BorrowTransaction/Checkout
        [Route("Checkout")]
        public async Task<IActionResult> Checkout()
        {
            ViewBag.UserId = new SelectList(await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName");
            ViewBag.BookId = new SelectList(await _context.Books.Where(b => b.AvailableCopies > 0).OrderBy(b => b.Title).ToListAsync(), "BookId", "Title");
            return View(new BorrowTransaction { BorrowDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) });
        }

        // GET: BorrowTransaction/Reserve
        [Route("Reserve")]
        public async Task<IActionResult> Reserve()
        {
            ViewBag.UserId = new SelectList(await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName");
            ViewBag.BookId = new SelectList(await _context.Books.Where(b => b.AvailableCopies > 0).OrderBy(b => b.Title).ToListAsync(), "BookId", "Title");
            // Standard 3-day hold for reservations
            return View("Checkout", new BorrowTransaction { BorrowDate = DateTime.Today, DueDate = DateTime.Today.AddDays(3), Status = "Reserved" });
        }

        // POST: BorrowTransaction/Checkout
        [HttpPost]
        [Route("Checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(BorrowTransaction transaction)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.BookId == transaction.BookId);

            if (transaction.DueDate <= transaction.BorrowDate)
            {
                ModelState.AddModelError("DueDate", "Due date must be after the borrow date.");
            }

            // Check if user already has an active loan for this book
            var existingLoan = await _context.BorrowTransactions
                .AnyAsync(bt => bt.UserId == transaction.UserId && bt.BookId == transaction.BookId && bt.ReturnDate == null);

            if (existingLoan)
            {
                var type = transaction.Status == "Reserved" ? "reservation" : "loan";
                ModelState.AddModelError("BookId", $"This user already has an active {type} for this book.");
            }
            
            if (book == null)
            {
                ModelState.AddModelError("BookId", "Selected book not found.");
            }
            else if (book.AvailableCopies <= 0)
            {
                ModelState.AddModelError("BookId", "The selected book is currently out of stock.");
            }

            if (ModelState.IsValid)
            {
                if (await _borrowService.CheckoutAsync(transaction))
                {
                    TempData["SuccessMessage"] = transaction.Status == "Reserved" ? "Book reserved successfully!" : "Book checked out successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Unable to process checkout. Please check book availability.");
            }

            ViewBag.UserId = new SelectList(await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName", transaction.UserId);
            ViewBag.BookId = new SelectList(await _context.Books.Where(b => b.AvailableCopies > 0).OrderBy(b => b.Title).ToListAsync(), "BookId", "Title", transaction.BookId);
            return View(transaction);
        }

        // POST: BorrowTransaction/ConfirmCheckout/5
        [HttpPost]
        [Route("ConfirmCheckout/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCheckout(int id)
        {
            if (await _borrowService.ConfirmCheckoutAsync(id))
            {
                TempData["SuccessMessage"] = "Reservation converted to active loan.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Cannot confirm checkout for this transaction.";
            return RedirectToAction(nameof(Index));
        }

        // POST: BorrowTransaction/Return/5
        [HttpPost]
        [Route("Return/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int id, string? returnUrl = null)
        {
            var success = await _borrowService.ProcessReturnAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = "Could not process return. Transaction may already be closed.";
            }
            else
            {
                TempData["SuccessMessage"] = "Book returned successfully.";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}