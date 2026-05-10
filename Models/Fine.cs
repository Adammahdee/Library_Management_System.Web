using System;
using System.ComponentModel.DataAnnotations;

namespace Library_Management_System.Web.Models
{
    public class Fine
    {
        [Key]
        public int FineId { get; set; }

        [Required]
        public int TransactionId { get; set; }

        public BorrowTransaction BorrowTransaction { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Paid Status")]
        public bool IsPaid { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}