



using Business.Contracts;
using Business.Contracts.Shipment;
using Business.DTOS;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Contracts;
using DataAccessLayer.Model;
using Domains;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Business.Contracts.Shipment
{
    public interface IShipments : IBaseService<TbShippment, ShippmentDto>
    {
        
        //Task Create(ShippmentDto shippment);
        
        //public Task<PagedResult<ShippmentDto>> GetAllShipments(int pageNumber, int pageSize, string searchTerm = null);

        //public Task<PagedResult<ShippmentDto>> GetShipments(int pageNumber, int pageSize, bool isUserData, ShipmentStatusEnum? status, string searchTerm = null);
        //Task<ShippmentDto?> GetByIdAsync(Guid id);

        //Task<bool> ChangeStatusAsync(Guid id, int status = 1);
        //Task<bool> DeleteAsync(Guid id);
        //Task Approved(ShippmentDto shippment);
        ////public Task Approve(ShippmentDto shippment);
        //public Task ReadyForShip(Guid id, Guid carrierId);
        //public Task Shipped(Guid id, DateTime deliveryDate);
        ////Task Update(TbShippment updatedShipment);

        
        //public Task Edit(ShippmentDto shippment);
        //public Task EditFields(Guid id, Action<TbShippment> updateAction);
        //public Task<List<ShippmentDto>> GetShipments();
        //public Task<ShippmentDto> GetShipment(Guid Id);
        //public Task<PagedResult<ShippmentDto>> GetAllShipments(int pageNumber, int pageSize);
       

    }
}















//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Business.DTOS;
//using DataAccessLayer.Contracts;
//using Domains;
//using DataAccessLayer.Model;

//namespace Business.Contracts.Shipment
//{
//    public interface IShipments : IBaseService<TbShippment, ShippmentDto>
//    {

//        public Task Create(ShippmentDto shippmentDto);

//        public Task<List<ShippmentDto>> GetShippments();

//        // New: server-side paged list
//        public Task<PagedResult<ShippmentDto>> GetShippmentsPaged(int page = 1, int pageSize = 10, string sortBy = null, string sortDir = "desc");
//    }
//}
