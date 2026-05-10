using System.Collections.Generic;

namespace Library_Management_System.Web.Models
{
    public class Book
    {
        public int BookId { get; set; }

        public string Title { get; set; }
            = string.Empty;

        public string ISBN { get; set; }
            = string.Empty;

        public int TotalCopies { get; set; }

        public int AvailableCopies { get; set; }

        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public int? PublisherId { get; set; }

        public Publisher? Publisher { get; set; }

        public ICollection<BookAuthor> BookAuthors
            { get; set; } = new List<BookAuthor>();

        public ICollection<BorrowTransaction>
            BorrowTransactions { get; set; }
            = new List<BorrowTransaction>();

        public ICollection<Reservation>
            Reservations { get; set; }
            = new List<Reservation>();
    }
}