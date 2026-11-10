using Business.DTOS;
using DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Contracts.Shipment
{
    public interface IShipmentAuditService
    {
        Task<IEnumerable<ShipmentTimelineEventDto>> GetShipmentTimelineAsync(Guid shipmentId);
        Task<IEnumerable<ShipmentTimelineEventDto>> GetShipmentTimelineByTrackingAsync(string trackingNumber);
        Task<IEnumerable<ShipmentTimelineEventDto>> GetUserShipmentEventsAsync(string userId, int pageSize = 50);
        Task<IEnumerable<ShipmentTimelineEventDto>> SearchUserShipmentEventsAsync(string userSearch, int pageSize = 50);

        /// <summary>
        /// Searches audit timeline records across tracking number, shipment id, event type, reason,
        /// user name, and user email, prioritized in that order (user name, then email, then tracking number),
        /// and returns a paged result using the shared PagedResult model.
        /// </summary>
        Task<PagedResult<ShipmentTimelineEventDto>> SearchAuditTimelineAsync(string? searchTerm, int page = 1, int pageSize = 10);
    }
}
