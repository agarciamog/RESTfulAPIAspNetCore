using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/* LinkedResourceBaseDto.cs
 * A List of LinkDto. Each book will have it's own list
 * of links.
 */

namespace Library.API.Models
{
    public abstract class LinkedResourceBaseDto
    {
        public List<LinkDto> Links { get; set; } = new List<LinkDto>();
    }
}
