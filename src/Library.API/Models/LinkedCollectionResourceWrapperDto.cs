using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/* LinkedCollectionResourceWrapperDto.cs
 * When we perform a GET on http://localhost:6058/api/authors/7
 * we get a list information on the resource and resource actions
 * on each book, but we are missing a link back to our resource,
 * http://localhost:6058/api/authors/7. This class will wrap
 * our BooksDto resource. We inherit LinkedResourceBaseDto, just
 * like BookDto. This means that our resource will now have a
 * list of links as well.
 * {
 *   "value": [
 *       {
 *           "id": "2",
 *           "title": "A Dance with Dragons",
 *           "description": "Some text",
 *           "authorId": "6",
 *           "links": [ ] // links here
 *       },
 *       {
 *           "id": "2",
 *           "title": "A Game of Thrones",
 *           "description": "Some text.",
 *           "authorId": "6",
 *           "links": [ ] // links here 
 *       }
 *   ],
 *   "links": [
 *       {
 *           "href": "http://localhost:6058/api/authors/6/books",
 *           "rel": "self",
 *           "method": "GET"
 *       }
 *   ]
 * }
 */

namespace Library.API.Models
{
    public class LinkedCollectionResourceWrapperDto<T> : LinkedResourceBaseDto
        where T : LinkedResourceBaseDto // T must inherit from LinkedResourceBaseDto
    {
        public IEnumerable<T> Value { get; set; }

        public LinkedCollectionResourceWrapperDto(IEnumerable<T> value)
        {
            Value = value;
        }
    }
}
