using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOS
{
    public partial class RefreshTokenDto : BaseDto
    {

        public string Token { get; set; }

        public string UserId { get; set; }

        public DateTime Expires { get; set; }

        public int CurrentState { get; set; }

        // ✅ ADD THESE NEW FIELDS:
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string RevokedReason { get; set; } // Why was it revoked



    }
}
