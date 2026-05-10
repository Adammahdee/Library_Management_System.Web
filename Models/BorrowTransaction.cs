using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Library_Management_System.Web.Models
{
    public class BorrowTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        [Required]
        public int BookId { get; set; }

        public Book Book { get; set; } = null!;

        [Required]
        public DateTime BorrowDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        [Required]
        public string Status { get; set; } = "Borrowed";

        public ICollection<Fine> Fines { get; set; }
            = new List<Fine>();

        public bool IsOverdue
        {
            get
            {
                return ReturnDate == null && DateTime.Now.Date > DueDate.Date;
            }
        }
    }
}