using AutoMapper;
using Business.Contracts;
using Business.Contracts.Logging;
using Business.Contracts.Payments;
using Business.Contracts.Shipment;
using Business.DTOS;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Contracts;
using Domains;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private readonly ISecurityEventLogger _securityLogger;
      
        private readonly ILogger<ShipmentCommandService> _logger;

        public ShipmentCommandService(IGenericRepository<TbShippment> repo, IMapper mapper,
                                 IUserService userService, IUserSender userSender,
                                 IUserReceiver userReceiver,
                                 ITrackingNumberCreator trackingNumberCreator,
                                 IRateCalculator rateCalculator, IShipmentsStatus shippmentStatus,
                                 ISecurityEventLogger securityLogger,
                                 IUnitOfWork unitOfWork,
                             

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
            _securityLogger = securityLogger;
        }

     
        public async Task EditFields(Guid id, Action<TbShippment> updateAction)
        {
            await _repo.UpdateFields(id, updateAction);
        }

     
        // ══════════════════════════════════════════════════════════════════════
        // 🔁 CENTRALIZED TRANSITION ENGINE (Eliminates all duplication)
        // ══════════════════════════════════════════════════════════════════════
        private async Task ExecuteTransitionAsync(
            Guid id,
            Guid userId,
            string statusName,
            Action<TbShippment> updateAction,
            string? reason = null,
            string? carrierId = null,
            string severity = "Information")
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Shipment ID is required", nameof(id));
            try
            {
                await _unitOfWork.BeginTransactionAsync();
             
                var shipment = await _repo.GetById(id);
                if (shipment == null)
                    throw new InvalidOperationException($"Shipment with ID '{id}' was not found.");
                // Apply state-specific changes
                await _repo.UpdateFields(id, updateAction);
                await _unitOfWork.CommitAsync();
                // Centralized Security & Audit Event
                await _securityLogger.LogShipmentStatusChanged(
                    shipmentId: id,
                    status: statusName,
                    userId: userId.ToString(),
                    trackingNumber: shipment.TrackingNumber?.ToString(),
                    carrierId: carrierId ?? shipment.CarrierId?.ToString(),
                    reason: reason,
                    severity: severity);
                //_logger.LogInformation("Shipment {ShipmentId} transitioned to {Status} by {UserId}", id, statusName, userId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Failed to transition shipment {ShipmentId} to {Status}", id, statusName);
                throw;
            }
        }
        public async Task Create(ShippmentDto shippment)
        {
            if (shippment == null)
                throw new ArgumentNullException(nameof(shippment));

            var userId = _userService.GetLoggedInUser();

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 1. Generate tracking & rate
                shippment.TrackingNumber = _trackingNumberCreator.GenerateTrackingNumber(shippment);
                shippment.ShippingRate = _rateCalculator.CalculateRate(shippment);
                shippment.CurrentState = (int)ShipmentStatusEnum.Created;
                shippment.IsPaid = false;
               // shippment.DelivryDate = null;
                shippment.CreatedBy = userId;
                shippment.CreatedDate = DateTime.UtcNow;

                // 2. Save Sender if new
                if (shippment.SenderId == Guid.Empty && shippment.UserSender != null)
                {
                    shippment.UserSender.UserId = userId;
                    var (_, senderId) = await _userSender.Add(shippment.UserSender);
                    shippment.SenderId = senderId;
                }

                // 3. Save Receiver if new
                if (shippment.ReceiverId == Guid.Empty && shippment.UserReceiver != null)
                {
                    shippment.UserReceiver.UserId = userId;
                    var (_, receiverId) = await _userReceiver.Add(shippment.UserReceiver);
                    shippment.ReceiverId = receiverId;
                }

                // 4. Save Shipment
                var (createdOk, createdId) = await this.Add(shippment);
                if (!createdOk)
                    throw new InvalidOperationException("Failed to add shipment to database.");

                shippment.Id = createdId;

                // 5. Initial status history
                await _shippmentStatus.Add(createdId, ShipmentStatusEnum.Created);

                await _unitOfWork.CommitAsync();

                // 6. ✅ Centralized Security & Audit Event
                await _securityLogger.LogShipmentStatusChanged(
                    shipmentId: createdId,
                    status: "Created",
                    userId: userId.ToString(),
                    trackingNumber: shippment.TrackingNumber?.ToString(),
                    carrierId: shippment.CarrierId?.ToString(),
                    reason: "Initial shipment creation",
                    severity: "Information");

                //_logger.LogInformation("Shipment {ShipmentId} created by {UserId}", createdId, userId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error while creating shipment");
                throw new Exception("Error while creating shipment", ex);
            }
        }

        public async Task Edit(ShippmentDto shippment)
        {
            if (shippment == null || shippment.Id == Guid.Empty)
                throw new ArgumentException("Valid shipment is required for edit.", nameof(shippment));

            var userId = _userService.GetLoggedInUser();

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 1. Recalculate rate
                shippment.ShippingRate = _rateCalculator.CalculateRate(shippment);

                // 2. Preserve database payment fields so edits never erase paid state
                var existingShipment = await _repo.GetById(shippment.Id);
                if (existingShipment == null)
                    throw new InvalidOperationException($"Shipment with ID '{shippment.Id}' was not found.");

                shippment.IsPaid = existingShipment.IsPaid;
                shippment.PaymentMethodId = existingShipment.PaymentMethodId;
                shippment.CurrentState = (int)ShipmentStatusEnum.Updated;

                // 3. Update Sender & Receiver
                if (shippment.UserSender != null)
                {
                    shippment.UserSender.Id = shippment.SenderId;
                    await _userSender.UpdateAsync(shippment.UserSender);
                }

                if (shippment.UserReceiver != null)
                {
                    shippment.UserReceiver.Id = shippment.ReceiverId;
                    await _userReceiver.UpdateAsync(shippment.UserReceiver);
                }

                // 4. Update shipment entity
                await this.UpdateAsync(shippment);

                await _repo.UpdateFields(shippment.Id, a =>
                {
                    a.CurrentState = (int)ShipmentStatusEnum.Updated;
                });

                // 5. Add status history entry
                await _shippmentStatus.Add(shippment.Id, ShipmentStatusEnum.Updated);

                await _unitOfWork.CommitAsync();

                // 6. ✅ Centralized Security & Audit Event
                await _securityLogger.LogShipmentStatusChanged(
                    shipmentId: shippment.Id,
                    status: "Updated",
                    userId: userId.ToString(),
                    trackingNumber: existingShipment.TrackingNumber?.ToString(),
                    reason: "Shipment details edited/updated",
                    severity: "Information");

                //_logger.LogInformation("Shipment {ShipmentId} edited by {UserId}", shippment.Id, userId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error while editing shipment {ShipmentId}", shippment.Id);
                throw new Exception("Error while editing shipment", ex);
            }
        }
        // ══════════════════════════════════════════════════════════════════════
        // 📦 CLEAN, CONCISE STATUS COMMANDS
        // ══════════════════════════════════════════════════════════════════════
        public Task Approved(Guid id, Guid userId) =>
            ExecuteTransitionAsync(id, userId, "Approved", s =>
            {
                s.CurrentState = (int)ShipmentStatusEnum.Approved;
                s.ApprovedBy = userId;
                s.ApprovedAt = DateTime.UtcNow;
            });
        public Task ReadyForShip(Guid id, Guid carrierId, Guid userId) =>
            ExecuteTransitionAsync(id, userId, "ReadyForShipping", s =>
            {
                s.CurrentState = (int)ShipmentStatusEnum.ReadyForShipping;
                s.CarrierId = carrierId;
                s.ReadyBy = userId;
                s.ReadyAt = DateTime.UtcNow;
            }, carrierId: carrierId.ToString());
        public Task Shipped(Guid id, Guid userId) =>
            ExecuteTransitionAsync(id, userId, "Shipped", s =>
            {
                s.CurrentState = (int)ShipmentStatusEnum.Shipped;
                s.ShippedBy = userId;
                s.ShippedAt = DateTime.UtcNow;
            });
        public Task Delivered(Guid id, Guid userId) =>
            ExecuteTransitionAsync(id, userId, "Delivered", s =>
            {
                s.CurrentState = (int)ShipmentStatusEnum.Delivered;
                s.DeliveredBy = userId;
                s.DeliveredAt = DateTime.UtcNow;
                s.DelivryDate = DateTime.UtcNow;
            }, reason: "Package marked as delivered");
        public Task Cancelled(Guid id, Guid userId, string? reason = null) =>
            ExecuteTransitionAsync(id, userId, "Cancelled", s =>
            {
                s.CurrentState = (int)ShipmentStatusEnum.Cancelled;
                s.CancelledBy = userId;
                s.CancelledAt = DateTime.UtcNow;
                s.CancellationReason = reason;
                s.IsPaid = false;
            }, reason: reason, severity: "Warning");
        public Task Returned(Guid id, Guid userId, string? reason = null) =>
            ExecuteTransitionAsync(id, userId, "Returned", s =>
            {
                s.CurrentState = (int)ShipmentStatusEnum.Returned;
                s.ReturnedBy = userId;
                s.ReturnedAt = DateTime.UtcNow;
                s.ReturnReason = reason;
            }, reason: reason, severity: "Warning");
        public Task Refunded(Guid id, Guid userId, string? reason = null) =>
            ExecuteTransitionAsync(id, userId, "Refunded", s =>
            {
                s.CurrentState = (int)ShipmentStatusEnum.Refunded;
                s.IsPaid = false;
                s.RefundedBy = userId;
                s.RefundedAt = DateTime.UtcNow;
                s.RefundReason = reason;
            }, reason: reason, severity: "Warning");
        public Task Delete(Guid id, Guid userId) =>
            ExecuteTransitionAsync(id, userId, "Deleted", s =>
            {
                s.CurrentState = (int)ShipmentStatusEnum.Deleted;
                s.IsDeleted = true;
                s.DeletedBy = userId;
            }, reason: "Shipment soft-deleted by user/admin", severity: "Warning");


        public async Task<bool> ChangeStatusAsync(Guid id, int status = 1)
        {
            if (id == Guid.Empty) return false;

            var userId = _userService.GetLoggedInUser();
            var statusEnum = (ShipmentStatusEnum)status;

            //await ExecuteTransitionAsync(
            //    id: id,
            //    userId: userId,
            //    statusName: statusEnum.ToString(),
            //    updateAction: s => s.CurrentState = status);

            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            if (id == Guid.Empty) return false;

            var userId = _userService.GetLoggedInUser();
            await Delete(id, userId);
            return true;
        }
        //public async Task Approved(Guid shipmentId, Guid userId)
        //{
        //    if (shipmentId == Guid.Empty)
        //        throw new ArgumentException("Shipment ID is required", nameof(shipmentId));

        //    try
        //    {
        //        await _unitOfWork.BeginTransactionAsync();

        //        // Load existing shipment to validate payment
        //        var existingShipment = await _repo.GetById(shipmentId);
        //        if (existingShipment == null)
        //            throw new InvalidOperationException("Shipment not found.");
        //        if (!existingShipment.IsPaid)
        //            throw new InvalidOperationException("Shipment must be paid before approval.");

        //        existingShipment.TrackingNumber?.ToString();
        //        // Update audit fields directly
        //        await _repo.UpdateFields(shipmentId, a =>
        //        {
        //            a.CurrentState = (int)ShipmentStatusEnum.Approved;
        //            a.ApprovedBy = userId;
        //            a.ApprovedAt = DateTime.UtcNow;
        //        });

        //        await _unitOfWork.CommitAsync();

        //        // 🆕 Security & Audit Event
        //        await _securityLogger.LogShipmentStatusChanged(
        //            shipmentId: shipmentId,
        //            status: "Approved",
        //            userId: userId.ToString(),
        //            trackingNumber: existingShipment.TrackingNumber?.ToString());


        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackAsync();
        //        throw new Exception("Error while approving shipment", ex);
        //    }
        //}
        //public async Task ReadyForShip(Guid id, Guid carrierId, Guid userId)
        //{
        //    if (id == Guid.Empty)
        //        throw new ArgumentException("Shipment ID is required", nameof(id));

        //    try
        //    {
        //        await _unitOfWork.BeginTransactionAsync();

        //        var existingShipment = await _repo.GetById(id);
        //        if (existingShipment == null)
        //            throw new InvalidOperationException("Shipment not found.");
        //        if (!existingShipment.IsPaid)
        //            throw new InvalidOperationException("Shipment must be paid before approval.");
        //        existingShipment.TrackingNumber?.ToString();


        //        await _repo.UpdateFields(id, shipment =>
        //        {
        //            shipment.CurrentState = (int)ShipmentStatusEnum.ReadyForShipping;
        //            shipment.CarrierId = carrierId;
        //            shipment.ReadyBy = userId;
        //            shipment.ReadyAt = DateTime.UtcNow;
        //            //shipment.UpdatedDate = DateTime.UtcNow;
        //            //shipment.UpdatedBy = userId;
        //        });

        //        await _unitOfWork.CommitAsync();

        //        // ✅ Security Audit: Logs CarrierId with the Ready status!
        //        await _securityLogger.LogShipmentStatusChanged(
        //            shipmentId: id,
        //            status: "ReadyForShipping",
        //            userId: userId.ToString(),
        //            carrierId: carrierId.ToString(),
        //            trackingNumber: existingShipment.TrackingNumber?.ToString(),
        //            severity: "Information");


        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackAsync();
        //        _logger.LogError(ex, "Failed to mark shipment {ShipmentId} as ready for shipping", id);
        //        throw new Exception($"Error marking shipment ready for shipping: {ex.Message}", ex);
        //    }
        //}




        //public async Task Shipped(Guid id, Guid userId)
        //{
        //    if (id == Guid.Empty)
        //        throw new ArgumentException("Shipment ID is required", nameof(id));
        //    try
        //    {
        //        await _unitOfWork.BeginTransactionAsync();

        //        var existingShipment = await _repo.GetById(id);
        //        if (existingShipment == null)
        //            throw new InvalidOperationException("Shipment not found.");
        //        if (!existingShipment.IsPaid)
        //            throw new InvalidOperationException("Shipment must be paid before approval.");
        //        existingShipment.TrackingNumber?.ToString();

        //        await _repo.UpdateFields(id, shipment =>
        //        {
        //            shipment.CurrentState = (int)ShipmentStatusEnum.Shipped;
        //            shipment.ShippedBy = userId;
        //            shipment.ShippedAt = DateTime.UtcNow;
        //           // shipment.DelivryDate = deliveryDate;  // Set to current time
        //        });
        //        await _unitOfWork.CommitAsync();

        //        // 🆕 Security & Audit Event
        //        await _securityLogger.LogShipmentStatusChanged(
        //            shipmentId: id,
        //            status: "Shipped",
        //            userId: userId.ToString(),
        //            trackingNumber: existingShipment.TrackingNumber?.ToString());

        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackAsync();
        //        _logger.LogError(ex, "Failed to mark shipment {ShipmentId} as shipped", id);
        //        throw new Exception($"Error marking shipment as shipped: {ex.Message}", ex);
        //    }
        //}

        //public async Task Delivered(Guid shipmentId, Guid userId)
        //{
        //    if (shipmentId == Guid.Empty)
        //        throw new ArgumentException("Shipment ID is required", nameof(shipmentId));

        //    try
        //    {
        //        await _unitOfWork.BeginTransactionAsync();


        //        var existingShipment = await _repo.GetById(shipmentId);
        //        if (existingShipment == null)
        //            throw new InvalidOperationException("Shipment not found.");
        //        if (!existingShipment.IsPaid)
        //            throw new InvalidOperationException("Shipment must be paid before approval.");
        //        existingShipment.TrackingNumber?.ToString();
        //        // Update audit fields and actual delivery timestamp
        //        await _repo.UpdateFields(shipmentId, a =>
        //        {
        //            a.CurrentState = (int)ShipmentStatusEnum.Delivered;
        //            a.DeliveredBy = userId;
        //            a.DeliveredAt = DateTime.UtcNow;
        //            a.DelivryDate = DateTime.UtcNow; // ✅ Sets real delivery date
        //        });

        //        await _unitOfWork.CommitAsync();
        //        // ✅ Centralized Security & Audit Event (With Tracking Number & Carrier)
        //        await _securityLogger.LogShipmentStatusChanged(
        //            shipmentId: shipmentId,
        //            status: "Delivered",
        //            userId: userId.ToString(),
        //            trackingNumber: existingShipment.TrackingNumber?.ToString(),
        //            carrierId: existingShipment.CarrierId?.ToString(),
        //            reason: "Package marked as delivered",
        //            severity: "Information");
        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackAsync();
        //        throw new Exception("Error while delivering shipment", ex);
        //    }
        //}


        //public async Task Delete(Guid id, Guid userId)
        //{
        //    if (id == Guid.Empty)
        //        throw new ArgumentException("Shipment ID is required", nameof(id));

        //    try
        //    {
        //        await _unitOfWork.BeginTransactionAsync();

        //        var existingShipment = await _repo.GetById(id);
        //        if (existingShipment == null)
        //            throw new InvalidOperationException("Shipment not found.");
        //        existingShipment.TrackingNumber?.ToString();
        //        await _repo.UpdateFields(id, shipment =>
        //        {
        //            shipment.CurrentState = (int)ShipmentStatusEnum.Deleted;
        //            shipment.IsDeleted = true;
        //            shipment.DeletedBy = userId;
        //            //shipment.DeletedDate = DateTime.UtcNow;
        //            //shipment.UpdatedDate = DateTime.UtcNow;
        //            //shipment.UpdatedBy = userId;
        //        });

        //        await _unitOfWork.CommitAsync();

        //        // ✅ Security Audit: Log Deletion with 'Warning' severity!
        //        await _securityLogger.LogShipmentStatusChanged(
        //            shipmentId: id,
        //            status: "Deleted",
        //            userId: userId.ToString(),
        //            trackingNumber: existingShipment.TrackingNumber?.ToString(),
        //            reason: "Shipment soft-deleted by user/admin",
        //            severity: "Warning"); // 👈 High visibility in security dashboards


        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackAsync();
        //        _logger.LogError(ex, "Failed to delete shipment {ShipmentId}", id);
        //        throw new Exception($"Error deleting shipment: {ex.Message}", ex);
        //    }
        //}



        //public async Task Refunded(Guid id, Guid userId, string? reason = null)
        //{
        //    if (id == Guid.Empty)
        //        throw new ArgumentException("Shipment ID is required", nameof(id));

        //    try
        //    {
        //        await _unitOfWork.BeginTransactionAsync();

        //        var existingShipment = await _repo.GetById(id);
        //        if (existingShipment == null)
        //            throw new InvalidOperationException("Shipment not found.");
        //        existingShipment.TrackingNumber?.ToString();
        //        await _repo.UpdateFields(id, shipment =>
        //        {
        //            shipment.CurrentState = (int)ShipmentStatusEnum.Refunded;
        //            shipment.IsPaid = false; // 👈 CRITICAL: Money was refunded, so it is no longer paid!
        //            shipment.RefundedBy   = userId;
        //            shipment.RefundedAt = DateTime.UtcNow;
        //            shipment.RefundReason = reason; // If you have this column
        //            //shipment.UpdatedDate = DateTime.UtcNow;
        //            //shipment.UpdatedBy = userId;
        //        });

        //        await _unitOfWork.CommitAsync();

        //        // 🆕 Security & Audit Event
        //        await _securityLogger.LogShipmentStatusChanged(
        //            shipmentId: id,
        //            status: "Refunded",
        //            userId: userId.ToString(),
        //            trackingNumber: existingShipment.TrackingNumber?.ToString(),
        //            reason: reason,
        //            severity: "Warning");
        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackAsync();
        //        _logger.LogError(ex, "Failed to mark shipment {ShipmentId} as refunded", id);
        //        throw new Exception($"Error refunding shipment: {ex.Message}", ex);
        //    }
        //}


        //public async Task Cancelled(Guid id, Guid userId, string? reason = null)
        //{
        //    if (id == Guid.Empty)
        //        throw new ArgumentException("Shipment ID is required", nameof(id));

        //    try
        //    {
        //        await _unitOfWork.BeginTransactionAsync();

        //        var existingShipment = await _repo.GetById(id);
        //        if (existingShipment == null)
        //            throw new InvalidOperationException("Shipment not found.");
        //        existingShipment.TrackingNumber?.ToString();
        //        await _repo.UpdateFields(id, shipment =>
        //        {
        //            shipment.CurrentState = (int)ShipmentStatusEnum.Cancelled;
        //            shipment.CancelledBy = userId;
        //            shipment.CancelledAt = DateTime.UtcNow;
        //            shipment.CancellationReason = reason;
        //            shipment.IsPaid = false; // Cancellation closes payment
        //            //shipment.UpdatedDate = DateTime.UtcNow;
        //            //shipment.UpdatedBy = userId;
        //        });

        //        await _unitOfWork.CommitAsync();

        //        await _securityLogger.LogShipmentStatusChanged(
        //            shipmentId: id,
        //            status: "Cancelled",
        //            userId: userId.ToString(),
        //            trackingNumber: existingShipment.TrackingNumber?.ToString(),
        //            reason: reason,
        //            severity: "Warning");
        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackAsync();
        //        _logger.LogError(ex, "Failed to cancel shipment {ShipmentId}", id);
        //        throw new Exception($"Error cancelling shipment: {ex.Message}", ex);
        //    }
        //}

        //public async Task Returned(Guid id, Guid userId, string? reason = null)
        //{
        //    if (id == Guid.Empty)
        //        throw new ArgumentException("Shipment ID is required", nameof(id));

        //    try
        //    {
        //        await _unitOfWork.BeginTransactionAsync();

        //        var existingShipment = await _repo.GetById(id);
        //        if (existingShipment == null)
        //            throw new InvalidOperationException("Shipment not found.");
        //        existingShipment.TrackingNumber?.ToString();
        //        await _repo.UpdateFields(id, shipment =>
        //        {
        //            shipment.CurrentState = (int)ShipmentStatusEnum.Returned;
        //            shipment.ReturnedBy = userId;
        //            shipment.ReturnedAt = DateTime.UtcNow;
        //            shipment.ReturnReason = reason;
        //            //shipment.UpdatedDate = DateTime.UtcNow;
        //            //shipment.UpdatedBy = userId;
        //        });

        //        await _unitOfWork.CommitAsync();
        //        // ✅ Security & Audit Log: Records the Return with the exact Reason
        //        await _securityLogger.LogShipmentStatusChanged(
        //            shipmentId: id,
        //            status: "Returned",
        //            userId: userId.ToString(),
        //            trackingNumber: existingShipment.TrackingNumber?.ToString(),
        //            carrierId: existingShipment.CarrierId?.ToString(),
        //            reason: reason,
        //            severity: "Warning"); // 👈 Warning severity for high visibility in audits

        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackAsync();
        //        _logger.LogError(ex, "Failed to mark shipment {ShipmentId} as returned", id);
        //        throw new Exception($"Error returning shipment: {ex.Message}", ex);
        //    }
        //}

    }
}
