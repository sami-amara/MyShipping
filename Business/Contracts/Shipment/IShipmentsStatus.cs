



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


