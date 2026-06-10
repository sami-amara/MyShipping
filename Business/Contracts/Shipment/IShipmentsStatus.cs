



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
    public interface IShipmentsStatus : IBaseService<TbShippmentStatus, ShippmentStatusDto>
    {
        Task<(bool Success, Guid Id)> Add(Guid shipmentId, ShipmentStatusEnum status);
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
