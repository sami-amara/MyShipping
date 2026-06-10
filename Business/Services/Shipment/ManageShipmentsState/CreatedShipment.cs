using Business.Contracts.Shipment;
using Business.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.Shipment.ManageShipmentsState
{
    public class CreatedShipment : IShipmentStateHandler
    {
        private readonly IShipmentCommand _shipment;
        private readonly IShipmentsStatus _status;

        public CreatedShipment(IShipmentCommand shipment, IShipmentsStatus status)
        {
            _shipment = shipment;
            _status = status;
        }

        public ShipmentStatusEnum TargetState { get => ShipmentStatusEnum.Created; }

        public async Task HandleState(ShippmentDto shipment)
        {
            await _shipment.Create(shipment);
        }
    }
}
