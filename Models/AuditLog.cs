using System;

namespace Library_Management_System.Web.Models
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public DateTime LogDate { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public string? Description { get; set; }
    }
}