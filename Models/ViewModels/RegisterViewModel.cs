using System.ComponentModel.DataAnnotations;

namespace Library_Management_System.Web.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(150)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Student number")]
        public string? StudentNumber { get; set; }

        [StringLength(50)]
        [Display(Name = "Staff number")]
        public string? StaffNumber { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
