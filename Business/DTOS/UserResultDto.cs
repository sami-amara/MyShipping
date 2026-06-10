using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOS
{
    public class UserResultDto 
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        //public string[] Errors { get; set; }
        public IEnumerable<string> Errors { get; set; }


        // ✅ ADD THESE PROPERTIES
        public bool IsLockedOut { get; set; }
        public bool IsAdminLocked { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }

    }
}
