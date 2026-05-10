using Library_Management_System.Web.Models;

namespace Library_Management_System.Web.Services.Interfaces
{
    public interface IFineService
    {
        Task GenerateFineAsync(BorrowTransaction transaction);

        Task<List<Fine>> GetAllFinesAsync();

        Task PayFineAsync(int fineId);
    }
}