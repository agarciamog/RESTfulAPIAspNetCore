using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/* BookDto.cs
 * We extended our BookDto. Now each book will
 * have it's own List<LinkDto>.
 */

namespace Library.API.Models
{
    public class BookDto : LinkedResourceBaseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid AuthorId { get; set; }
    }
}
