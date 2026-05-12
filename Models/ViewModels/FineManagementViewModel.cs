using Library_Management_System.Web.Models;

namespace Library_Management_System.Web.Models.ViewModels
{
    public class FineManagementViewModel
    {
        public List<Fine> Fines { get; set; } = new();
        public string? Search { get; set; }
        public string Status { get; set; } = "unpaid";
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Sort { get; set; } = "created_desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; } = 1;
    }
}
