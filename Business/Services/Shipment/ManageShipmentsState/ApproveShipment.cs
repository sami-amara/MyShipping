using Business.Contracts;
using Business.Contracts.Shipment;
using Business.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;


namespace Business.Services.Shipment.ManageShipmentsState
{
    public class ApproveShipment : IShipmentStateHandler
    {
        IShipmentCommand _shipment;
        IShipmentsStatus _status;
        IShipmentQuery _shipmentQuery;
        IUserService _userService;

        public ApproveShipment(IShipmentCommand shipment, IShipmentsStatus status, 
            IShipmentQuery shipmentQuery, IUserService userService)
        {
            _shipment = shipment;
            _status = status;
            _shipmentQuery = shipmentQuery;
            _userService = userService;
        }


        public ShipmentStatusEnum TargetState => ShipmentStatusEnum.Approved;



        //public ShipmentStatusEnum TargetState { get => ShipmentStatusEnum.Approved; }

        public async Task HandleState(ShippmentDto shipment)
        {

            var currentShipment = await _shipmentQuery.GetByIdAsync(shipment.Id);
            if (currentShipment == null)
                throw new InvalidOperationException("Shipment not found.");
            if (!currentShipment.IsPaid)
            {
                throw new InvalidOperationException("Shipment must be paid before it can be approved.");
            }


            var userId = _userService.GetLoggedInUser();
            //await _shipment.Edit(shipment);
            await _shipment.Approved(shipment.Id, userId);
            await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            await _status.Add(shipment.Id, TargetState);
        }
    }
}










