using Business.Contracts.Shipment;
using Business.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.Shipment.ManageShipmentsState
{
    public class ApproveShipment : IShipmentStateHandler
    {
        IShipmentCommand _shipment;
        IShipmentsStatus _status;
        IShipmentQuery _shipmentQuery;

        public ApproveShipment(IShipmentCommand shipment, IShipmentsStatus status, IShipmentQuery shipmentQuery)
        {
            _shipment = shipment;
            _status = status;
            _shipmentQuery = shipmentQuery;
        }

        public ShipmentStatusEnum TargetState { get => ShipmentStatusEnum.Approved; }

        public async Task HandleState(ShippmentDto shipment)
        {
            // ? PAYMENT VALIDATION: Shipment MUST be paid before approval
            // Load current shipment from database to get accurate IsPaid status
            var currentShipment = await _shipmentQuery.GetByIdAsync(shipment.Id);
            if (currentShipment == null)
                throw new InvalidOperationException("Shipment not found.");
            if (!currentShipment.IsPaid)
            {
                throw new InvalidOperationException("Shipment must be paid before it can be approved.");
            }

            // ? Optional: Validate that shipment is in Created or Updated status
            // Uncomment if you want to enforce this business rule
            // if (currentShipment.CurrentState != (int)ShipmentStatusEnum.Created && 
            //     currentShipment.CurrentState != (int)ShipmentStatusEnum.Updated);
            // {
            //     throw new InvalidOperationException("Only shipments in Created or Updated status can be approved.");
            // }

            await _shipment.Edit(shipment);
            await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            await _status.Add(shipment.Id, TargetState);
        }
    }
}
