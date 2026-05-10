using System.Collections.Generic;

namespace Library_Management_System.Web.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }
            = string.Empty;

        public ICollection<Book> Books { get; set; }
            = new List<Book>();
    }
}