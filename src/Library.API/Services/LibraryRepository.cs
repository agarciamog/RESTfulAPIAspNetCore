using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Library.API.Services
{
    public class LibraryRepository : ILibraryRepository
    {
        private LibraryContext _context;
        private readonly IPropertyMappingService _propertyMappingService;

        public LibraryRepository(LibraryContext context, IPropertyMappingService propertyMappingService)
        {
            _context = context;
            _propertyMappingService = propertyMappingService;
        }

        public void AddAuthor(Author author)
        {
            author.Id = Guid.NewGuid();
            _context.Authors.Add(author);

            // the repository fills the id (instead of using identity columns)
            if (author.Books.Any())
            {
                foreach (var book in author.Books)
                {
                    book.Id = Guid.NewGuid();
                }
            }
        }

        public async Task AddBookForAuthor(Guid authorId, Book book)
        {
            var author = await GetAuthor(authorId);
            if (author != null)
            {
                // if there isn't an id filled out (ie: we're not upserting),
                // we should generate one
                if (book.Id == Guid.Empty)
                {
                    book.Id = Guid.NewGuid();
                }
                author.Books.Add(book);
            }
        }

        public async Task<bool> AuthorExists(Guid authorId)
        {
            return await _context.Authors.AnyAsync(a => a.Id == authorId);
        }

        public void DeleteAuthor(Author author)
        {
            _context.Authors.Remove(author);
        }

        public void DeleteBook(Book book)
        {
            _context.Books.Remove(book);
        }

        public async Task<Author> GetAuthor(Guid authorId)
        {
            return await _context.Authors.FirstOrDefaultAsync(a => a.Id == authorId);
        }

        public async Task<PagedList<Author>> GetAuthors(AuthorsResourceParameter authorsResourceParameter)
        {
            var collectionBeforePaging = _context.Authors.ApplySort(authorsResourceParameter.OrderBy,
                _propertyMappingService.GetPropertyMapping<AuthorDto, Author>());

            if (!string.IsNullOrEmpty(authorsResourceParameter.Genre))
            {
                // trim and to lower case
                var genreForwhereClause = authorsResourceParameter.Genre.Trim().ToLower();
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.Genre.ToLower() == genreForwhereClause);
            }

            if (!string.IsNullOrEmpty(authorsResourceParameter.SearchQuery))
            {
                // trim and to lower case
                var searhForwhereClause = authorsResourceParameter.SearchQuery.Trim().ToLower();
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.Genre.ToLower().Contains(searhForwhereClause)
                    || a.FirstName.ToLower().Contains(searhForwhereClause)
                    || a.LastName.ToLower().Contains(searhForwhereClause));
            }

            return await PagedList<Author>.Create(collectionBeforePaging, 
                authorsResourceParameter.PageNumber, 
                authorsResourceParameter.PageSize);
        }

        public async Task<IEnumerable<Author>> GetAuthors(IEnumerable<Guid> authorIds)
        {
            return await _context.Authors.Where(a => authorIds.Contains(a.Id))
                .OrderBy(a => a.FirstName)
                .OrderBy(a => a.LastName)
                .ToListAsync();
        }

        public void UpdateAuthor(Author author)
        {
            // no code in this implementation
        }

        public async Task<Book> GetBookForAuthor(Guid authorId, Guid bookId)
        {
            return await _context.Books
              .Where(b => b.AuthorId == authorId && b.Id == bookId).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Book>> GetBooksForAuthor(Guid authorId)
        {
            return await _context.Books
                        .Where(b => b.AuthorId == authorId).OrderBy(b => b.Title).ToListAsync();
        }

        public void UpdateBookForAuthor(Book book)
        {
            _context.Entry(book).State = EntityState.Modified;
        }

        public async Task<bool> Save()
        {
            return (await _context.SaveChangesAsync() >= 0);
        }
    }
}
