using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Library_Management_System.Web.Services;
using Library_Management_System.Web.Services.Interfaces;
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
        private readonly IFineService _fineService;

        public BorrowTransactionController(ApplicationDbContext context, IBorrowService borrowService, IFineService fineService)
        {
            _context = context;
            _borrowService = borrowService;
            _fineService = fineService;
        }

        // GET: BorrowTransaction
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            await _borrowService.RunOverdueAutomationAsync();
            var transactions = await _borrowService.GetTransactionHistoryAsync();
            return View(transactions);
        }

        [Route("History")]
        public async Task<IActionResult> History(string? search, string? status, string sort = "borrow_desc", int page = 1, int pageSize = 10)
        {
            await _borrowService.RunOverdueAutomationAsync();
            var transactions = await _borrowService.GetTransactionHistoryAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                transactions = transactions.Where(t =>
                        (t.User?.FullName ?? string.Empty).ToLower().Contains(s) ||
                        (t.User?.Email ?? string.Empty).ToLower().Contains(s) ||
                        (t.Book?.Title ?? string.Empty).ToLower().Contains(s))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                transactions = transactions.Where(t => t.Status == status).ToList();
            }

            transactions = sort switch
            {
                "due_asc" => transactions.OrderBy(t => t.DueDate).ToList(),
                "due_desc" => transactions.OrderByDescending(t => t.DueDate).ToList(),
                "borrow_asc" => transactions.OrderBy(t => t.BorrowDate).ToList(),
                _ => transactions.OrderByDescending(t => t.BorrowDate).ToList()
            };

            var totalItems = transactions.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);
            var paged = transactions.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.StatusOptions = transactions.Select(t => t.Status).Distinct().OrderBy(s => s).ToList();

            return View(paged);
        }

        // GET: BorrowTransaction/Details/5
        [Route("Details/{id?}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var transaction = await _borrowService.GetTransactionByIdAsync(id.Value);

            if (transaction == null) return NotFound();

            return View(transaction);
        }

        // GET: BorrowTransaction/Checkout
        [Route("Checkout")]
        public async Task<IActionResult> Checkout()
        {
            var model = new BorrowTransaction
            {
                BorrowDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(14),
                Status = "Borrowed"
            };

            await PopulateFormDropdownsAsync(model.UserId, model.BookId, model.Status);
            return View(model);
        }

        // GET: BorrowTransaction/Reserve
        [Route("Reserve")]
        public async Task<IActionResult> Reserve()
        {
            var model = new BorrowTransaction
            {
                BorrowDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(3),
                Status = "Reserved"
            };

            await PopulateFormDropdownsAsync(model.UserId, model.BookId, model.Status);
            return View("Checkout", model);
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
                var (success, error) = await _borrowService.CheckoutAsync(transaction);
                if (success)
                {
                    TempData["SuccessMessage"] = transaction.Status == "Reserved" ? "Book reserved successfully!" : "Book checked out successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, error ?? "Unable to process checkout. Please check book availability.");
            }

            await PopulateFormDropdownsAsync(transaction.UserId, transaction.BookId, transaction.Status);
            return View(transaction);
        }

        // GET: BorrowTransaction/Create
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            var model = new BorrowTransaction
            {
                BorrowDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(14),
                Status = "Borrowed"
            };

            await PopulateFormDropdownsAsync(model.UserId, model.BookId, model.Status);
            return View(model);
        }

        // POST: BorrowTransaction/Create
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BorrowTransaction transaction)
        {
            if (transaction.DueDate <= transaction.BorrowDate)
            {
                ModelState.AddModelError(nameof(transaction.DueDate), "Due date must be after the borrow date.");
            }

            if (ModelState.IsValid)
            {
                var (success, error) = await _borrowService.CheckoutAsync(transaction);
                if (success)
                {
                    TempData["SuccessMessage"] = "Borrow transaction created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, error ?? "Unable to create transaction. Verify book availability.");
            }

            await PopulateFormDropdownsAsync(transaction.UserId, transaction.BookId, transaction.Status);
            return View(transaction);
        }

        // GET: BorrowTransaction/Edit/5
        [Route("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var transaction = await _borrowService.GetTransactionByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            await PopulateFormDropdownsAsync(transaction.UserId, transaction.BookId, transaction.Status);
            return View(transaction);
        }

        // POST: BorrowTransaction/Edit/5
        [HttpPost]
        [Route("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BorrowTransaction transaction)
        {
            if (id != transaction.TransactionId)
            {
                return NotFound();
            }

            if (transaction.DueDate <= transaction.BorrowDate)
            {
                ModelState.AddModelError(nameof(transaction.DueDate), "Due date must be after the borrow date.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var updated = await _borrowService.UpdateTransactionAsync(transaction);
                    if (!updated)
                    {
                        return NotFound();
                    }
                    TempData["SuccessMessage"] = "Borrow transaction updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.BorrowTransactions.AnyAsync(bt => bt.TransactionId == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            await PopulateFormDropdownsAsync(transaction.UserId, transaction.BookId, transaction.Status);
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

        [HttpPost]
        [Route("PayFine/{fineId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayFine(int fineId, int? transactionId = null)
        {
            var paid = await _fineService.PayFineAsync(fineId);
            if (!paid)
            {
                TempData["ErrorMessage"] = "Fine payment failed. Fine was not found.";
            }
            else
            {
                TempData["SuccessMessage"] = "Fine marked as paid successfully.";
            }

            if (transactionId.HasValue)
            {
                return RedirectToAction(nameof(Details), new { id = transactionId.Value });
            }

            return RedirectToAction(nameof(History));
        }

        private async Task PopulateFormDropdownsAsync(string? selectedUserId = null, int? selectedBookId = null, string? selectedStatus = null)
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var books = await _context.Books
                .OrderBy(b => b.Title)
                .ToListAsync();

            ViewBag.UserId = new SelectList(users, "Id", "FullName", selectedUserId);
            ViewBag.BookId = new SelectList(books, "BookId", "Title", selectedBookId);
            ViewBag.Status = new SelectList(
                new[] { "Borrowed", "Reserved", "Returned", "Returned (Overdue)", "Cancelled", "Overdue" },
                selectedStatus ?? "Borrowed");

            if (!books.Any())
            {
                ViewBag.BookWarning = "No books found. Add at least one book before creating a borrow transaction.";
            }
        }
    }
}
