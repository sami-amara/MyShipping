using Business.Contracts.Shipment;
using Business.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.Shipment.ManageShipmentsState
{
    public class ReturnedShipment : IShipmentStateHandler

    {
        IShipmentCommand _shipment;
        IShipmentsStatus _status;
        public ReturnedShipment(IShipmentCommand shipment, IShipmentsStatus status)
        {
            _shipment = shipment;
            _status = status;
        }

        public ShipmentStatusEnum TargetState { get => ShipmentStatusEnum.Returned; }

        public async Task HandleState(ShippmentDto shipment)
        {
            await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            await _status.Add(shipment.Id, TargetState);
        }
    }
}
