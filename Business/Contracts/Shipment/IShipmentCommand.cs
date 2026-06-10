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
        Task Approved(ShippmentDto shippment);
        //public Task Approve(ShippmentDto shippment);
        public Task ReadyForShip(Guid id, Guid carrierId);
        public Task Shipped(Guid id, DateTime deliveryDate);
        //Task Update(TbShippment updatedShipment);
        public Task Edit(ShippmentDto shippment);
        public Task EditFields(Guid id, Action<TbShippment> updateAction);

    }
}
