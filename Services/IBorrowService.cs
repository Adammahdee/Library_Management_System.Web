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

        /// <summary>
        /// Retrieves all system transactions (audit logs) ordered by date.
        /// </summary>
        Task<List<Transaction>> GetAllTransactionsAsync();

        /// <summary>
        /// Records a new system transaction into the audit log.
        /// </summary>
        /// <param name="transaction">The transaction log entry.</param>
        Task RecordTransactionAsync(Transaction transaction);

        /// <summary>
        /// Processes a new book checkout or reservation and records the system activity.
        /// </summary>
        /// <param name="transaction">The borrow transaction details.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        Task<bool> CheckoutAsync(BorrowTransaction transaction);

        /// <summary>
        /// Confirms a reservation and converts it into an active loan.
        /// </summary>
        /// <param name="transactionId">The ID of the reservation transaction.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        Task<bool> ConfirmCheckoutAsync(int transactionId);

        /// <summary>
        /// Retrieves all currently active loans (borrowed but not yet returned) for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of active borrow transactions including book details.</returns>
        Task<List<BorrowTransaction>> GetActiveLoansAsync(string userId);
    }
}