using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library_Management_System.Web.Models.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public int? DepartmentId { get; set; }

        [Required]
        public string Role { get; set; } = "Student";

        public bool IsActive { get; set; } = true;

        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();
    }
}
