using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Library_Management_System.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? pageNumber)
        {
            int pageSize = 10;
            int pageIndex = pageNumber ?? 1;

            var query = _context.Books
                .Include(book => book.Category)
                .AsNoTracking()
                .OrderBy(book => book.Title);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var books = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["PageIndex"] = pageIndex;
            ViewData["TotalPages"] = Math.Max(1, totalPages);

            // Updated to return the Book entity list as per mapping requirements
            return View(books);
        }

        public async Task<IActionResult> Create()
        {
            var model = new BookViewModel
            {
                Quantity = 1,
                AvailableQuantity = 1
            };

            ViewBag.CategoryId = await GetCategoryOptionsAsync();
            ViewBag.PublisherId = new SelectList(await _context.Publishers.OrderBy(p => p.PublisherName).ToListAsync(), "PublisherId", "PublisherName");
            ViewBag.Authors = new MultiSelectList(await _context.Authors.OrderBy(a => a.AuthorName).ToListAsync(), "AuthorId", "AuthorName");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookViewModel model, int[] selectedAuthors)
        {
            var title = model.Title?.Trim() ?? string.Empty;
            var isbn = model.ISBN?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError(nameof(model.Title), "Title is required.");
            }

            if (string.IsNullOrWhiteSpace(isbn))
            {
                ModelState.AddModelError(nameof(model.ISBN), "ISBN is required.");
            }

            if (model.AvailableQuantity > model.Quantity)
            {
                ModelState.AddModelError(
                    nameof(model.AvailableQuantity),
                    "Available quantity cannot be greater than quantity.");
            }

            if (!string.IsNullOrWhiteSpace(isbn)
                && await _context.Books.AnyAsync(book => book.ISBN == isbn))
            {
                ModelState.AddModelError(nameof(model.ISBN), "A book with this ISBN already exists.");
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await GetCategoryOptionsAsync(model.CategoryId);
                return View(model);
            }

            var book = new Book
            {
                Title = title,
                ISBN = isbn,
                TotalCopies = model.Quantity,
                AvailableCopies = model.AvailableQuantity,
                CategoryId = model.CategoryId,
                PublisherId = model.PublisherId
            };

            _context.Books.Add(book);

            if (selectedAuthors != null)
            {
                foreach (var authorId in selectedAuthors)
                {
                    _context.BookAuthors.Add(new BookAuthor { Book = book, AuthorId = authorId });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null) return NotFound();

            var model = new BookViewModel
            {
                BookId = book.BookId,
                Title = book.Title,
                ISBN = book.ISBN,
                Quantity = book.TotalCopies,
                AvailableQuantity = book.AvailableCopies,
                CategoryName = book.Category?.CategoryName,
                PublisherName = book.Publisher?.PublisherName,
                AuthorNames = book.BookAuthors.Select(ba => ba.Author?.AuthorName ?? "Unknown").ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.Include(b => b.BookAuthors).FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null) return NotFound();

            var model = new BookViewModel
            {
                BookId = book.BookId,
                Title = book.Title,
                ISBN = book.ISBN,
                Quantity = book.TotalCopies,
                AvailableQuantity = book.AvailableCopies,
                CategoryId = book.CategoryId,
                PublisherId = book.PublisherId
            };

            ViewBag.CategoryId = new SelectList(await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(), "CategoryId", "CategoryName", model.CategoryId);
            ViewBag.PublisherId = new SelectList(await _context.Publishers.OrderBy(p => p.PublisherName).ToListAsync(), "PublisherId", "PublisherName", model.PublisherId);
            ViewBag.Authors = new MultiSelectList(await _context.Authors.OrderBy(a => a.AuthorName).ToListAsync(), "AuthorId", "AuthorName", book.BookAuthors.Select(ba => ba.AuthorId).ToArray());

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookViewModel model, int[] selectedAuthors)
        {
            if (id != model.BookId) return NotFound();

            if (ModelState.IsValid)
            {
                var book = await _context.Books.Include(b => b.BookAuthors).FirstOrDefaultAsync(b => b.BookId == id);
                if (book == null) return NotFound();

                var normalizedIsbn = model.ISBN?.Trim() ?? string.Empty;
                if (await _context.Books.AnyAsync(b => b.BookId != id && b.ISBN == normalizedIsbn))
                {
                    ModelState.AddModelError(nameof(model.ISBN), "A different book with this ISBN already exists.");
                    ViewBag.CategoryId = new SelectList(await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(), "CategoryId", "CategoryName", model.CategoryId);
                    ViewBag.PublisherId = new SelectList(await _context.Publishers.OrderBy(p => p.PublisherName).ToListAsync(), "PublisherId", "PublisherName", model.PublisherId);
                    ViewBag.Authors = new MultiSelectList(await _context.Authors.OrderBy(a => a.AuthorName).ToListAsync(), "AuthorId", "AuthorName", book.BookAuthors.Select(ba => ba.AuthorId).ToArray());
                    return View(model);
                }

                // Logic Error Fix: Calculate the change in total quantity to adjust availability correctly.
                // Do not rely on hidden fields for AvailableCopies as they can be tampered with or become stale.
                int quantityDelta = model.Quantity - book.TotalCopies;
                int currentLoans = book.TotalCopies - book.AvailableCopies;

                if (model.Quantity < currentLoans)
                {
                    ModelState.AddModelError("Quantity", $"Cannot reduce total stock below the number of current active loans ({currentLoans}).");
                    ViewBag.CategoryId = new SelectList(await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(), "CategoryId", "CategoryName", model.CategoryId);
                    ViewBag.PublisherId = new SelectList(await _context.Publishers.OrderBy(p => p.PublisherName).ToListAsync(), "PublisherId", "PublisherName", model.PublisherId);
                    ViewBag.Authors = new MultiSelectList(await _context.Authors.OrderBy(a => a.AuthorName).ToListAsync(), "AuthorId", "AuthorName", book.BookAuthors.Select(ba => ba.AuthorId).ToArray());
                    return View(model);
                }

                book.Title = model.Title;
                book.ISBN = normalizedIsbn;
                book.TotalCopies = model.Quantity;
                book.AvailableCopies += quantityDelta;
                book.CategoryId = model.CategoryId;
                book.PublisherId = model.PublisherId;

                _context.BookAuthors.RemoveRange(book.BookAuthors);
                if (selectedAuthors != null)
                {
                    foreach (var authorId in selectedAuthors)
                    {
                        book.BookAuthors.Add(new BookAuthor { BookId = id, AuthorId = authorId });
                    }
                }

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError(string.Empty, "This book was modified by another user. Please reload and try again.");
                }
            }
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                var hasActiveTransactions = await _context.BorrowTransactions
                    .AnyAsync(t => t.BookId == id && t.ReturnDate == null);
                if (hasActiveTransactions)
                {
                    TempData["ErrorMessage"] = "Cannot delete a book with active borrow transactions.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Book deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetCategoryOptionsAsync(int? selectedCategoryId = null)
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.CategoryName)
                .Select(category => new SelectListItem
                {
                    Value = category.CategoryId.ToString(),
                    Text = category.CategoryName,
                    Selected = selectedCategoryId.HasValue && category.CategoryId == selectedCategoryId.Value
                })
                .ToListAsync();
        }
    }
}
