using Business.Contracts;
using Business.Contracts.Shipment;
using Business.DTOS;
using DataAccessLayer.Contracts;
using Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.Shipment.ManageShipmentsState
{
    public class DeletedShipment : IShipmentStateHandler
    {
        private readonly IShipmentCommand _shipment;

        IShipmentQuery _shipmentQuery;
        private readonly IShipmentsStatus _status;
        private readonly IGenericRepository<TbShippment> _repo;
        private readonly IUserService _userService;

        public DeletedShipment(
            IShipmentCommand shipment, 
            IShipmentsStatus status,
            IGenericRepository<TbShippment> repo,
            IUserService userService,
            IShipmentQuery shipmentQuery)
        {
            _shipment = shipment;
            _status = status;
            _repo = repo;
            _userService = userService;
            _shipmentQuery = shipmentQuery;
        }

        public ShipmentStatusEnum TargetState { get => ShipmentStatusEnum.Deleted; }

        public async Task HandleState(ShippmentDto shipment)
        {
            // 1. Validate shipment exists
            var currentShipment = await _shipmentQuery.GetByIdAsync(shipment.Id);
            if (currentShipment == null)
                throw new InvalidOperationException("Shipment not found.");

            // 2. Business Rule: Only allow deleting shipments in 'Created' status
            if (currentShipment.CurrentState != (int)ShipmentStatusEnum.Created)
            {
                throw new InvalidOperationException("Only newly created shipments can be deleted.");
            }

            // 3. Financial Rule: Prevent deleting paid shipments! Must use Refund/Cancel instead
            if (currentShipment.IsPaid)
            {
                throw new InvalidOperationException("Paid shipments cannot be deleted. Please cancel or refund the shipment instead.");
            }

            // 4. Get the logged-in user and execute soft delete
            var userId = _userService.GetLoggedInUser();
            await _shipment.Delete(shipment.Id, userId);

            // 5. Update status & history
            await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            await _status.Add(shipment.Id, TargetState);
        }

     
    }
}
