using Library_Management_System.Web.Models;

namespace Library_Management_System.Web.Services
{
    public interface IBorrowService
    {
        Task<List<BorrowTransaction>> GetTransactionHistoryAsync();

        Task<BorrowTransaction?> GetTransactionByIdAsync(int transactionId);

        Task<bool> ProcessReturnAsync(int transactionId);

        decimal CalculateOverdueFine(BorrowTransaction transaction);

        Task<bool> CheckoutAsync(BorrowTransaction transaction);

        Task<bool> ConfirmCheckoutAsync(int transactionId);

        Task<List<BorrowTransaction>> GetActiveLoansAsync(string userId);

        Task<bool> UpdateTransactionAsync(BorrowTransaction transaction);

        Task<int> RunOverdueAutomationAsync();
    }
}
