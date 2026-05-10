using Microsoft.AspNetCore.Identity;

namespace Library_Management_System.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
            = string.Empty;

        public string? StudentNumber { get; set; }

        public string? StaffNumber { get; set; }

        public int? DepartmentId { get; set; }

        public Department? Department { get; set; }

        public bool IsActive { get; set; }
            = true;

        public ICollection<BorrowTransaction> BorrowTransactions { get; set; }
            = new List<BorrowTransaction>();

        public ICollection<Reservation> Reservations { get; set; }
            = new List<Reservation>();

        public ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();
    }
}
