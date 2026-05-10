using Library_Management_System.Web.Models;

namespace Library_Management_System.Web.Services
{
    public interface IBorrowService
    {
        /// <summary>
        /// Processes a book return, updates inventory, and applies fines if overdue.
        /// </summary>
        /// <param name="transactionId">The ID of the borrow transaction.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        Task<bool> ProcessReturnAsync(int transactionId);

        /// <summary>
        /// Calculates the overdue fine for a given transaction.
        /// </summary>
        /// <param name="transaction">The borrow transaction details.</param>
        /// <returns>The calculated fine amount.</returns>
        decimal CalculateOverdueFine(BorrowTransaction transaction);
    }
}