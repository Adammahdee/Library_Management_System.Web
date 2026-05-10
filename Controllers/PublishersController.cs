using Library_Management_System.Web.Data;
using Library_Management_System.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    [Route("Publisher")]
    [Route("Publishers")]
    public class PublishersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PublishersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Publishers
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var publishers = await _context.Publishers
                .Include(p => p.Books)
                .AsNoTracking()
                .OrderBy(p => p.PublisherName)
                .ToListAsync();
            return View(publishers);
        }

        // GET: Publishers/Details/5
        [Route("Details/{id?}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var publisher = await _context.Publishers
                .Include(p => p.Books)
                .FirstOrDefaultAsync(m => m.PublisherId == id);

            if (publisher == null) return NotFound();

            return View(publisher);
        }

        // GET: Publishers/Create
        [Route("Create")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Publishers/Create
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PublisherName,Address,PhoneNumber,Email")] Publisher publisher)
        {
            // Check for duplicate publisher name
            if (!string.IsNullOrWhiteSpace(publisher.PublisherName) &&
                await _context.Publishers.AnyAsync(p => p.PublisherName.Trim() == publisher.PublisherName.Trim()))
            {
                ModelState.AddModelError("PublisherName", "A publisher with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(publisher);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Publisher created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(publisher);
        }

        // GET: Publishers/Edit/5
        [Route("Edit/{id?}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null) return NotFound();
            return View(publisher);
        }

        // POST: Publishers/Edit/5
        [HttpPost]
        [Route("Edit/{id?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PublisherId,PublisherName,Address,PhoneNumber,Email")] Publisher publisher)
        {
            if (id != publisher.PublisherId) return NotFound();

            // Check for duplicate publisher name (excluding the current record being edited)
            if (!string.IsNullOrWhiteSpace(publisher.PublisherName) &&
                await _context.Publishers.AnyAsync(p => p.PublisherName.Trim() == publisher.PublisherName.Trim() && p.PublisherId != id))
            {
                ModelState.AddModelError("PublisherName", "A publisher with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(publisher);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PublisherExists(publisher.PublisherId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(publisher);
        }

        // GET: Publishers/Delete/5
        [Route("Delete/{id?}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var publisher = await _context.Publishers
                .Include(p => p.Books)
                .FirstOrDefaultAsync(m => m.PublisherId == id);

            if (publisher == null) return NotFound();

            return View(publisher);
        }

        // POST: Publishers/Delete/5
        [HttpPost, ActionName("Delete")]
        [Route("Delete/{id?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher != null)
            {
                _context.Publishers.Remove(publisher);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PublisherExists(int id)
        {
            return _context.Publishers.Any(e => e.PublisherId == id);
        }
    }
}