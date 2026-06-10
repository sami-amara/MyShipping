using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Payments.PayPal
{
    public class ShipmentRefundRequest
    {
        public Guid ShipmentId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
