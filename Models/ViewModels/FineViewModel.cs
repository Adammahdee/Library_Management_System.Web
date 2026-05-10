using System;
using System.ComponentModel.DataAnnotations;

namespace Library_Management_System.Web.Models.ViewModels
{
    public class FineViewModel
    {
        public int FineId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public string BookTitle { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        [Display(Name = "Paid")]
        public bool IsPaid { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}