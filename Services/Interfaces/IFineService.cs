using Library_Management_System.Web.Models;

namespace Library_Management_System.Web.Services.Interfaces
{
    public interface IFineService
    {
        Task GenerateFineAsync(BorrowTransaction transaction);

        Task<List<Fine>> GetAllFinesAsync();

        Task<List<Fine>> GetUnpaidFinesAsync();

        Task<decimal> EnsureOverdueFineAsync(BorrowTransaction transaction, DateTime effectiveDate, bool updateOpenLoanStatus);

        Task<bool> PayFineAsync(int fineId);
    }
}
