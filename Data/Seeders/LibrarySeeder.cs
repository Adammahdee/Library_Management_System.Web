using Library_Management_System.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Data.Seeders
{
    public static class LibrarySeeder
    {
        public static async Task SeedSampleCatalogAsync(ApplicationDbContext context)
        {
            if (await context.Books.AnyAsync())
            {
                return;
            }

            var category = await context.Categories.FirstOrDefaultAsync();
            if (category == null)
            {
                category = new Category { CategoryName = "General" };
                context.Categories.Add(category);
                await context.SaveChangesAsync();
            }

            var publisher = await context.Publishers.FirstOrDefaultAsync();
            if (publisher == null)
            {
                publisher = new Publisher
                {
                    PublisherName = "Default Publisher",
                    Email = "publisher@example.com"
                };
                context.Publishers.Add(publisher);
                await context.SaveChangesAsync();
            }

            var author = await context.Authors.FirstOrDefaultAsync();
            if (author == null)
            {
                author = new Author { AuthorName = "Default Author" };
                context.Authors.Add(author);
                await context.SaveChangesAsync();
            }

            var book = new Book
            {
                Title = "Sample Seed Book",
                ISBN = $"SEED-{DateTime.UtcNow:yyyyMMddHHmmss}",
                TotalCopies = 5,
                AvailableCopies = 5,
                CategoryId = category.CategoryId,
                PublisherId = publisher.PublisherId
            };

            context.Books.Add(book);
            await context.SaveChangesAsync();

            context.BookAuthors.Add(new BookAuthor
            {
                BookId = book.BookId,
                AuthorId = author.AuthorId
            });

            await context.SaveChangesAsync();
        }
    }
}
