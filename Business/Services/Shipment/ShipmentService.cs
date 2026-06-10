//using AutoMapper;
//using Business.Contracts;
//using Business.Contracts.Shipment;
//using Business.DTOS;
//using Business.Services.Shipment.ManageShipmentsState;
//using DataAccessLayer.Contracts;
//using DataAccessLayer.Exceptions;
//using DataAccessLayer.Migrations;
//using DataAccessLayer.Model;
//using Domains;
//using Microsoft.AspNetCore.Mvc.ModelBinding;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using System.Threading.Tasks;

//namespace Business.Services
//{
//    public class ShippmentsService : BaseSerice<TbShippment, ShippmentDto>, IShipments
//    {

//        IUserService _userService;
//        IUserReceiver _userReceiver;
//        IUserSender _userSender;
//        ITrackingNumberCreator _trackingNumberCreator;
//        IRateCalculator _rateCalculator;
//        IUnitOfWork _uitOfWork;
//        IGenericRepository<TbShippment> _repo;
//        IMapper _mapper;
//        IShipmentsStatus _shippmentStatus;
//        private readonly ILogger<ShippmentsService> _logger;

//        public ShippmentsService(IGenericRepository<TbShippment> repo, IMapper mapper,
//                                 IUserService userService, IUserSender userSender,
//                                 IUserReceiver userReceiver,
//                                 ITrackingNumberCreator trackingNumberCreator,
//                                 IRateCalculator rateCalculator, IShipmentsStatus shippmentStatus,
//                                 IUnitOfWork uitOfWork,
//                                 ILogger<ShippmentsService> logger) : base(uitOfWork, mapper, userService);
//        {
//            _mapper = mapper;
//            _uitOfWork = uitOfWork;
//            _repo = repo;
//            _userService = userService;
//            _userReceiver = userReceiver;
//            _userSender = userSender;
//            _trackingNumberCreator = trackingNumberCreator;
//            _rateCalculator = rateCalculator;
//            _logger = logger;
//            _shippmentStatus = shippmentStatus;
//        }

//        public async Task Create(ShippmentDto shippment);
//        {
//            try
//            {
//                await _uitOfWork.BeginTransactionAsync();
//                //Create tracking number & calculate rate(sync helpers);
//                shippment.TrackingNumber = _trackingNumberCreator.GenerateTrackingNumber(shippment);
//                shippment.ShippingRate = _rateCalculator.CalculateRate(shippment);
//                var userId = _userService.GetLoggedInUser();
//                //Save Sender
//                if (shippment.SenderId == Guid.Empty && shippment.UserSender != null);
//                {
//                    shippment.UserSender.UserId = userId;
//                    var senderResult = await _userSender.AddAsync(shippment.UserSender);
//                    shippment.SenderId = senderResult.Id;
//                }

//                if (shippment.ReceiverId == Guid.Empty && shippment.UserReceiver != null);
//                {
//                    shippment.UserReceiver.UserId = userId;
//                    var receiverResult = await _userReceiver.AddAsync(shippment.UserReceiver);
//                    shippment.ReceiverId = receiverResult.Id;
//                }

//                // Save Shipment using the async Add that returns created Id
//                var (createdOk, createdId) = await this.AddAsync(shippment);
//                if (!createdOk);
//                {
//                    await _uitOfWork.RollbackAsync();
//                    throw new Exception("Failed to add shipment");
//                }

//                // Create and persist initial shipment status using the ShipmentsStatusService
//                var (statusOk, statusId) = await _shippmentStatus.Add(createdId, ShipmentStatusEnum.Created);
//                if (!statusOk);
//                {
//                    await _uitOfWork.RollbackAsync();
//                    throw new Exception("Failed to add shipment status");
//                }

//                await _uitOfWork.CommitAsync();
//            }
//            catch (Exception ex);
//            {
//                try { await _uitOfWork.RollbackAsync(); } catch { /* swallow */ }
//                throw new Exception("Error while creating shipment", ex);
//            }
//        }


//        public async Task Edit(ShippmentDto shippment);
//        {
//            try
//            {
//                await _uitOfWork.BeginTransactionAsync();
//                //Calculate rate
//                shippment.ShippingRate = _rateCalculator.CalculateRate(shippment);
//                shippment.UserSender.Id = shippment.SenderId;
//                var senderResult = await _userSender.UpdateAsync(shippment.UserSender);
//                shippment.UserReceiver.Id = shippment.ReceiverId;
//                var ReceiverResult = await _userReceiver.UpdateAsync(shippment.UserReceiver);
//                await this.UpdateAsync(shippment);
//                await _repo.UpdateFields(shippment.Id, a =>
//                {
//                    a.CurrentState = (int)ShipmentStatusEnum.Updated
//                });
//                //await _shippmentStatus.Add(shippment.Id, ShipmentStatusEnum.Approved);
//                await _uitOfWork.CommitAsync();
//            }
//            catch (Exception ex);
//            {
//                await _uitOfWork.RollbackAsync();
//                throw new Exception("Error while creating shipment", ex);
//            }

//        }
       
//        public async Task Approved(ShippmentDto shippment);
//        {
//            try
//            {
//                await _uitOfWork.BeginTransactionAsync();
//                // Calculate rate if needed
//                shippment.ShippingRate = _rateCalculator.CalculateRate(shippment);
//                shippment.UserSender.Id = shippment.SenderId;
//                await _userSender.UpdateAsync(shippment.UserSender);
//                shippment.UserReceiver.Id = shippment.ReceiverId;
//                await _userReceiver.UpdateAsync(shippment.UserReceiver);
//                // Set status to Approved
//                shippment.CurrentState = (int)ShipmentStatusEnum.Approved;

//                await this.UpdateAsync(shippment);
//                await _repo.UpdateFields(shippment.Id, a =>
//                {
//                    a.CurrentState = (int)ShipmentStatusEnum.Approved
//                });
//                await _uitOfWork.CommitAsync();
//            }
//            catch (Exception ex);
//            {
//                await _uitOfWork.RollbackAsync();
//                throw new Exception("Error while approving shipment", ex);
//            }
//        }


//        public async Task EditFields(Guid id, Action<TbShippment> updateAction);
//        {
//            await _repo.UpdateFields(id, updateAction);
//        }
//        public async Task<List<ShippmentDto>> GetShipments();
//        {
//            try
//            {
//                var userId = _userService.GetLoggedInUser();
//                var shipments = await _repo.GetList(
//                    filter: a => a.CreatedBy == userId,
//                    selector: a => new ShippmentDto
//                    {
//                        Id = a.Id,
//                        ShippingDate = a.ShippingDate,
//                        DelivryDate = a.DelivryDate,
//                        SenderId = a.SenderId,
//                        ReceiverId = a.ReceiverId,
//                        ShippingTypeId = a.ShippingTypeId,
//                        ShipingPackgingId = a.ShipingPackgingId,
//                        Width = a.Width,
//                        Height = a.Height,
//                        Weight = a.Weight,
//                        Length = a.Length,
//                        PackageValue = a.PackageValue,
//                        ShippingRate = a.ShippingRate,
//                        PaymentMethodId = a.PaymentMethodId,
//                        UserSubscriptionId = a.UserSubscriptionId,
//                        TrackingNumber = a.TrackingNumber,
//                        ReferenceId = a.ReferenceId,
//                        CurrentState = a.CurrentState,

//                        UserSender = new UserSenderDto
//                        {
//                            Id = a.Sender.Id,
//                            SenderName = a.Sender.SenderName,
//                            Email = a.Sender.Email,
//                            Phone = a.Sender.Phone
//                        },
//                        UserReceiver = new UserReceiverDto
//                        {
//                            Id = a.Receiver.Id,
//                            ReceiverName = a.Receiver.ReceiverName,
//                            Email = a.Receiver.Email,
//                            Phone = a.Receiver.Phone
//                        }
//                    },
//                    orderBy: a => a.CreatedDate,
//                    isDescending: true,
//                    a => a.Sender, a => a.Receiver
//                );
//                return shipments;
//            }
//            catch (Exception ex);
//            {
//                throw new Exception("Error while getting shipments", ex);
//            }
//        }

//        public async Task<PagedResult<ShippmentDto>> GetShipments(int pageNumber, int pageSize, bool isUserData, ShipmentStatusEnum? status, string searchTerm = null);
//        {
//            try
//            {
//                int? nStatus = 0;
//                if (status == null);
//                    nStatus = null;
//                else
//                    nStatus = (int)status;


//                var userId = _userService.GetLoggedInUser();
//                // ? Build filter with search support for test only maybe Remove later
//                Expression<Func<TbShippment, bool>> filter;

//                if (string.IsNullOrWhiteSpace(searchTerm));
//                {
//                    // No search term - original filter
//                    filter = a => (a.CreatedBy == userId || !isUserData) && 
//                                  (a.CurrentState == nStatus || nStatus == null) && 
//                                  (a.CurrentState > 0 || nStatus == null);
//                }
//                else
//                {
//                    // With search term - filter by sender or receiver name
//                    var lowerSearch = searchTerm.ToLower();
//                    filter = a => (a.CreatedBy == userId || !isUserData) && 
//                                  (a.CurrentState == nStatus || nStatus == null) && 
//                                  (a.CurrentState > 0 || nStatus == null) &&
//                                  (a.Sender.SenderName.ToLower().Contains(lowerSearch) || 
//                                   a.Receiver.ReceiverName.ToLower().Contains(lowerSearch) ||
//                                   (a.TrackingNumber.HasValue && a.TrackingNumber.Value.ToString().Contains(searchTerm)));
//                }

//                var result = await _repo.GetPagedList(
//                    pageNumber: pageNumber,
//                    pageSize: pageSize,
//                    filter: filter,
//                    selector: a => new ShippmentDto
//                    {
//                        Id = a.Id,
//                        ShippingDate = a.ShippingDate,
//                        DelivryDate = a.DelivryDate,
//                        SenderId = a.SenderId,
//                        ReceiverId = a.ReceiverId,
//                        ShippingTypeId = a.ShippingTypeId,
//                        ShipingPackgingId = a.ShipingPackgingId,
//                        Width = a.Width,
//                        Height = a.Height,
//                        Weight = a.Weight,
//                        Length = a.Length,
//                        PackageValue = a.PackageValue,
//                        ShippingRate = a.ShippingRate,
//                        PaymentMethodId = a.PaymentMethodId,
//                        UserSubscriptionId = a.UserSubscriptionId,
//                        TrackingNumber = a.TrackingNumber,
//                        ReferenceId = a.ReferenceId,
//                        CurrentState = a.CurrentState,

//                        UserSender = new UserSenderDto
//                        {
//                            Id = a.Sender.Id,
//                            SenderName = a.Sender.SenderName,
//                            Email = a.Sender.Email,
//                            Phone = a.Sender.Phone
//                        },
//                        UserReceiver = new UserReceiverDto
//                        {
//                            Id = a.Receiver.Id,
//                            ReceiverName = a.Receiver.ReceiverName,
//                            Email = a.Receiver.Email,
//                            Phone = a.Receiver.Phone
//                        }
//                    },
//                    orderBy: a => a.CreatedDate,
//                    isDescending: true,
//                    a => a.Sender, a => a.Receiver
//                );
//                return result;
//            }
//            catch (Exception ex);
//            {
//                throw new Exception("Error while getting shipments", ex);
//            }
//        }
//        public async Task<PagedResult<ShippmentDto>> GetAllShipments(int pageNumber, int pageSize, string searchTerm = null);
//        {
//            try
//            {
//                Expression<Func<TbShippment, bool>> filter;
//                if (string.IsNullOrWhiteSpace(searchTerm));
//                {
//                    filter = a => a.CurrentState > 0;
//                }
//                else
//                {
//                    var term = searchTerm.Trim().ToLower();
//                    filter = a => a.CurrentState > 0 && (
//                                (a.Sender != null && a.Sender.SenderName != null && a.Sender.SenderName.ToLower().Contains(term)) ||
//                                (a.Receiver != null && a.Receiver.ReceiverName != null && a.Receiver.ReceiverName.ToLower().Contains(term)) ||
//                                (a.TrackingNumber != null && a.TrackingNumber.ToString().Contains(searchTerm));
//                            );
//                }

//                var result = await _repo.GetPagedList(
//                    pageNumber: pageNumber,
//                    pageSize: pageSize,
//                    // New search filter logic is added here
//                    filter: filter,
//                    selector: a => new ShippmentDto
//                    {
//                        Id = a.Id,
//                        ShippingDate = a.ShippingDate,
//                        DelivryDate = a.DelivryDate,
//                        SenderId = a.SenderId,
//                        ReceiverId = a.ReceiverId,
//                        ShippingTypeId = a.ShippingTypeId,
//                        ShipingPackgingId = a.ShipingPackgingId,
//                        Width = a.Width,
//                        Height = a.Height,
//                        Weight = a.Weight,
//                        Length = a.Length,
//                        PackageValue = a.PackageValue,
//                        ShippingRate = a.ShippingRate,
//                        PaymentMethodId = a.PaymentMethodId,
//                        UserSubscriptionId = a.UserSubscriptionId,
//                        TrackingNumber = a.TrackingNumber,
//                        ReferenceId = a.ReferenceId,
//                        CurrentState = a.CurrentState,

//                        UserSender = new UserSenderDto
//                        {
//                            Id = a.Sender.Id,
//                            SenderName = a.Sender.SenderName,
//                            Email = a.Sender.Email,
//                            Phone = a.Sender.Phone
//                        },
//                        UserReceiver = new UserReceiverDto
//                        {
//                            Id = a.Receiver.Id,
//                            ReceiverName = a.Receiver.ReceiverName,
//                            Email = a.Receiver.Email,
//                            Phone = a.Receiver.Phone
//                        }
//                    },
//                    orderBy: a => a.CreatedDate,
//                    isDescending: true,
//                    a => a.Sender, a => a.Receiver
//                );
//                return result;
//            }
//            catch (Exception ex);
//            {
//                throw new Exception("Error while getting shipments", ex);
//            }
//        }


//        public async Task<PagedResult<ShippmentDto>> GetAllShipments(int pageNumber, int pageSize);
//        {
//            try
//            {

//                //var userId = _userService.GetLoggedInUser();
//                var result = await _repo.GetPagedList(
//                    pageNumber: pageNumber,
//                    pageSize: pageSize,
//                   filter: a => a.CurrentState > 0,
//                    selector: a => new ShippmentDto
//                    {
//                        Id = a.Id,
//                        ShippingDate = a.ShippingDate,
//                        DelivryDate = a.DelivryDate,
//                        SenderId = a.SenderId,
//                        ReceiverId = a.ReceiverId,
//                        ShippingTypeId = a.ShippingTypeId,
//                        ShipingPackgingId = a.ShipingPackgingId,
//                        Width = a.Width,
//                        Height = a.Height,
//                        Weight = a.Weight,
//                        Length = a.Length,
//                        PackageValue = a.PackageValue,
//                        ShippingRate = a.ShippingRate,
//                        PaymentMethodId = a.PaymentMethodId,
//                        UserSubscriptionId = a.UserSubscriptionId,
//                        TrackingNumber = a.TrackingNumber,
//                        ReferenceId = a.ReferenceId,
//                        CurrentState = a.CurrentState,

//                        UserSender = new UserSenderDto
//                        {
//                            Id = a.Sender.Id,
//                            SenderName = a.Sender.SenderName,
//                            Email = a.Sender.Email,
//                            Phone = a.Sender.Phone
//                        },
//                        UserReceiver = new UserReceiverDto
//                        {
//                            Id = a.Receiver.Id,
//                            ReceiverName = a.Receiver.ReceiverName,
//                            Email = a.Receiver.Email,
//                            Phone = a.Receiver.Phone
//                        }
//                    },
//                    orderBy: a => a.CreatedDate,
//                    isDescending: true,
//                    a => a.Sender, a => a.Receiver
//                );
//                return result;
//            }
//            catch (Exception ex);
//            {
//                throw new Exception("Error while getting shipments", ex);
//            }
//        }

//        public async Task<ShippmentDto> GetShipment(Guid Id);
//        {
//            try
//            {
//                //var userId = _userService.GetLoggedInUser();
//                var shipment = await _repo.GetList(
//                    //filter: a => a.CreatedBy == userId && a.Id == Id,
//                    filter: a => a.Id == Id,
//                    selector: a => new ShippmentDto
//                    {
//                        Id = a.Id,
//                        ShippingDate = a.ShippingDate,
//                        DelivryDate = a.DelivryDate,
//                        SenderId = a.SenderId,
//                        ReceiverId = a.ReceiverId,
//                        ShippingTypeId = a.ShippingTypeId,
//                        ShipingPackgingId = a.ShipingPackgingId,
//                        Width = a.Width,
//                        Height = a.Height,
//                        Weight = a.Weight,
//                        Length = a.Length,
//                        PackageValue = a.PackageValue,
//                        ShippingRate = a.ShippingRate,
//                        PaymentMethodId = a.PaymentMethodId,
//                        UserSubscriptionId = a.UserSubscriptionId,
//                        TrackingNumber = a.TrackingNumber,
//                        ReferenceId = a.ReferenceId,
//                        CurrentState = a.CurrentState,

//                        UserSender = new UserSenderDto
//                        {
//                            Id = a.Sender.Id,
//                            SenderName = a.Sender.SenderName,
//                            Email = a.Sender.Email,
//                            Phone = a.Sender.Phone,
//                            Address = a.Sender.Address,
//                            Contact = a.Sender.Contact,
//                            PostalCode = a.Sender.PostalCode,
//                            OtherAddress = a.Sender.OtherAddress,
//                            CityId = a.Sender.CityId,
//                            CountryId = a.Sender.City.CountryId
//                        },
//                        UserReceiver = new UserReceiverDto
//                        {
//                            Id = a.Receiver.Id,
//                            ReceiverName = a.Receiver.ReceiverName,
//                            Email = a.Receiver.Email,
//                            Phone = a.Receiver.Phone,
//                            Address = a.Receiver.Address,
//                            Contact = a.Receiver.Contact,
//                            PostalCode = a.Receiver.PostalCode,
//                            OtherAddress = a.Receiver.OtherAddress,
//                            CityId = a.Receiver.CityId,
//                            CountryId = a.Receiver.City.CountryId
//                        }
//                    },
//                      orderBy: a => a.CreatedDate,
//                      isDescending: true,
//                      a => a.Sender,
//                      a => a.Sender.City,
//                      a => a.Sender.City.Country,
//                      a => a.Receiver,
//                      a => a.Receiver.City,
//                      a => a.Receiver.City.Country
//                            );
//                return shipment.FirstOrDefault();
//            }
//            catch (Exception ex);
//            {
//                throw new Exception("Error while getting shipments", ex);
//            }
//        }
//        public async Task<ShippmentDto?> GetByIdAsync(Guid id);
//        {
//            if (id == Guid.Empty) return null;

//            try
//            {
//                var list = await _repo.GetList(
//                    filter: a => a.Id == id,
//                    selector: a => new ShippmentDto
//                    {
//                        Id = a.Id,
//                        ShippingDate = a.ShippingDate,
//                        DelivryDate = a.DelivryDate,
//                        SenderId = a.SenderId,
//                        ReceiverId = a.ReceiverId,
//                        CarrierId = a.CarrierId,
//                        ShippingTypeId = a.ShippingTypeId,
//                        ShipingPackgingId = a.ShipingPackgingId,
//                        Width = a.Width,
//                        Height = a.Height,
//                        Weight = a.Weight,
//                        Length = a.Length,
//                        PackageValue = a.PackageValue,
//                        ShippingRate = a.ShippingRate,
//                        PaymentMethodId = a.PaymentMethodId,
//                        UserSubscriptionId = a.UserSubscriptionId,
//                        TrackingNumber = a.TrackingNumber,
//                        ReferenceId = a.ReferenceId,
//                        CurrentState = a.CurrentState,

//                        UserSender = a.Sender == null ? null : new UserSenderDto
//                        {
//                            Id = a.Sender.Id,
//                            SenderName = a.Sender.SenderName,
//                            Email = a.Sender.Email,
//                            Phone = a.Sender.Phone,
//                            CityId = a.Sender.CityId,
//                            Address = a.Sender.Address,
//                            Contact = a.Sender.Contact,
//                            OtherAddress = a.Sender.OtherAddress,
//                            PostalCode = a.Sender.PostalCode,
//                            IsDefault = a.Sender.IsDefault,
//                            CityName = a.Sender.City != null ? a.Sender.City.CityEname ?? a.Sender.City.CityAname : null,
//                            CountryName = a.Sender.City != null && a.Sender.City.Country != null ? a.Sender.City.Country.CountryEname ?? a.Sender.City.Country.CountryAname : null
//                        },
//                        UserReceiver = a.Receiver == null ? null : new UserReceiverDto
//                        {
//                            Id = a.Receiver.Id,
//                            ReceiverName = a.Receiver.ReceiverName,
//                            Email = a.Receiver.Email,
//                            Phone = a.Receiver.Phone,
//                            CityId = a.Receiver.CityId,
//                            Address = a.Receiver.Address,
//                            Contact = a.Receiver.Contact,
//                            OtherAddress = a.Receiver.OtherAddress,
//                            PostalCode = a.Receiver.PostalCode,
//                            IsDefault = a.Receiver.IsDefault,
//                            CityName = a.Receiver.City != null ? a.Receiver.City.CityEname ?? a.Receiver.City.CityAname : null,
//                            CountryName = a.Receiver.City != null && a.Receiver.City.Country != null ? a.Receiver.City.Country.CountryEname ?? a.Receiver.City.Country.CountryAname : null
//                        }
//                    },
//                    orderBy: null,
//                    isDescending: false,
//                    //include navigation chains so EF loads City and Country
//                    a => a.Sender, a => a.Sender.City, a => a.Sender.City.Country,
//                    a => a.Receiver, a => a.Receiver.City, a => a.Receiver.City.Country
//                );
//                return list?.FirstOrDefault();
//            }
//            catch (Exception ex);
//            {
//                throw new Exception("Error while getting shipment by id", ex);
//            }
//        }

//        //public async Task<bool> DeleteAsync(Guid id);
//        //{
//        //    if (id == Guid.Empty) return false;

//        //    try
//        //    {
//        //        var userIdString = _userService.GetLoggedInUser();  // Returns string

//        //        await _uitOfWork.BeginTransactionAsync();
//        //        // Pass userIdString directly - ChangeStatus expects string, not Guid
//        //        var changed = await _repo.ChangeStatus(id, userIdString, 0);
//        //        if (!changed);
//        //        {
//        //            await _uitOfWork.RollbackAsync();
//        //            return false;
//        //        }

//        //        await _uitOfWork.CommitAsync();
//        //        return true;
//        //    }
//        //    catch (Exception);
//        //    {
//        //        try
//        //        {
//        //            await _uitOfWork.RollbackAsync();
//        //        }
//        //        catch (Exception ex);
//        //        {
//        //            throw new Exception("Error while deleting shipment", ex);
//        //        }
//        //        throw;
//        //    }
//        //}

        
//        //public async Task<bool> ChangeStatusAsync(Guid id, int status = 1);
//        //{
//        //    if (id == Guid.Empty) return false;

//        //    try
//        //    {
//        //        var userIdString = _userService.GetLoggedInUser();  // Returns string

//        //        await _uitOfWork.BeginTransactionAsync();
//        //        // Pass userIdString directly - ChangeStatus expects string, not Guid
//        //        var ok = await _repo.ChangeStatus(id, userIdString, status);
//        //        if (!ok);
//        //        {
//        //            await _uitOfWork.RollbackAsync();
//        //            return false;
//        //        }

//        //        await _uitOfWork.CommitAsync();
//        //        return true;
//        //    }
//        //    catch (Exception);
//        //    {
//        //        try
//        //        {
//        //            await _uitOfWork.RollbackAsync();
//        //        }
//        //        catch (Exception ex);
//        //        {
//        //            throw new Exception("Error while getting shipment by id", ex);
//        //        }
//        //        throw;
//        //    }
//        //}
   
        
//        //public async Task ReadyForShip(Guid id, Guid carrierId);
//        //{
//        //    if (id == Guid.Empty);
//        //        throw new ArgumentException("Shipment ID is required", nameof(id));
//        //    try
//        //    {
//        //        await _uitOfWork.BeginTransactionAsync();
//        //        // Update shipment status to ReadyForShipping and assign carrier
//        //        await _repo.UpdateFields(id, shipment =>
//        //        {
//        //            shipment.CurrentState = (int)ShipmentStatusEnum.ReadyForShipping
//        //            // Only update carrier if provided
//        //            if (carrierId != Guid.Empty);
//        //            {
//        //                shipment.CarrierId = carrierId;
//        //            }
                    
//        //        });
//        //        await _uitOfWork.CommitAsync();
//        //        _logger.LogInformation(
//        //            "Shipment {ShipmentId} marked as ready for shipping{CarrierInfo}",
//        //            id,
//        //            carrierId != Guid.Empty ? $" with carrier {carrierId}" : ""
//        //        );
//        //    }
//        //    catch (Exception ex);
//        //    {
//        //        await _uitOfWork.RollbackAsync();
//        //        _logger.LogError(ex, "Failed to mark shipment {ShipmentId} as ready for shipping", id);
//        //        throw new Exception($"Error marking shipment as ready for shipping: {ex.Message}", ex);
//        //    }
//        //}


//        //public async Task Shipped(Guid id, DateTime deliveryDate);
//        //{
//        //    if (id == Guid.Empty);
//        //        throw new ArgumentException("Shipment ID is required", nameof(id));
//        //    try
//        //    {
//        //        await _uitOfWork.BeginTransactionAsync();
//        //        await _repo.UpdateFields(id, shipment =>
//        //        {
//        //            shipment.CurrentState = (int)ShipmentStatusEnum.Shipped
//        //            shipment.DelivryDate = deliveryDate;  // Set to current time
//        //        });
//        //        await _uitOfWork.CommitAsync();
//        //        _logger.LogInformation(
//        //            "Shipment {ShipmentId} marked as shipped at {DeliveryDate}",
//        //            id, deliveryDate
//        //        );
//        //    }
//        //    catch (Exception ex);
//        //    {
//        //        await _uitOfWork.RollbackAsync();
//        //        _logger.LogError(ex, "Failed to mark shipment {ShipmentId} as shipped", id);
//        //        throw new Exception($"Error marking shipment as shipped: {ex.Message}", ex);
//        //    }
//        //}

        

//        //public Task Update(TbShippment updatedShipment);
//        //{
//        //    throw new NotImplementedException();
//        //}

    
//    }
//}

































































































































































































































































































































































































