using System.Collections.Generic;

namespace Library_Management_System.Web.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalUsers { get; set; }
        public int TotalBorrowedBooks { get; set; }
        public int ReturnedBooks { get; set; }
        public int OverdueBooks { get; set; }
        public int PendingFines { get; set; }
        public decimal TotalFineAmount { get; set; }
        public List<MonthlyRevenueViewModel> MonthlyFineRevenue { get; set; } = new();
        public List<UserFineSummaryViewModel> TopFineUsers { get; set; } = new();
    }

    public class MonthlyRevenueViewModel
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class UserFineSummaryViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TotalFineAmount { get; set; }
    }
}