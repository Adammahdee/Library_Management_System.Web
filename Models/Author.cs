using System.Collections.Generic;

namespace Library_Management_System.Web.Models
{
    public class Author
    {
        public int AuthorId { get; set; }

        public string AuthorName { get; set; }
            = string.Empty;

        public ICollection<BookAuthor>
            BookAuthors { get; set; }
            = new List<BookAuthor>();
    }
}