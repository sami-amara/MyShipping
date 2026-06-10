



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Model
{
    // Simple pager result used by repository GetPagedList
    public class PagedResult<T>
    {


        //public List<T> Items { get; set; } = new();
        //public int TotalCount { get; set; }
        public int Page { get; set; }
        //public int PageSize { get; set; }
        //public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling((double)TotalCount / PageSize) : 0;


        public List<T> Items { get; set; }
        public int PageNumber { get; set; }

        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}

