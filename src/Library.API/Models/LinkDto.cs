using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/* We use LinkDto to create a list of resources available.
   For example, a GET http://localhost/api/authors/2/books/6
   will return a list of links of actions you can perform:
   {
   "id": "bc4c35c3-3857-4250-9449-155fcf5109ec",
   "title": "The Winds of Winter",
   "description": "Forthcoming 6th novel in A Song of Ice and Fire.",
   "authorId": "76053df4-6687-4353-8937-b45556748abe",
   "links": [
        {
            "href": "http://localhost:6058/api/authors/2/books/6",
            "rel": "self",
            "method": "GET"
        },
        {
            "href": "http://localhost:6058/api/authors/2/books/6",
            "rel": "delete_book",
            "method": "DELETE"
        },
        {
            "href": "http://localhost:6058/api/authors/2/books/6",
            "rel": "update_book",
            "method": "PUT"
        },
        {
            "href": "http://localhost:6058/api/authors/2/books/6",
            "rel": "partially_update_book",
            "method": "PATCH"
        }
    ]}

    In the next class, LinkedResourceBaseDto.cs we create a
    List<LinkDto>.
*/

namespace Library.API.Models
{
    public class LinkDto
    {
        public string Href { get; private set; }
        public string Rel { get; private set; }
        public string Method { get; private set; }

        public LinkDto(string href, string rel, string method)
        {
            Href = href;
            Rel = rel;
            Method = method;
        }
    }
}
