using Business.DTOS;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Contracts.Shipment
{
    public interface IShipmentQuery
    {
        public Task<List<ShippmentDto>> GetShipments();
        public Task<ShippmentDto> GetShipment(Guid Id);
        public Task<PagedResult<ShippmentDto>> GetAllShipments(int pageNumber, int pageSize);
        public Task<PagedResult<ShippmentDto>> GetAllShipments(int pageNumber, int pageSize, string searchTerm = null);

        public Task<PagedResult<ShippmentDto>> GetShipments(int pageNumber, int pageSize, bool isUserData, List<ShipmentStatusEnum>? statuses, string searchTerm = null, bool? isPaid = null);
        Task<ShippmentDto?> GetByIdAsync(Guid id);

    }
}
