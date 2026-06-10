using AutoMapper;
using Business.Contracts;
using Business.Contracts.Shipment;
using Business.DTOS;
using Business.Helpers;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Contracts; // Add this line
using DataAccessLayer.Model;
using DataAccessLayer.Repositories;
using Domains;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace Business.Services.Shipment
{
    public class ShipmentsStatusService : BaseService<TbShippmentStatus, ShippmentStatusDto>, IShipmentsStatus
    {
        private readonly IUserService _userService;
        private readonly IUserReceiver _userReceiver;
        private readonly IUserSender _userSender;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<TbShippmentStatus> _repo;
        private readonly IMapper _mapper;
        private readonly ILogger<ShipmentsStatusService> _logger;
        public ShipmentsStatusService(IGenericRepository<TbShippmentStatus> repository,
            IMapper mapper, IUserService userService,
            IUserReceiver userReceiver, IUserSender userSender, IUnitOfWork unitOfWork, ILogger<ShipmentsStatusService> logger) : base(unitOfWork, mapper, userService)
        {
            _userService = userService;
            _userReceiver = userReceiver;
            _userSender = userSender;
            _unitOfWork = unitOfWork;
            _repo = repository;
            _mapper = mapper;
            _logger = logger;
        }  

        /// <summary>
        /// Convenience overload: create a shipment status record for the given shipmentId and status enum.
        /// Returns a tuple of (success, createdId) to match AddAsync semantics used elsewhere.
        /// </summary>
        public async Task<(bool Success, Guid Id)> Add(Guid shipmentId, ShipmentStatusEnum status)
        {
            var oStatus = new ShippmentStatusDto
            {
                ShippmentId = shipmentId,
                CurrentState = (int)status
            };

            var dbObject = _mapper.Map<ShippmentStatusDto, TbShippmentStatus>(oStatus);
            //dbObject.CreatedBy = _userService.GetLoggedInUser();
            //dbObject.CreatedDate = DateTime.UtcNow;
            //dbObject.CurrentState = 1;


            var now = DateTime.UtcNow;
            var userId = _userService.GetLoggedInUser();
            dbObject.CreatedBy = userId;
            dbObject.CreatedDate = now;

            // record the actual passed state
            dbObject.CurrentState = oStatus.CurrentState;

            // set Updated* so history rows have the actor/timestamp
            dbObject.UpdatedBy = userId;
            dbObject.UpdatedDate = now;
            var (ok, id) = await _repo.Add(dbObject);
            return (ok, id);
        }


  



    }  
}

























