using DataAccessLayer.Model;
using UI.Models;

namespace UI.Helpers
{
    public static class PaginationHelper
    {
        // Convert a PagedResult<T> (server) into UI PaginationInfo with numeric window
        public static PaginationInfo ToPaginationInfo<T>(this PagedResult<T> paged, int windowSize = 7)
        {
            var page = paged?.Page > 0 ? paged.Page : 1;
            var pageSize = paged?.PageSize > 0 ? paged.PageSize : 10;
            var totalCount = paged?.TotalCount ?? 0;
            var totalPages = pageSize > 0 ? Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize)) : 1;

            var half = windowSize / 2;
            var start = Math.Max(1, page - half);
            var end = Math.Min(totalPages, start + windowSize - 1);
            if (end - start + 1 < windowSize)
                start = Math.Max(1, end - windowSize + 1);

           return new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Start = start,
                End = end,
                PrevPage = Math.Max(1, page - 1),
                NextPage = Math.Min(totalPages, page + 1)
           };
        }
    }
}











//using DataAccessLayer.Model;

//namespace UI.Helpers
//{
//    public static class PaginationHelper
//    {
//        // Convert a PagedResult<T> (server) into UI PaginationInfo with numeric window
//        public static PaginationInfo ToPaginationInfo<T>(this PagedResult<T> paged, int windowSize = 7)
//        {
//            var page = paged?.Page > 0 ? paged.Page : 1;
//            var pageSize = paged?.PageSize > 0 ? paged.PageSize : 10;
//            var totalCount = paged?.TotalCount ?? 0;
//            var totalPages = pageSize > 0 ? Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize)) : 1;

//            var half = windowSize / 2;
//            var start = Math.Max(1, page - half);
//            var end = Math.Min(totalPages, start + windowSize - 1);
//            if (end - start + 1 < windowSize)
//                start = Math.Max(1, end - windowSize + 1);

//            return new PaginationInfo
//            {
//                Page = page,
//                PageSize = pageSize,
//                TotalCount = totalCount,
//                TotalPages = totalPages,
//                Start = start,
//                End = end,
//                PrevPage = Math.Max(1, page - 1),
//                NextPage = Math.Min(totalPages, page + 1)
//            };
//        }
//    }
//}
