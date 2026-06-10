using System;
using System.Collections.Generic;
using Domains;

namespace Domains 
{
    public class TbRefreshToken : BaseTable
    {


        public string Token { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public DateTime Expires { get; set; }

        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; } // Why was it revoked

       


    }
}



