using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.Shipment.ManageShipmentsState
{
    public enum ShipmentStatusEnum
    {
        Deleted = 0,
        Created = 1,
        Updated = 2,
        Approved = 3,
        ReadyForShipping = 4,
        Shipped = 5,
        Delivered = 6,
        Cancelled = 7,
        Returned = 8,
        Refunded = 9
    }
}
