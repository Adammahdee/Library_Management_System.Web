using System.Collections.Generic;

namespace Library_Management_System.Web.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }

        public string DepartmentName { get; set; }
            = string.Empty;

        public ICollection<ApplicationUser> Users { get; set; }
            = new List<ApplicationUser>();
    }
}
