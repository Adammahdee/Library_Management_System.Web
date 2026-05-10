using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Library_Management_System.Web.Models
{
    public class Publisher
    {
        [Key]
        public int PublisherId { get; set; }

        [Required(ErrorMessage = "Publisher name is required")]
        [StringLength(150)]
        [Display(Name = "Publisher Name")]
        public string PublisherName { get; set; }
            = string.Empty;

        [StringLength(255)]
        public string? Address { get; set; }

        [Phone]
        [StringLength(30)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        public ICollection<Book> Books { get; set; }
            = new List<Book>();
    }
}