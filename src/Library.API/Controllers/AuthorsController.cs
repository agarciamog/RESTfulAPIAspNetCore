using Library.API.Models;
using Library.API.Services;
using Library.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Library.API.Entities;
using Microsoft.AspNetCore.Http;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : ControllerBase
    {
        private readonly ILibraryRepository libraryRepository;
        private readonly IUrlHelper urlHelper;
        private readonly IPropertyMappingService propertyMappingService;
        private readonly ITypeHelperService typeHelperService;

        public AuthorsController(ILibraryRepository libraryRepository, 
            IUrlHelper urlHelper,
            IPropertyMappingService propertyMappingService,
            ITypeHelperService typeHelperService)
        {
            this.libraryRepository = libraryRepository;
            this.urlHelper = urlHelper;
            this.propertyMappingService = propertyMappingService;
            this.typeHelperService = typeHelperService;
        }

        [HttpGet(Name = "GetAuthors")]
        [HttpHead]
        public async Task<IActionResult> GetAuthors([FromQuery] AuthorsResourceParameter authorsResourceParameter,
            [FromHeader(Name = "Accept")] string mediaType) // accept header for getting the media type
        {
            if(!propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameter.OrderBy))
            {
                return BadRequest();
            }

            if(!typeHelperService.TypeHasProperties<AuthorDto>(authorsResourceParameter.Fields))
            {
                return BadRequest();
            }

            var authorsFromRepo = await libraryRepository.GetAuthors(authorsResourceParameter);

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            if (mediaType == "application/vnd.marvin.hateoas+json")
            {
                var paginationMetaData = new
                {
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages
                };

                Response.Headers.Add("X-Pagination",
                    Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetaData));

                // Links for each author for DELETE, POST, GET
                var links = CreateLinksForAuthors(authorsResourceParameter,
                    authorsFromRepo.HasNext, authorsFromRepo.HasPrevious);

                var shapedAuthors = authors.ShapeData(authorsResourceParameter.Fields);

                // Links for each author for DELETE, POST, GET
                var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
                {
                    var authorAsDictionary = author as IDictionary<string, object>;
                    var authorLinks = CreateLinksForAuthor(
                        (Guid)authorAsDictionary["Id"], authorsResourceParameter.Fields);

                    authorAsDictionary.Add("links", authorLinks);

                    return authorAsDictionary;
                });

                var linkedCollectionResource = new
                {
                    value = shapedAuthorsWithLinks,
                    links
                };

                return Ok(linkedCollectionResource);
            }
            else
            {
                var previousPageLink = authorsFromRepo.HasPrevious ?
                    CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.PreviousPage) :
                    null;

                var nextPageLink = authorsFromRepo.HasNext ?
                    CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.NextPage) :
                    null;

                var paginationMetaData = new
                {
                    previousPageLink,
                    nextPageLink,
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages
                };

                Response.Headers.Add("X-Pagination",
                    Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetaData));

                return Ok(authors.ShapeData(authorsResourceParameter.Fields));
            }
            
        }

        /* Creates a links for pagination
         */
        private string CreateAuthorsResourceUri(
            AuthorsResourceParameter authorsResourceParameter,
            ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameter.Fields,
                            orderBy = authorsResourceParameter.OrderBy,
                            searchQuery = authorsResourceParameter.SearchQuery,
                            genre = authorsResourceParameter.Genre,
                            pageNumber = authorsResourceParameter.PageNumber - 1,
                            pageSize = authorsResourceParameter.PageSize
                        }); ;
                case ResourceUriType.NextPage:
                    return urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameter.Fields,
                            orderBy = authorsResourceParameter.OrderBy,
                            searchQuery = authorsResourceParameter.SearchQuery,
                            genre = authorsResourceParameter.Genre,
                            pageNumber = authorsResourceParameter.PageNumber + 1,
                            pageSize = authorsResourceParameter.PageSize
                        });
                case ResourceUriType.Current:
                default:
                    return urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameter.Fields,
                            orderBy = authorsResourceParameter.OrderBy,
                            searchQuery = authorsResourceParameter.SearchQuery,
                            genre = authorsResourceParameter.Genre,
                            pageNumber = authorsResourceParameter.PageNumber,
                            pageSize = authorsResourceParameter.PageSize
                        });
            }
        }

        [HttpGet("{id}", Name ="GetAuthor")]
        public async Task<IActionResult> GetAuthor([FromRoute] Guid id, [FromQuery] string fields)
        {
            /* Now that we've create CreateLinksForAuthor, which will return
             * the links for our shapped data, we will add the links and return
             * them as part of our shapped data.
             */

            if(!typeHelperService.TypeHasProperties<AuthorDto>(fields))
                return BadRequest();

            var authorFromRepo = await libraryRepository.GetAuthor(id);

            if (authorFromRepo == null)
                return NotFound();

            var author = Mapper.Map<AuthorDto>(authorFromRepo);

            // We'll use these links in our response
            var links = CreateLinksForAuthor(id, fields);

            // ShapeDate returns ExpandoObject, but it's also just an IDictionary<string, object>
            var linkedResourceToReturn = author.ShapeData(fields) as IDictionary<string, object>;

            // Added our links to the return resource
            linkedResourceToReturn.Add("links", links);

            return Ok(linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesMediaType("Content-Type", 
            new[] { "application/vnd.marvin.author.full+json",
            "application/vnd.marvin.author.full+xml"})]
        public async Task<IActionResult> CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
                return BadRequest(); // 400

            var authorEntity = Mapper.Map<Author>(author);

            libraryRepository.AddAuthor(authorEntity);

            if(!(await libraryRepository.Save()))
            {
                throw new Exception("Creating an author failed on save.");
                // return StatusCode(500, "Creating an author failed on save.");
            }

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            /* We'll do the save as with GetAuthor, expect
             * we'll pass null for the fields when shapping the data.
             */
            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            var linkedResourceToReturn = authorToReturn.ShapeData(null) as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            // Since linkedResourceToReturn is a dictionary, we should use ["Id"]
            return CreatedAtRoute("GetAuthor",
                new { id = linkedResourceToReturn["Id"] },
                linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
        [RequestHeaderMatchesMediaType("Content-Type", 
            new [] { "application/vnd.marvin.authorwithdateofdeath.full+json",
            "application/vnd.marvin.authorwithdateofdeath.full+xml"})]
        //[RequestHeaderMatchesMediaType("Accept", new[] { "application/json" })] // multiples because AttributeUsage > AllowMultiples
        public async Task<IActionResult> CreateAuthorWithDateOfDeath(
            [FromBody] AuthorForCreationWithDateOfDeathDto author)
        {
            if (author == null)
                return BadRequest(); // 400

            var authorEntity = Mapper.Map<Author>(author);

            libraryRepository.AddAuthor(authorEntity);

            if (!(await libraryRepository.Save()))
            {
                throw new Exception("Creating an author failed on save.");
                // return StatusCode(500, "Creating an author failed on save.");
            }

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            /* We'll do the save as with GetAuthor, expect
             * we'll pass null for the fields when shapping the data.
             */
            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            var linkedResourceToReturn = authorToReturn.ShapeData(null) as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            // Since linkedResourceToReturn is a dictionary, we should use ["Id"]
            return CreatedAtRoute("GetAuthor",
                new { id = linkedResourceToReturn["Id"] },
                linkedResourceToReturn);
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> BlockedAuthorCreation(Guid id)
        {
            if (await libraryRepository.AuthorExists(id))
                return new StatusCodeResult(StatusCodes.Status409Conflict);

            return NotFound();
        }

        [HttpDelete("{id}", Name = "DeleteAuthor")]
        public async Task<IActionResult> DeleteAuthor(Guid id)
        {
            if (!(await libraryRepository.AuthorExists(id)))
                return NotFound();

            var authorFromRepo = await libraryRepository.GetAuthor(id);

            libraryRepository.DeleteAuthor(authorFromRepo);

            if (!(await libraryRepository.Save()))
                throw new Exception("Failed to delete author on save.");

            return NoContent();
        }

        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
        {
            /* We'll use this example to demostrate how to create links for
             * our shapped data using a List of LinkDto.
             */
            var links = new List<LinkDto>();

            /* If no fields are found, then just add the id to the route. If 
             * fields do exist, then add it to the route.
             */
            if(string.IsNullOrWhiteSpace(fields))
            {
                links.Add(new LinkDto(
                    urlHelper.Link("GetAuthor", new { id = id}),
                    "self",
                    "GET"
                ));
            }
            else
            {
                links.Add(new LinkDto(
                    urlHelper.Link("GetAuthor", new { id = id, fields = fields }),
                    "self",
                    "GET"
                ));
            }

            /* We'll add our links as we've done in the past.
             * Remember to name the routes.
             */
            links.Add(new LinkDto(
                urlHelper.Link("DeleteAuthor", new { id = id}),
                "delete_author",
                "DELETE"
            ));

            links.Add(new LinkDto(
                urlHelper.Link("CreateBookForAuthor", new { authorId = id }),
                "create_book_for_author",
                "POST"
            ));

            links.Add(new LinkDto(
                urlHelper.Link("GetBooksForAuthor", new { authorId = id }),
                "books",
                "GET"
            ));

            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForAuthors(
            AuthorsResourceParameter authorsResourceParameter,
            bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>();

            links.Add(
                new LinkDto(CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.Current),
                "self",
                "GET"
                ));

            if (hasNext)
            {
                links.Add(
                new LinkDto(CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.NextPage),
                "nextPage",
                "GET"
                ));
            }

            if (hasPrevious)
            {
                links.Add(
                new LinkDto(CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.PreviousPage),
                "previousPage",
                "GET"
                ));
            }

            return links;
        }

        [HttpOptions]
        public IActionResult GetAuthorsOptions()
        {
            Response.Headers.Add("Allow", "GET,OPTIONS,POST");
            return Ok();
        }
    }
}
