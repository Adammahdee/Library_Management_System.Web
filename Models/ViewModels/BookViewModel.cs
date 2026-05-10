using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Library_Management_System.Web.Models.ViewModels
{
    public class BookViewModel
    {
        public int BookId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "ISBN is required")]
        public string ISBN { get; set; } = string.Empty;

        [Display(Name = "Total Stock")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a positive number")]
        public int Quantity { get; set; }

        [Display(Name = "Available")]
        public int AvailableQuantity { get; set; }

        [Display(Name = "Category")]
        [Required(ErrorMessage = "Please select a category")]
        public int CategoryId { get; set; }

        public string? CategoryName { get; set; }

        [Display(Name = "Publisher")]
        public int? PublisherId { get; set; }

        public string? PublisherName { get; set; }

        public int[]? AuthorIds { get; set; }

        public List<string>? AuthorNames { get; set; }

        public IEnumerable<SelectListItem>? Categories { get; set; }
    }
}