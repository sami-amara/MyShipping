using Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOS
{
    public partial class ShippmentStatusDto : BaseDto
    {
        public Guid? ShippmentId { get; set; }

        public string? Notes { get; set; }

    }
}
