using Business.Contracts.Shipment;
using Business.DTOS;
using DataAccessLayer.Contracts;
using DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Shipment
{
    public class ShipmentAuditService : IShipmentAuditService
    {
        private readonly ISecurityAuditLogRepository _auditRepo;

        public ShipmentAuditService(ISecurityAuditLogRepository auditRepo)
        {
            _auditRepo = auditRepo;
        }

        public async Task<IEnumerable<ShipmentTimelineEventDto>> GetShipmentTimelineAsync(Guid shipmentId)
        {
            var events = await _auditRepo.GetList<ShipmentTimelineEventDto>(
                filter: log => log.ShipmentId == shipmentId,
                selector: log => new ShipmentTimelineEventDto
                {
                    Id = log.Id,
                    ShipmentId = log.ShipmentId,
                    TrackingNumber = log.TrackingNumber ?? "N/A",
                    ShipmentEventType = log.ShipmentEventType ?? log.EventType,
                    Severity = log.Severity,
                    Timestamp = log.Timestamp,
                    UserId = log.UserId,
                    Email = log.Email ?? (log.User != null ? log.User.UserName : null) ?? (log.User != null ? log.User.Email : null) ?? "System",
                    UserDisplayName = (log.User != null ? log.User.UserName : null) ?? "System",
                    IpAddress = log.IpAddress ?? "N/A",
                    Description = log.Description,
                    Reason = log.Reason,
                    Metadata = log.Metadata
                },
                orderBy: log => log.Timestamp,
                isDescending: false
            );

            return events;
        }

        public async Task<IEnumerable<ShipmentTimelineEventDto>> GetShipmentTimelineByTrackingAsync(string trackingNumber)
        {
            var events = await _auditRepo.GetList<ShipmentTimelineEventDto>(
                filter: log => log.TrackingNumber == trackingNumber,
                selector: log => new ShipmentTimelineEventDto
                {
                    Id = log.Id,
                    ShipmentId = log.ShipmentId,
                    TrackingNumber = log.TrackingNumber ?? "N/A",
                    ShipmentEventType = log.ShipmentEventType ?? log.EventType,
                    Severity = log.Severity,
                    Timestamp = log.Timestamp,
                    UserId = log.UserId,
                    Email = log.Email ?? (log.User != null ? log.User.UserName : null) ?? (log.User != null ? log.User.Email : null) ?? "System",
                    UserDisplayName = (log.User != null ? log.User.UserName : null) ?? "System",
                    IpAddress = log.IpAddress ?? "N/A",
                    Description = log.Description,
                    Reason = log.Reason,
                    Metadata = log.Metadata
                },
                orderBy: log => log.Timestamp,
                isDescending: false
            );

            return events;
        }

        public async Task<IEnumerable<ShipmentTimelineEventDto>> GetUserShipmentEventsAsync(string userId, int pageSize = 50)
        {
            var events = await _auditRepo.GetList<ShipmentTimelineEventDto>(
                filter: log => log.UserId == userId && log.ShipmentId != null,
                selector: log => new ShipmentTimelineEventDto
                {
                    Id = log.Id,
                    ShipmentId = log.ShipmentId,
                    TrackingNumber = log.TrackingNumber ?? "N/A",
                    ShipmentEventType = log.ShipmentEventType ?? log.EventType,
                    Severity = log.Severity,
                    Timestamp = log.Timestamp,
                    UserId = log.UserId,
                    Email = log.Email ?? (log.User != null ? log.User.UserName : null) ?? (log.User != null ? log.User.Email : null) ?? "System",
                    UserDisplayName = (log.User != null ? log.User.UserName : null) ?? "System",
                    IpAddress = log.IpAddress ?? "N/A",
                    Description = log.Description,
                    Reason = log.Reason,
                    Metadata = log.Metadata
                },
                orderBy: log => log.Timestamp,
                isDescending: true
            );

            return events.Take(pageSize);
        }

        public async Task<IEnumerable<ShipmentTimelineEventDto>> SearchUserShipmentEventsAsync(string userSearch, int pageSize = 50)
        {
            var events = await _auditRepo.GetList<ShipmentTimelineEventDto>(
                filter: log => log.ShipmentId != null &&
                    (log.UserId == userSearch ||
                     log.Email == userSearch ||
                     (log.User != null && log.User.UserName == userSearch) ||
                     (log.User != null && log.User.Email == userSearch)),
                selector: log => new ShipmentTimelineEventDto
                {
                    Id = log.Id,
                    ShipmentId = log.ShipmentId,
                    TrackingNumber = log.TrackingNumber ?? "N/A",
                    ShipmentEventType = log.ShipmentEventType ?? log.EventType,
                    Severity = log.Severity,
                    Timestamp = log.Timestamp,
                    UserId = log.UserId,
                    Email = log.Email ?? (log.User != null ? log.User.UserName : null) ?? (log.User != null ? log.User.Email : null) ?? "System",
                    UserDisplayName = (log.User != null ? log.User.UserName : null) ?? "System",
                    IpAddress = log.IpAddress ?? "N/A",
                    Description = log.Description,
                    Reason = log.Reason,
                    Metadata = log.Metadata
                },
                orderBy: log => log.Timestamp,
                isDescending: true
            );

            return events.Take(pageSize);
        }

        public async Task<PagedResult<ShipmentTimelineEventDto>> SearchAuditTimelineAsync(string? searchTerm, int page = 1, int pageSize = 10)
        {
            page = page > 0 ? page : 1;
            pageSize = pageSize > 0 ? pageSize : 10;

            var term = (searchTerm ?? string.Empty).Trim();

            IEnumerable<ShipmentTimelineEventDto> events;

            if (string.IsNullOrEmpty(term))
            {
                events = await _auditRepo.GetList<ShipmentTimelineEventDto>(
                    filter: log => log.ShipmentId != null,
                    selector: ProjectToDto(),
                    orderBy: log => log.Timestamp,
                    isDescending: true
                );
            }
            else
            {
                Guid? shipmentIdTerm = Guid.TryParse(term, out var parsedId) ? parsedId : (Guid?)null;

                

                events = await _auditRepo.GetList<ShipmentTimelineEventDto>(
                    filter: log => log.ShipmentId != null &&
                    (
                        (shipmentIdTerm != null && log.ShipmentId == shipmentIdTerm) ||

                        log.TrackingNumber.Contains(term) ||

                        log.ShipmentEventType.Contains(term) ||

                        (log.Reason != null && log.Reason.Contains(term)) ||

                        (log.UserId != null && log.UserId.Contains(term)) ||

                        (log.Email != null && log.Email.Contains(term)) ||

                        (log.User != null &&
                         log.User.FirstName != null &&
                         log.User.FirstName.Contains(term)) ||

                        (log.User != null &&
                         log.User.LastName != null &&
                         log.User.LastName.Contains(term)) ||

                        (log.User != null &&
                         log.User.FirstName != null &&
                         log.User.LastName != null &&
                         (log.User.FirstName + " " + log.User.LastName).Contains(term)) ||

                        (log.User != null &&
                         log.User.Email != null &&
                         log.User.Email.Contains(term))
                    ),
                    selector: ProjectToDto(),
                    orderBy: log => log.Timestamp,
                    isDescending: true
                );


                // Priority ordering: user name match first, then email match, then tracking number match.
                events = events
                    .OrderByDescending(e => e.UserDisplayName != null && e.UserDisplayName.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(e => e.Email != null && e.Email.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(e => e.TrackingNumber != null && e.TrackingNumber.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(e => e.Timestamp)
                    .ToList();
            }

            var totalCount = events.Count();
            var items = events.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult<ShipmentTimelineEventDto>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        private static System.Linq.Expressions.Expression<Func<DataAccessLayer.Models.SecurityAuditLog, ShipmentTimelineEventDto>> ProjectToDto()
        {
            return log => new ShipmentTimelineEventDto
            {
                Id = log.Id,
                ShipmentId = log.ShipmentId,
                TrackingNumber = log.TrackingNumber ?? "N/A",
                ShipmentEventType = log.ShipmentEventType ?? log.EventType,
                Severity = log.Severity,
                Timestamp = log.Timestamp,
                UserId = log.UserId,
                Email = log.Email ?? (log.User != null ? log.User.FirstName : null) ?? (log.User != null ? log.User.Email : null) ?? "System",
                //UserDisplayName = (log.User != null ? log.User.FirstName : null) ?? "System",
                UserDisplayName =
    log.User != null
        ? (
            !string.IsNullOrWhiteSpace(
                ((log.User.FirstName ?? "") + " " + (log.User.LastName ?? "")).Trim()
            )
                ? ((log.User.FirstName ?? "") + " " + (log.User.LastName ?? "")).Trim()
                : (log.User.Email ?? "System")
          )
        : "System",
                IpAddress = log.IpAddress ?? "N/A",
                Description = log.Description,
                Reason = log.Reason,
                Metadata = log.Metadata
            };
        }
    }
}

































//using Business.Contracts.Payments;
//using Business.Contracts.Shipment;
//using DataAccessLayer.Contracts;
//using DataAccessLayer.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Business.DTOS;

//namespace Business.Services.Shipment
//{

//    public class ShipmentAuditService : IShipmentAuditService
//    {
//        private readonly IGenericRepository<SecurityAuditLog> _auditRepo;

//        public ShipmentAuditService(IGenericRepository<SecurityAuditLog> auditRepo)
//        {
//            _auditRepo = auditRepo;
//        }

//        /// <summary>
//        /// Retrieve complete timeline of events for a shipment (ordered by timestamp)
//        /// </summary>
//        /// 

//        public async Task<IEnumerable<ShipmentTimelineEventDto>> GetShipmentTimelineAsync(Guid shipmentId)
//        {
//            var events = await _auditRepo.GetList<ShipmentTimelineEventDto>(
//                filter: log => log.ShipmentId == shipmentId,
//                selector: log => new ShipmentTimelineEventDto
//                {
//                    Id = log.Id,
//                    ShipmentId = log.ShipmentId,
//                    TrackingNumber = log.TrackingNumber ?? "N/A",
//                    ShipmentEventType = log.ShipmentEventType ?? log.EventType,
//                    Severity = log.Severity,
//                    Timestamp = log.Timestamp,
//                    UserId = log.UserId,
//                    Email = log.Email ?? "System",
//                    IpAddress = log.IpAddress ?? "N/A",
//                    Description = log.Description,
//                    Reason = log.Reason,
//                    Metadata = log.Metadata
//                },
//                orderBy: log => log.Timestamp,
//                isDescending: false
//            );

//            return events;
//        }
//        //public async Task<IEnumerable<ShipmentTimelineEventDto>> GetShipmentTimelineAsync(Guid shipmentId)
//        //{
//        //    var events = await _auditRepo.GetAllAsync(
//        //        predicate: log => log.ShipmentId == shipmentId,
//        //        orderBy: q => q.OrderBy(log => log.Timestamp)
//        //    );

//        //    return events.Select(log => MapToTimelineEvent(log)).ToList();
//        //}

//        /// <summary>
//        /// Retrieve shipment events created or modified by a specific user
//        /// </summary>
//        /// 
//        public async Task<IEnumerable<ShipmentTimelineEventDto>> GetUserShipmentEventsAsync(string userId, int pageSize = 50)
//        {
//            var events = await _auditRepo.GetList<ShipmentTimelineEventDto>(
//                filter: log => log.UserId == userId && log.ShipmentId != null,
//                selector: log => new ShipmentTimelineEventDto
//                {
//                    Id = log.Id,
//                    ShipmentId = log.ShipmentId,
//                    TrackingNumber = log.TrackingNumber ?? "N/A",
//                    ShipmentEventType = log.ShipmentEventType ?? log.EventType,
//                    Severity = log.Severity,
//                    Timestamp = log.Timestamp,
//                    UserId = log.UserId,
//                    Email = log.Email ?? "System",
//                    IpAddress = log.IpAddress ?? "N/A",
//                    Description = log.Description,
//                    Reason = log.Reason,
//                    Metadata = log.Metadata
//                },
//                orderBy: log => log.Timestamp,
//                isDescending: true
//            );

//            return events.Take(pageSize);
//        }
//        //public async Task<IEnumerable<ShipmentTimelineEventDto>> GetUserShipmentEventsAsync(string userId, int pageSize = 50)
//        //{
//        //    var events = await _auditRepo.GetAllAsync(
//        //        predicate: log => log.UserId == userId && log.ShipmentId != null,
//        //        orderBy: q => q.OrderByDescending(log => log.Timestamp),
//        //        skip: 0,
//        //        take: pageSize
//        //    );

//        //    return events.Select(log => MapToTimelineEvent(log)).ToList();
//        //}

//        private static ShipmentTimelineEventDto MapToTimelineEvent(SecurityAuditLog log)
//        {
//            return new ShipmentTimelineEventDto
//            {
//                Id = log.Id,
//                ShipmentId = log.ShipmentId,
//                TrackingNumber = log.TrackingNumber ?? "N/A",
//                ShipmentEventType = log.ShipmentEventType ?? log.EventType,
//                Severity = log.Severity,
//                Timestamp = log.Timestamp,
//                UserId = log.UserId,
//                Email = log.Email ?? "System",
//                IpAddress = log.IpAddress ?? "N/A",
//                Description = log.Description,
//                Reason = log.Reason,
//                Metadata = log.Metadata
//            };
//        }
//    }


//}
