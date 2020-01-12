using AutoMapper;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ILibraryRepository libraryRepository;
        private readonly ILogger<BooksController> logger;
        private readonly IUrlHelper urlHelper;

        public BooksController(ILibraryRepository libraryRepository, 
            ILogger<BooksController> logger,
            IUrlHelper urlHelper)
        {
            this.libraryRepository = libraryRepository;
            this.logger = logger;
            this.urlHelper = urlHelper;
        }

        [HttpGet(Name = "GetBooksForAuthor")]
        public async Task<IActionResult> GetBooksForAuthor(Guid authorId)
        {
            if (!(await libraryRepository.AuthorExists(authorId)))
            {
                return NotFound();
            }

            var booksForAuthorFromRepo = await libraryRepository.GetBooksForAuthor(authorId);

            var booksForAuthor = Mapper.Map<IEnumerable<BookDto>>(booksForAuthorFromRepo);

            // Iterate through our books collection and create links for each. 
            booksForAuthor = booksForAuthor.Select(book =>
            {
                book = CreateLinksForBook(book);
                return book;
            });

            // We wrap our books collection so that our author also has a collection
            // of links.
            var booksWrapper = new LinkedCollectionResourceWrapperDto<BookDto>(booksForAuthor);

            // We add our link to api/authors/{authorId}/books
            return Ok(CreateLinksForBooks(booksWrapper));
        }

        [HttpGet("{id}", Name ="GetBookForAuthor")]
        public async Task<IActionResult> GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!(await libraryRepository.AuthorExists(authorId)))
                return NotFound();

            var bookForAuthorFromRepo = await libraryRepository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
                return NotFound();

            var bookForAuthor = Mapper.Map<BookDto>(bookForAuthorFromRepo);

            return Ok(CreateLinksForBook(bookForAuthor));
        }

        [HttpPost(Name = "CreateBookForAuthor")]
        public async Task<IActionResult> CreateBookForAuthor(Guid authorId,
            [FromBody] BookForCreationDto book)
        {
            if (book == null)
                return BadRequest(); // 400

            // Validation
            ////////////////////////////
            if (book.Description == book.Title)
            {
                // Our custom code validation should go after a bad request,
                // and before we check for IsValid and process. If the tile
                // and description are the same, we add a model error.
                ModelState.AddModelError(nameof(BookForCreationDto),
                    "The Provided description should be different from the title");
            }

            // We check if our ModelState IsValid, for example if any of the requirements
            // estabilished in our BookForCreationDto in the form of annotations fail,
            // the state of IsValid will be false. We pass our model to our ObjectResult
            // constructor and the rest is handled by the framework.
            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState); // 422
            ////////////////////////////

            if (!(await libraryRepository.AuthorExists(authorId)))
                return NotFound();

            var bookForAuthorEntity = Mapper.Map<Book>(book);

            await libraryRepository.AddBookForAuthor(authorId, bookForAuthorEntity);

            if (!(await libraryRepository.Save()))
                throw new Exception("Failed to save author. Try again.");

            var bookToReturn = Mapper.Map<BookDto>(bookForAuthorEntity);

            return CreatedAtRoute("GetBookForAuthor",
                new { authorId = authorId, id = bookToReturn.Id },
                CreateLinksForBook(bookToReturn));
        }

        [HttpDelete("{id}", Name = "DeleteBookForAuthor")]
        public async Task<IActionResult> DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!(await libraryRepository.AuthorExists(authorId)))
                return NotFound();

            var bookForAuthorFromRepo = await libraryRepository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
                return NotFound();

            libraryRepository.DeleteBook(bookForAuthorFromRepo);

            if (!(await libraryRepository.Save()))
                throw new Exception("Failed to delete book on save.");

            logger.LogInformation(100, $"Book {id} for {authorId} wasd deleted.");

            return NoContent(); // 204 Success, but no content
        }

        [HttpPut("{id}", Name = "UpdateBookForAuthor")]
        public async Task<IActionResult> UpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] BookForUpdateDto book)
        {
            if (book == null)
                return BadRequest();

            if (book.Title == book.Description)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The Provided description should be different from the title");
            }

            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState); // 422

            if (!(await libraryRepository.AuthorExists(authorId)))
                return NotFound();

            var bookForAuthorFromRepo = await libraryRepository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                var bookToAdd = Mapper.Map<Book>(book);
                bookToAdd.Id = id;

                await libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if(!(await libraryRepository.Save()))
                {
                    throw new Exception("Failed creating book for authoer on save.");
                }

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor",
                    new { authorId = bookToReturn.AuthorId, id = bookToReturn.Id },
                    CreateLinksForBook(bookToReturn));
            }

            // maps the input book to our entity
            Mapper.Map(book, bookForAuthorFromRepo);

            libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if(!(await libraryRepository.Save()))
                throw new Exception("failed to update book(s) for author.");

            return NoContent();
        }

        [HttpPatch("{id}", Name = "PartiallyUpdateBookForAuthor")]
        public async Task<IActionResult> PartiallyUpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();

            if (!(await libraryRepository.AuthorExists(authorId)))
                return NotFound();

            var bookForAuthorFromRepo = await libraryRepository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                var bookForUpdateDto = new BookForUpdateDto();

                patchDoc.ApplyTo(bookForUpdateDto, ModelState);

                if(bookForUpdateDto.Title == bookForUpdateDto.Description)
                {
                    ModelState.AddModelError(nameof(bookForUpdateDto),
                        "The Provided description should be different from the title.");
                }

                TryValidateModel(bookForUpdateDto);

                if (!ModelState.IsValid)
                    return new UnprocessableEntityObjectResult(ModelState); // 422

                var bookToAdd = Mapper.Map<Book>(bookForUpdateDto);
                bookToAdd.Id = id;

                await libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if(!(await libraryRepository.Save()))
                {
                    throw new Exception("Failed to upsert book for author on save.");
                }

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);
                return CreatedAtRoute("GetAuthorForBook",
                    new { authorId = bookToAdd.AuthorId, id = bookToAdd.Id },
                    CreateLinksForBook(bookToReturn));
            }

            var bookToPatch = Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

            //patchDoc.ApplyTo(bookToPatch, ModelState);
            patchDoc.ApplyTo(bookToPatch);

            if (bookToPatch.Title == bookToPatch.Description)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The Provided description should be different from the title");
            }

            TryValidateModel(bookToPatch);

            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState); // 422

            Mapper.Map(bookToPatch, bookForAuthorFromRepo);

            libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!(await libraryRepository.Save()))
                throw new Exception("Failed to patch book for author on save.");

            return NoContent();
        }

        /* 
         * When we pass a BookDto we'll add it's resources to our List<LinkDto>
         * and we return the book. Remember each book now has it's own Links list.
         */

        private BookDto CreateLinksForBook(BookDto book)
        {
            book.Links.Add(
                new LinkDto(urlHelper.Link("GetBookForAuthor", new { authorId = book.AuthorId, id = book.Id }),
                "self",
                "GET"));

            book.Links.Add(
                new LinkDto(urlHelper.Link("DeleteBookForAuthor", new { authorId = book.AuthorId, id = book.Id }),
                "delete_book",
                "DELETE"));

            book.Links.Add(
                new LinkDto(urlHelper.Link("UpdateBookForAuthor", new { authorId = book.AuthorId, id = book.Id}),
                "update_book",
                "PUT"));

            book.Links.Add(
                new LinkDto(urlHelper.Link("PartiallyUpdateBookForAuthor", new { authorId = book.AuthorId, id = book.Id}),
                "partially_update_book",
                "PATCH"));

            return book;
        }

        /* 
         * We add our link for api/author/{authorId}/books
         */
        private LinkedCollectionResourceWrapperDto<BookDto> CreateLinksForBooks(
            LinkedCollectionResourceWrapperDto<BookDto> booksWrapper)
        {
            // link to self
            // Don't need authorId since our urlHelper already knows about it,
            // but I added it for clarity. Could have just been new { }.
            booksWrapper.Links.Add(
                new LinkDto(urlHelper.Link("GetBooksForAuthor", new { authorId = booksWrapper.Value.First().AuthorId}),
                "self",
                "GET"));
            return booksWrapper;
        }
    }
}
