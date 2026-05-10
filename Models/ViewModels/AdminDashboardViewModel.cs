namespace Library_Management_System.Web.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int AvailableBooks { get; set; }
        public int ActiveUsers { get; set; }
        public int BorrowedBooks { get; set; }
        public int PendingReservations { get; set; }
        public int UnreadNotifications { get; set; }
        public int OutstandingFines { get; set; }
        public decimal OutstandingFineAmount { get; set; }
        public IReadOnlyList<RecentBorrowTransactionViewModel> RecentBorrowTransactions { get; set; }
            = new List<RecentBorrowTransactionViewModel>();
    }
}
