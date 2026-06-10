using Business.Services.Shipment.ManageShipmentsState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Contracts.Shipment
{
    public interface IShipmentStateHandlerFactory
    {
        IShipmentStateHandler GetHandler(ShipmentStatusEnum status);
    }
}
