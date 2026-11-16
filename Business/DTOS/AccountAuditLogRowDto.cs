using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOS
{
    public class AccountAuditLogRowDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = "";
        public string Action { get; set; } = "";
        public string? Reason { get; set; }
        public string InitiatedBy { get; set; } = "";
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserEmail { get; set; } = "";
        public string UserName { get; set; } = "";
    }
}
