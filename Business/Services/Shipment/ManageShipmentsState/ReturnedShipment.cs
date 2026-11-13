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
    public class ReturnedShipment : IShipmentStateHandler
    {
        private readonly IShipmentCommand _shipment;
        private readonly IShipmentsStatus _status;
        private readonly IShipmentQuery _shipmentQuery;
        private readonly IUserService _userService;

        public ReturnedShipment(
            IShipmentCommand shipment,
            IShipmentsStatus status,
            IShipmentQuery shipmentQuery,
            IUserService userService)
        {
            _shipment = shipment;
            _status = status;
            _shipmentQuery = shipmentQuery;
            _userService = userService;
        }

        public ShipmentStatusEnum TargetState => ShipmentStatusEnum.Returned;

        public async Task HandleState(ShippmentDto shipment)
        {
            if (shipment == null || shipment.Id == Guid.Empty)
                throw new ArgumentException("Shipment ID is required for return");

            // 1. Validate shipment exists
            var currentShipment = await _shipmentQuery.GetByIdAsync(shipment.Id);
            if (currentShipment == null)
                throw new InvalidOperationException("Shipment not found.");

            // 2. Business Rule: Can ONLY return shipments that were physically out (Shipped or Delivered)
            if (currentShipment.CurrentState != (int)ShipmentStatusEnum.Shipped &&
                currentShipment.CurrentState != (int)ShipmentStatusEnum.Delivered)
            {
                throw new InvalidOperationException("Only shipped or delivered shipments can be marked as returned.");
            }

            // 3. Record audit fields (ReturnedBy, ReturnedAt, ReturnReason)
            var userId = _userService.GetLoggedInUser();
            var reason = shipment.ReturnReason ?? "Package returned to warehouse";
            await _shipment.Returned(shipment.Id, userId, reason);

            // 4. Update status & history
            await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            await _status.Add(shipment.Id, TargetState);
        }
    }
}






