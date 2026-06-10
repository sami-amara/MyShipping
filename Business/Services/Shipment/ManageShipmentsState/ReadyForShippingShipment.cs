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

        public ShipmentStatusEnum TargetState { get => ShipmentStatusEnum.ReadyForShipping; }

        public async Task HandleState(ShippmentDto shipment)
        {
            // ? PAYMENT VALIDATION: Shipment MUST be paid before ready for shipping
            // Load current shipment from database to get accurate IsPaid status
            var currentShipment = await _shipmentQuery.GetByIdAsync(shipment.Id);
            if (currentShipment == null)
            throw new InvalidOperationException("Shipment not found.");
            if (!currentShipment.IsPaid)
            {
                throw new InvalidOperationException("Shipment must be paid before it can be marked as ready for shipping.");
            }

            var userId = _userService.GetLoggedInUser();
            await _shipment.EditFields(shipment.Id, a =>
            {
                a.CarrierId = shipment.CarrierId;
                a.CurrentState = (int)TargetState;
                a.UpdatedBy = userId;
                a.UpdatedDate = DateTime.UtcNow;
            });
            await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            await _status.Add(shipment.Id, TargetState);
        }
    }
}
