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
        private readonly IShipmentsStatus _status;
        private readonly IGenericRepository<TbShippment> _repo;
        private readonly IUserService _userService;

        public DeletedShipment(
            IShipmentCommand shipment, 
            IShipmentsStatus status,
            IGenericRepository<TbShippment> repo,
            IUserService userService)
        {
            _shipment = shipment;
            _status = status;
            _repo = repo;
            _userService = userService;
        }

        public ShipmentStatusEnum TargetState { get => ShipmentStatusEnum.Deleted; }

        public async Task HandleState(ShippmentDto shipment)
        {
            // ✅ Soft delete: Set IsDeleted, DeletedDate, DeletedBy
            var userId = _userService.GetLoggedInUser();
            await _repo.Delete(shipment.Id, userId);
            // ✅ Also update CurrentState for workflow tracking and backward compatibility
            await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            // ✅ Track status history
            await _status.Add(shipment.Id, TargetState);
        }
    }
}
