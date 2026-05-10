using Library_Management_System.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library_Management_System.Web.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    public class TransactionController : Controller
    {
        private readonly IBorrowService _borrowService;

        public TransactionController(IBorrowService borrowService)
        {
            _borrowService = borrowService;
        }

        // GET: Transaction/Index
        public async Task<IActionResult> Index()
        {
            var transactions = await _borrowService.GetAllTransactionsAsync();
            return View(transactions);
        }
    }
}