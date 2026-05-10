using System;

namespace Library_Management_System.Web.Models
{
    public class Reservation
    {
        public int ReservationId { get; set; }

        public string UserId { get; set; }
            = string.Empty;

        public ApplicationUser User { get; set; }
            = null!;

        public int BookId { get; set; }

        public Book Book { get; set; }
            = null!;

        public DateTime ReservationDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public string Status { get; set; }
            = string.Empty;
    }
}
