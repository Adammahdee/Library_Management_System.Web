using System;
using System.ComponentModel.DataAnnotations;

namespace Library_Management_System.Web.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = string.Empty; // e.g., "Borrow", "Return", "Reservation", "Cancellation", "Fine Assessment", "Fine Payment"

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public decimal? Amount { get; set; }

        public string? Description { get; set; }
    }
}