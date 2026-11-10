using Business.DTOS;
using Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Contracts.Shipment
{
    public interface IShipmentCommand
    {

        Task Create(ShippmentDto shippment);
        Task<bool> ChangeStatusAsync(Guid id, int status = 1);
        Task<bool> DeleteAsync(Guid id);
        //Task Approved(ShippmentDto shippment);
        //Task Approved(ShippmentDto shippment, Guid userId);
         Task Approved(Guid shipmentId, Guid userId);
        public Task ReadyForShip(Guid id, Guid carrierId, Guid userId);
       
        public Task Shipped(Guid id, Guid userId);

        Task Delivered(Guid shipmentId, Guid userId);
        //Task Update(TbShippment updatedShipment);
        public Task Edit(ShippmentDto shippment);
        public Task EditFields(Guid id, Action<TbShippment> updateAction);
        Task Delete(Guid id, Guid userId);

        Task Refunded(Guid id, Guid userId, string? reason = null);

        Task Cancelled(Guid id, Guid userId, string? reason = null);
        Task Returned(Guid id, Guid userId, string? reason = null);
    }
}
