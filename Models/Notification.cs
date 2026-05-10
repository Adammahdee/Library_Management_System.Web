using System;

namespace Library_Management_System.Web.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }

        public string UserId { get; set; }
            = string.Empty;

        public ApplicationUser User { get; set; }
            = null!;

        public string Title { get; set; }
            = string.Empty;

        public string Message { get; set; }
            = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
