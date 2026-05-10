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
        public async Task<IActionResult> Checkout()
        {
            ViewBag.UserId = new SelectList(await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName");
            ViewBag.BookId = new SelectList(await _context.Books.Where(b => b.AvailableCopies > 0).OrderBy(b => b.Title).ToListAsync(), "BookId", "Title");
            return View(new BorrowTransaction { DueDate = DateTime.Now.AddDays(14) });
        }

        // POST: BorrowTransaction/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(BorrowTransaction transaction)
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.BookId == transaction.BookId);

            // Check if user already has an active loan for this book
            var existingLoan = await _context.BorrowTransactions
                .AnyAsync(bt => bt.UserId == transaction.UserId && bt.BookId == transaction.BookId && bt.ReturnDate == null);

            if (existingLoan)
            {
                ModelState.AddModelError("", "This user already has an active loan for this book.");
            }
            else if (book == null || book.AvailableCopies <= 0)
            {
                ModelState.AddModelError("", "The selected book is currently unavailable.");
            }

            if (ModelState.IsValid)
            {
                transaction.Status = "Borrowed";
                
                if (book != null) book.AvailableCopies--;
                
                _context.Add(transaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.UserId = new SelectList(await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName", transaction.UserId);
            ViewBag.BookId = new SelectList(await _context.Books.Where(b => b.AvailableCopies > 0).OrderBy(b => b.Title).ToListAsync(), "BookId", "Title", transaction.BookId);
            return View(transaction);
        }

        // POST: BorrowTransaction/Return/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int id)
        {
            var success = await _borrowService.ProcessReturnAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = "Could not process return. Transaction may already be closed.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}