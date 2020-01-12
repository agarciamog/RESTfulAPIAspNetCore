using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authorscollection")]
    public class AuthorsCollectionController : ControllerBase
    {
        private readonly ILibraryRepository libraryRepository;

        public AuthorsCollectionController(ILibraryRepository libraryRepository)
        {
            this.libraryRepository = libraryRepository;
        }

        [HttpPost()]
        public async Task<IActionResult> CreateAuthorsColletion(
            [FromBody] IEnumerable<AuthorForCreationDto> authors)
        {
            if(authors == null)
            {
                return BadRequest();
            }

            var authorsEntities = Mapper.Map<IEnumerable<Author>>(authors);

            foreach (var author in authorsEntities)
            {
                libraryRepository.AddAuthor(author);
            }

            if(!(await libraryRepository.Save()))
            {
                throw new Exception("Creating authors failed on save.");
            }

            var authorCollectionToReturn = 
                Mapper.Map<IEnumerable<AuthorDto>>(authorsEntities);

            var idsAsString = string.Join(",",
                authorCollectionToReturn.Select(x => x.Id));

            return CreatedAtRoute("GetAuthorCollection",
                new { ids = idsAsString },
                authorCollectionToReturn);
        }
        
        [HttpGet("({ids})", Name = "GetAuthorCollection")]
        public async Task<IActionResult> GetAuthorCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {
            if (ids == null)
                BadRequest();

            var authorEntities = await libraryRepository.GetAuthors(ids);

            if (ids.Count() != authorEntities.Count())
                return NotFound();

            var authorsToReturn = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);

            return Ok(authorsToReturn);
        }
    }
}