using Library_Management_System.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Web.Data.Seeders
{
    public static class LibrarySeeder
    {
        public static async Task SeedSampleCatalogAsync(ApplicationDbContext context)
        {
            await SeedDepartmentsAsync(context);

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

        private static async Task SeedDepartmentsAsync(ApplicationDbContext context)
        {
            var defaultDepartments = new[]
            {
                "Computer Science",
                "Business Administration",
                "Electrical Engineering",
                "Mechanical Engineering",
                "Mathematics"
            };

            var existingDepartmentNames = await context.Departments
                .Select(d => d.DepartmentName)
                .ToListAsync();

            var missingDepartments = defaultDepartments
                .Where(name => !existingDepartmentNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                .Select(name => new Department { DepartmentName = name })
                .ToList();

            if (missingDepartments.Count == 0)
            {
                return;
            }

            context.Departments.AddRange(missingDepartments);
            await context.SaveChangesAsync();
        }
    }
}
