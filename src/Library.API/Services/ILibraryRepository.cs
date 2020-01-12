using Library.API.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Library.API.Helpers;

namespace Library.API.Services
{
    public interface ILibraryRepository
    {
        Task<PagedList<Author>> GetAuthors(AuthorsResourceParameter authorsResourceParameter);
        Task<Author> GetAuthor(Guid authorId);
        Task<IEnumerable<Author>> GetAuthors(IEnumerable<Guid> authorIds);
        void AddAuthor(Author author);
        void DeleteAuthor(Author author);
        void UpdateAuthor(Author author);
        Task<bool> AuthorExists(Guid authorId);
        Task<IEnumerable<Book>> GetBooksForAuthor(Guid authorId);
        Task<Book> GetBookForAuthor(Guid authorId, Guid bookId);
        Task AddBookForAuthor(Guid authorId, Book book);
        void UpdateBookForAuthor(Book book);
        void DeleteBook(Book book);
        Task<bool> Save();
    }
}
