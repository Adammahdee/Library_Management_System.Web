using System;

namespace Library_Management_System.Web.Models.ViewModels
{
    public class AuditLogViewModel
    {
        public int AuditLogId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public DateTime LogDate { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; } // To display user email instead of just ID
        public string? Description { get; set; }
    }
}