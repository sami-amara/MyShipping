using Business.Contracts;
using Business.Contracts.Shipment;
using Business.DTOS;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Contracts;
using Domains;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;

namespace Business.Services.Shipment
{
    public class ShipmentCommandService : BaseService<TbShippment, ShippmentDto>, IShipmentCommand
    {


        IUserService _userService;
        IUserReceiver _userReceiver;
        IUserSender _userSender;
        ITrackingNumberCreator _trackingNumberCreator;
        IRateCalculator _rateCalculator;
        IUnitOfWork _unitOfWork;
        IGenericRepository<TbShippment> _repo;
        IMapper _mapper;
        IShipmentsStatus _shippmentStatus;
        IPaymentTransactionService _paymentTransactionService;
        private readonly ILogger<ShipmentCommandService> _logger;

        public ShipmentCommandService(IGenericRepository<TbShippment> repo, IMapper mapper,
                                 IUserService userService, IUserSender userSender,
                                 IUserReceiver userReceiver,
                                 ITrackingNumberCreator trackingNumberCreator,
                                 IRateCalculator rateCalculator, IShipmentsStatus shippmentStatus,
                                 IUnitOfWork unitOfWork,
                                 IPaymentTransactionService paymentTransactionService,
                                 ILogger<ShipmentCommandService> logger) : base(unitOfWork, mapper, userService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _repo = repo;
            _userService = userService;
            _userReceiver = userReceiver;
            _userSender = userSender;
            _trackingNumberCreator = trackingNumberCreator;
            _rateCalculator = rateCalculator;
            _logger = logger;
            _shippmentStatus = shippmentStatus;
            _paymentTransactionService = paymentTransactionService;
        }

        public async Task Create(ShippmentDto shippment)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                //Create tracking number & calculate rate(sync helpers);
                shippment.TrackingNumber = _trackingNumberCreator.GenerateTrackingNumber(shippment);
                shippment.ShippingRate = _rateCalculator.CalculateRate(shippment);
                var userId = _userService.GetLoggedInUser();
                //Save Sender
                if (shippment.SenderId == Guid.Empty && shippment.UserSender != null)
                {
                    shippment.UserSender.UserId = userId;
                    var (senderSuccess, senderId) = await _userSender.Add(shippment.UserSender);
                    shippment.SenderId = senderId;
                }

                if (shippment.ReceiverId == Guid.Empty && shippment.UserReceiver != null)
                {
                    shippment.UserReceiver.UserId = userId;
                    var (receiverSuccess, receiverId) = await _userReceiver.Add(shippment.UserReceiver);
                    shippment.ReceiverId = receiverId;
                }

                // Save Shipment using the Add method that returns created Id
                var (createdOk, createdId) = await this.Add(shippment);
                if (!createdOk)
                {
                    await _unitOfWork.RollbackAsync();
                    throw new Exception("Failed to add shipment");
                }

                // ? IMPORTANT: Assign the created ID back to the DTO so it's available to the controller
                shippment.Id = createdId;

                // Create and persist initial shipment status using the ShipmentsStatusService
                var (statusOk, statusId) = await _shippmentStatus.Add(createdId, ShipmentStatusEnum.Created);
                if (!statusOk)
                {
                    await _unitOfWork.RollbackAsync();
                    throw new Exception("Failed to add shipment status");
                }

                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                try { await _unitOfWork.RollbackAsync(); } catch { /* swallow */ }

                //if (ex is InvalidOperationException && ex.Message.Contains("payment failed", StringComparison.OrdinalIgnoreCase));
                //{
                //    throw;
                //}

                throw new Exception("Error while creating shipment", ex);
            }
        }


        public async Task Edit(ShippmentDto shippment)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                //Calculate rate
                shippment.ShippingRate = _rateCalculator.CalculateRate(shippment);
                // Preserve payment-owned fields from DB so edit/status actions never reset them
                var existingShipment = await _repo.GetById(shippment.Id);
                if (existingShipment != null)
                {
                    shippment.IsPaid = existingShipment.IsPaid;
                    shippment.PaymentMethodId = existingShipment.PaymentMethodId;
                }

                shippment.UserSender.Id = shippment.SenderId;
                var senderResult = await _userSender.UpdateAsync(shippment.UserSender);
                shippment.UserReceiver.Id = shippment.ReceiverId;
                var ReceiverResult = await _userReceiver.UpdateAsync(shippment.UserReceiver);
                await this.UpdateAsync(shippment);
                await _repo.UpdateFields(shippment.Id, a =>
                {
                    a.CurrentState = (int)ShipmentStatusEnum.Updated;
                });
                //await _shippmentStatus.Add(shippment.Id, ShipmentStatusEnum.Approved);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("Error while creating shipment", ex);
            }

        }

        public async Task Approved(ShippmentDto shippment)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                // Calculate rate if needed
                shippment.ShippingRate = _rateCalculator.CalculateRate(shippment);
                shippment.UserSender.Id = shippment.SenderId;
                await _userSender.UpdateAsync(shippment.UserSender);
                shippment.UserReceiver.Id = shippment.ReceiverId;
                await _userReceiver.UpdateAsync(shippment.UserReceiver);
                // Set status to Approved
                shippment.CurrentState = (int)ShipmentStatusEnum.Approved;

                await this.UpdateAsync(shippment);
                await _repo.UpdateFields(shippment.Id, a =>
                {
                    a.CurrentState = (int)ShipmentStatusEnum.Approved;
                });
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("Error while approving shipment", ex);
            }
        }


        public async Task EditFields(Guid id, Action<TbShippment> updateAction)
        {
            await _repo.UpdateFields(id, updateAction);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            if (id == Guid.Empty) return false;

            try
            {
                var userIdString = _userService.GetLoggedInUser();
                await _unitOfWork.BeginTransactionAsync();
                // ? Soft delete: Set IsDeleted, DeletedDate, DeletedBy
                var deleted = await _repo.Delete(id, userIdString);
                if (!deleted)
                {
                    await _unitOfWork.RollbackAsync();
                    return false;
                }

                // ? Also update CurrentState for workflow tracking and backward compatibility
                await _repo.ChangeStatus(id, userIdString, (int)ShipmentStatusEnum.Deleted);
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                try
                {
                    await _unitOfWork.RollbackAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while deleting shipment", ex);
                }
                throw;
            }
        }


        public async Task<bool> ChangeStatusAsync(Guid id, int status = 1)
        {
            if (id == Guid.Empty) return false;

            try
            {
                var userIdString = _userService.GetLoggedInUser();  // Returns string

                await _unitOfWork.BeginTransactionAsync();
                // Pass userIdString directly - ChangeStatus expects string, not Guid
                var ok = await _repo.ChangeStatus(id, userIdString, status);
                if (!ok)
                {
                    await _unitOfWork.RollbackAsync();
                    return false;
                }

                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                try
                {
                    await _unitOfWork.RollbackAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while getting shipment by id", ex);
                }
                throw;
            }
        }


        public async Task ReadyForShip(Guid id, Guid carrierId)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Shipment ID is required", nameof(id));
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                // Update shipment status to ReadyForShipping and assign carrier
                await _repo.UpdateFields(id, shipment =>
                {
                    shipment.CurrentState = (int)ShipmentStatusEnum.ReadyForShipping;
                    // Only update carrier if provided
                    if (carrierId != Guid.Empty)
                    {
                        shipment.CarrierId = carrierId;
                    }

                });
                await _unitOfWork.CommitAsync();
                _logger.LogInformation(
                    "Shipment {ShipmentId} marked as ready for shipping{CarrierInfo}",
                    id,
                    carrierId != Guid.Empty ? $" with carrier {carrierId}" : ""
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Failed to mark shipment {ShipmentId} as ready for shipping", id);
                throw new Exception($"Error marking shipment as ready for shipping: {ex.Message}", ex);
            }
        }


        public async Task Shipped(Guid id, DateTime deliveryDate)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Shipment ID is required", nameof(id));
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _repo.UpdateFields(id, shipment =>
                {
                    shipment.CurrentState = (int)ShipmentStatusEnum.Shipped;
                    shipment.DelivryDate = deliveryDate;  // Set to current time
                });
                await _unitOfWork.CommitAsync();
                _logger.LogInformation(
                    "Shipment {ShipmentId} marked as shipped at {DeliveryDate}",
                    id, deliveryDate
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Failed to mark shipment {ShipmentId} as shipped", id);
                throw new Exception($"Error marking shipment as shipped: {ex.Message}", ex);
            }
        }
    }
}
