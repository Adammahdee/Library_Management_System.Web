namespace Library_Management_System.Web.Models.ViewModels
{
    public class RecentBorrowTransactionViewModel
    {
        public string BorrowerName { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
