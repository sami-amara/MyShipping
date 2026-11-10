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
    public class ReadyForShippingShipment : IShipmentStateHandler
    {
        IShipmentCommand _shipment;
        IShipmentsStatus _status;
        IUserService _userService;
        IShipmentQuery _shipmentQuery;

        public ReadyForShippingShipment(IShipmentCommand shipment, IShipmentsStatus status,
            IUserService IUserService, IShipmentQuery shipmentQuery)
        {
            _shipment = shipment;
            _status = status;
            _userService = IUserService;
            _shipmentQuery = shipmentQuery;
        }


        public ShipmentStatusEnum TargetState => ShipmentStatusEnum.ReadyForShipping;

        public async Task HandleState(ShippmentDto shipment)
        {
            if (shipment == null || shipment.Id == Guid.Empty)
                throw new ArgumentException("Shipment ID is required", nameof(shipment));

            // 1. Load current shipment from database
            var currentShipment = await _shipmentQuery.GetByIdAsync(shipment.Id);
            if (currentShipment == null)
                throw new InvalidOperationException("Shipment not found.");

            // 2. PAYMENT VALIDATION: Shipment MUST be paid before ready for shipping
            if (!currentShipment.IsPaid)
            {
                throw new InvalidOperationException("Shipment must be paid before it can be marked as ready for shipping.");
            }

            // 3. STATUS VALIDATION: Only approved shipments can be prepared for shipping
            if (currentShipment.CurrentState != (int)ShipmentStatusEnum.Approved)
            {
                throw new InvalidOperationException("Only approved shipments can be marked as ready for shipping.");
            }

            // 4. CARRIER VALIDATION: Get carrier from DTO or existing database record
            var carrierId = (shipment.CarrierId.HasValue && shipment.CarrierId.Value != Guid.Empty)
                ? shipment.CarrierId.Value
                : currentShipment.CarrierId;

            if (!carrierId.HasValue || carrierId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("A carrier must be selected before marking the shipment ready for shipping.");
            }

            // 5. Get logged-in user and execute command with CarrierId
            var userId = _userService.GetLoggedInUser();
            await _shipment.ReadyForShip(shipment.Id, carrierId.Value, userId);

            // 6. Update status & history
            await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            await _status.Add(shipment.Id, TargetState);
        }


        //public ShipmentStatusEnum TargetState { get => ShipmentStatusEnum.ReadyForShipping; }

        //public async Task HandleState(ShippmentDto shipment)
        //{
        //    // ? PAYMENT VALIDATION: Shipment MUST be paid before ready for shipping
        //    // Load current shipment from database to get accurate IsPaid status
        //    var currentShipment = await _shipmentQuery.GetByIdAsync(shipment.Id);
        //    if (currentShipment == null)
        //    throw new InvalidOperationException("Shipment not found.");
        //    if (!currentShipment.IsPaid)
        //    {
        //        throw new InvalidOperationException("Shipment must be paid before it can be marked as ready for shipping.");
        //    }

        //    var userId = _userService.GetLoggedInUser();


        //    await _shipment.ReadyForShip(shipment.Id, userId);
        //    await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
        //    await _status.Add(shipment.Id, TargetState);
        //}
    }
}
