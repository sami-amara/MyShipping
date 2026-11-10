using Business.Contracts;
using Business.Contracts.Shipment;
using Business.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Business.Services.Shipment.ManageShipmentsState
{
    public class DelivredShipment : IShipmentStateHandler
    {
        IShipmentCommand _shipment;
        IShipmentsStatus _status;
        IShipmentQuery _shipmentQuery;

        IUserService _userService;
        public DelivredShipment(IShipmentCommand shipment, IShipmentsStatus status, 
            IShipmentQuery shipmentQuery, IUserService userService)
        {
            _shipment = shipment;
            _status = status;
            _shipmentQuery = shipmentQuery;
            _userService = userService;
        }

        public ShipmentStatusEnum TargetState { get => ShipmentStatusEnum.Delivered; }

        public async Task HandleState(ShippmentDto shipment)
        {
            // 1. Payment & existence validation
            var currentShipment = await _shipmentQuery.GetByIdAsync(shipment.Id);
            if (currentShipment == null)
                throw new InvalidOperationException("Shipment not found.");
            if (!currentShipment.IsPaid)
            {
                throw new InvalidOperationException("Shipment must be paid before it can be delivered.");
            }
            // 2. Get the logged-in user and update audit via command
            var userId = _userService.GetLoggedInUser();
            await _shipment.Delivered(shipment.Id, userId);
            // 3. Update status & history
            await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            await _status.Add(shipment.Id, TargetState);
        }
    }
}
