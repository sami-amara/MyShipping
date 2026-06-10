using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Models
{
    public sealed class PaginationInfo
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public int TotalPages { get; init; } = 1;
        public int TotalCount { get; init; } = 0;
        public int Start { get; init; } = 1;
        public int End { get; init; } = 1;
        public int PrevPage { get; init; } = 1;
        public int NextPage { get; init; } = 1;
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
