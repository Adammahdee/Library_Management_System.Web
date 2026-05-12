using Library_Management_System.Web.Models;

namespace Library_Management_System.Web.Services
{
    public interface IBorrowService
    {
        Task<List<BorrowTransaction>> GetTransactionHistoryAsync();

        Task<BorrowTransaction?> GetTransactionByIdAsync(int transactionId);

        Task<bool> ProcessReturnAsync(int transactionId);

        decimal CalculateOverdueFine(BorrowTransaction transaction);

        /// <summary>
        /// Processes a new book checkout or reservation, enforcing borrow limits.
        /// </summary>
        /// <param name="transaction">The borrow transaction details.</param>
        /// <returns>A tuple indicating success and an error message if the operation fails (e.g., borrow limit exceeded).</returns>
        Task<(bool success, string? errorMessage)> CheckoutAsync(BorrowTransaction transaction);

        Task<bool> ConfirmCheckoutAsync(int transactionId);

        Task<List<BorrowTransaction>> GetActiveLoansAsync(string userId);

        /// <summary>
        /// Retrieves the number of active loans (Borrowed or Overdue) for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The count of active borrow transactions.</returns>
        Task<int> GetActiveBorrowCountAsync(string userId);

        Task<bool> UpdateTransactionAsync(BorrowTransaction transaction);

        Task<int> RunOverdueAutomationAsync();
    }
}
