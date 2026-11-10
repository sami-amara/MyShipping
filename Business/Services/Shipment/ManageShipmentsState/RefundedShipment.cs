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
    public class RefundedShipment : IShipmentStateHandler
    {
        private readonly IShipmentCommand _shipment;
        private readonly IShipmentsStatus _status;
        private readonly IShipmentQuery _shipmentQuery;
        private readonly IUserService _userService;

        public RefundedShipment(
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

        public ShipmentStatusEnum TargetState => ShipmentStatusEnum.Refunded;

        public async Task HandleState(ShippmentDto shipment)
        {
            // 1. Validate existence
            var currentShipment = await _shipmentQuery.GetByIdAsync(shipment.Id);
            if (currentShipment == null)
                throw new InvalidOperationException("Shipment not found.");

            // 2. Business validation: Only PAID shipments can be refunded
            if (!currentShipment.IsPaid)
            {
                throw new InvalidOperationException("Cannot refund an unpaid shipment.");
            }

            // 3. Status validation: Cannot refund if already Shipped, Delivered, or Cancelled
            if (currentShipment.CurrentState == (int)ShipmentStatusEnum.Shipped ||
                currentShipment.CurrentState == (int)ShipmentStatusEnum.Delivered ||
                currentShipment.CurrentState == (int)ShipmentStatusEnum.Cancelled ||
                currentShipment.CurrentState == (int)ShipmentStatusEnum.Returned)
            {
                throw new InvalidOperationException("Shipments that are shipped, delivered, or already closed cannot be refunded.");
            }

            // 4. Update shipment via command
            var userId = _userService.GetLoggedInUser();
            var reason = shipment.RefundReason ?? "Refunded only";

            await _shipment.Refunded(shipment.Id, userId, reason);

            // 5. Update state & history
            await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            await _status.Add(shipment.Id, TargetState);
        }
    }
}
