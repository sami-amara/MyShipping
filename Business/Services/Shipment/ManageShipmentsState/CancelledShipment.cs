using Business.Contracts;
using Business.Contracts.Payments;
using Business.Contracts.Shipment;
using Business.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.Shipment.ManageShipmentsState
{
    public class CancelledShipment : IShipmentStateHandler
    {
        private readonly IShipmentCommand _shipment;
        private readonly IShipmentsStatus _status;
        private readonly IShipmentQuery _shipmentQuery;
        private readonly IUserService _userService;
        private readonly IPaymentTransactionService _paymentTransactionService;

        public CancelledShipment(
            IShipmentCommand shipment,
            IShipmentsStatus status,
            IShipmentQuery shipmentQuery,
            IUserService userService,
            IPaymentTransactionService paymentTransactionService)
        {
            _shipment = shipment;
            _status = status;
            _shipmentQuery = shipmentQuery;
            _userService = userService;
            _paymentTransactionService = paymentTransactionService;
        }

        public ShipmentStatusEnum TargetState => ShipmentStatusEnum.Cancelled;

        public async Task HandleState(ShippmentDto shipment)
        {
            if (shipment == null || shipment.Id == Guid.Empty)
                throw new ArgumentException("Shipment ID is required for cancellation");

            // 1. Validate shipment exists
            var currentShipment = await _shipmentQuery.GetByIdAsync(shipment.Id);
            if (currentShipment == null)
                throw new InvalidOperationException("Shipment not found.");

            // 2. Business Rule: Cannot cancel once Shipped, Delivered, or already Closed
            if (currentShipment.CurrentState == (int)ShipmentStatusEnum.Shipped ||
                currentShipment.CurrentState == (int)ShipmentStatusEnum.Delivered ||
                currentShipment.CurrentState == (int)ShipmentStatusEnum.Returned ||
                currentShipment.CurrentState == (int)ShipmentStatusEnum.Deleted)
            {
                throw new InvalidOperationException("Shipments that are already shipped, delivered, or closed cannot be cancelled.");
            }

            // 3. If it was paid, automatically refund payment through the gateway
            var reason = shipment.CancellationReason ?? "Shipment cancelled by user/admin";
            if (currentShipment.IsPaid)
            {
                await _paymentTransactionService.RefundPayment(shipment.Id, reason);
            }

            // 4. Update shipment audit fields (CancelledBy, CancelledAt, CancellationReason)
            var userId = _userService.GetLoggedInUser();
            await _shipment.Cancelled(shipment.Id, userId, reason);

            // 5. Update status & history
            await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            await _status.Add(shipment.Id, TargetState);
        }
    }
}





