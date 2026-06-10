using AutoMapper;
using Business.Contracts;
using Business.Contracts.Shipment;
using Business.DTOS;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Contracts;
using DataAccessLayer.Model;
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
    public class ShipmentQueryServic : BaseService<TbShippment, ShippmentDto>, IShipmentQuery
    {
        

        IUserService _userService;
        IGenericRepository<TbShippment> _repo;
        IMapper _mapper;
        /// <summary>
        /// Constructor for ShipmentQueryServic - initializes dependencies for shipment querying.
        /// </summary>
        /// <param name="repo">Generic repository for TbShippment entities</param>
        /// <param name="userService">Service for retrieving current logged-in user information</param>
        /// <param name="userSender">Service for managing user sender data (currently unused in constructor)</param>
        /// <param name="mapper">AutoMapper instance for entity-to-DTO mapping</param>
        public ShipmentQueryServic(IGenericRepository<TbShippment> repo, 
                                 IUserService userService, IUserSender userSender,
                                 IMapper mapper) : base(repo, mapper, userService)
        {
            // Initialize dependencies for shipment queries
            _repo = repo;
            _userService = userService;
            _mapper = mapper;

        }
        /// <summary>
        /// Retrieves all shipments for the currently logged-in user.
        /// Returns a complete list (not paged) of shipments ordered by creation date descending.
        /// </summary>
        /// <returns>List of shipment DTOs belonging to the current user, including sender and receiver details</returns>
        public async Task<List<ShippmentDto>> GetShipments()
        {
            try
            {
                // ✅ Get the current logged-in user's ID to filter their shipments only
                var userId = _userService.GetLoggedInUser();
                // ✅ Query database with filter, projection, ordering, and eager loading
                var shipments = await _repo.GetList(
                    filter: a => a.CreatedBy == userId,  // Only return shipments created by current user
                    selector: a => new ShippmentDto  // Project to DTO to avoid loading full entities
                    {
                        Id = a.Id,
                        ShippingDate = a.ShippingDate,
                        DelivryDate = a.DelivryDate,
                        SenderId = a.SenderId,
                        ReceiverId = a.ReceiverId,
                        ShippingTypeId = a.ShippingTypeId,
                        ShipingPackgingId = a.ShipingPackgingId,
                        Width = a.Width,
                        Height = a.Height,
                        Weight = a.Weight,
                        Length = a.Length,
                        PackageValue = a.PackageValue,
                        ShippingRate = a.ShippingRate,
                        PaymentMethodId = a.PaymentMethodId,
                        PaymentMethodName = a.PaymentMethod != null ? a.PaymentMethod.MethodEname ?? a.PaymentMethod.MethdAname : null,
                        UserSubscriptionId = a.UserSubscriptionId,
                        TrackingNumber = a.TrackingNumber,
                        ReferenceId = a.ReferenceId,
                        CurrentState = a.CurrentState,

                        UserSender = new UserSenderDto
                        {
                            Id = a.Sender.Id,
                            SenderName = a.Sender.SenderName,
                            Email = a.Sender.Email,
                            Phone = a.Sender.Phone
                        },
                        UserReceiver = new UserReceiverDto
                        {
                            Id = a.Receiver.Id,
                            ReceiverName = a.Receiver.ReceiverName,
                            Email = a.Receiver.Email,
                            Phone = a.Receiver.Phone
                        }
                    },
                    orderBy: a => a.CreatedDate,  // Sort by creation date
                    isDescending: true,  // Newest first
                    a => a.Sender, a => a.Receiver, a => a.PaymentMethod  // Eager load related entities to avoid N+1 queries
                );
                return shipments;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting shipments", ex);
            }
        }

        /// <summary>
        /// Retrieves a paged list of shipments with flexible filtering.
        /// Supports filtering by multiple statuses (e.g., Reviewer can see both Created AND Updated shipments).
        /// </summary>
        /// <param name="pageNumber">Page number to retrieve (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="isUserData">If true, filters by current user's shipments only; if false, shows all users' shipments</param>
        /// <param name="statuses">List of statuses to filter by (e.g., [Created, Updated]). If null, shows all statuses.</param>
        /// <param name="searchTerm">Optional search term to filter by sender/receiver name or tracking number</param>
        /// <param name="isPaid">Optional payment status filter (true/false/null for all)</param>
        /// <returns>Paged result of shipment DTOs matching the filter criteria</returns>
        public async Task<PagedResult<ShippmentDto>> GetShipments(int pageNumber, int pageSize, bool isUserData, 
            List<ShipmentStatusEnum>? statuses, 
            string searchTerm = null, bool? isPaid = null)
        {
            try
            {
                // ✅ Convert enum list to integer list for database filtering
                // If statuses is null or empty, statusList will be null (meaning no status filter - show all)
                List<int>? statusList = null;
                if (statuses != null && statuses.Any())
                {
                    // Convert each ShipmentStatusEnum to its integer value
                    // Example: [Created(1), Updated(2)] becomes [1, 2]
                    statusList = statuses.Select(s => (int)s).ToList();
                }


                var userId = _userService.GetLoggedInUser();

                // ✅ Build dynamic filter expression based on parameters
                Expression<Func<TbShippment, bool>> filter;

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    // ✅ No search term - apply basic filters only
                    filter = a => (a.CreatedBy == userId || !isUserData) &&  // Filter by user if isUserData=true
                                  (statusList == null || statusList.Contains(a.CurrentState)) &&  // ✅ Multi-status filter: if statusList=[1,2], shows status 1 OR 2
                                  (!a.IsDeleted || statusList == null) &&  // Exclude soft-deleted records (unless showing all)
                                  (a.IsPaid == isPaid || isPaid == null);  // Filter by payment status if provided
                }
                else
                {
                    // ✅ With search term - add name/tracking number search to the filter
                    var lowerSearch = searchTerm.ToLower();
                    filter = a => (a.CreatedBy == userId || !isUserData) &&  // Filter by user if isUserData=true
                                  (statusList == null || statusList.Contains(a.CurrentState)) &&  // ✅ Multi-status filter using Contains (OR logic)
                                  (!a.IsDeleted || statusList == null) &&  // Exclude soft-deleted records
                                  (a.IsPaid == isPaid || isPaid == null) &&  // Filter by payment status
                                  (a.Sender.SenderName.ToLower().Contains(lowerSearch) ||  // Search in sender name
                                   a.Receiver.ReceiverName.ToLower().Contains(lowerSearch) ||  // Search in receiver name
                                   (a.TrackingNumber.HasValue && a.TrackingNumber.Value.ToString().Contains(searchTerm)));  // Search in tracking number
                }

                var result = await _repo.GetPagedList(
                    filter: filter,
                    selector: a => new ShippmentDto
                    {
                        Id = a.Id,
                        ShippingDate = a.ShippingDate,
                        DelivryDate = a.DelivryDate,
                        SenderId = a.SenderId,
                        ReceiverId = a.ReceiverId,
                        ShippingTypeId = a.ShippingTypeId,
                        ShipingPackgingId = a.ShipingPackgingId,
                        Width = a.Width,
                        Height = a.Height,
                        Weight = a.Weight,
                        Length = a.Length,
                        PackageValue = a.PackageValue,
                        ShippingRate = a.ShippingRate,
                        PaymentMethodId = a.PaymentMethodId,
                        PaymentMethodName = a.PaymentMethod != null ? a.PaymentMethod.MethodEname ?? a.PaymentMethod.MethdAname : null,
                        UserSubscriptionId = a.UserSubscriptionId,
                        TrackingNumber = a.TrackingNumber,
                        ReferenceId = a.ReferenceId,
                        CurrentState = a.CurrentState,
                        IsPaid = a.IsPaid,

                        UserSender = new UserSenderDto
                        {
                            Id = a.Sender.Id,
                            SenderName = a.Sender.SenderName,
                            Email = a.Sender.Email,
                            Phone = a.Sender.Phone
                        },
                        UserReceiver = new UserReceiverDto
                        {
                            Id = a.Receiver.Id,
                            ReceiverName = a.Receiver.ReceiverName,
                            Email = a.Receiver.Email,
                            Phone = a.Receiver.Phone
                        }
                    },
                    orderBy: a => a.CreatedDate,
                    isDescending: true,
                    page: pageNumber,
                    pageSize: pageSize,
                    a => a.Sender, a => a.Receiver, a => a.PaymentMethod
                );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting shipments", ex);
            }
        }
        /// <summary>
        /// Retrieves a paged list of all shipments (not filtered by user) with optional search.
        /// Used by admin views to see all shipments across all users.
        /// </summary>
        /// <param name="pageNumber">Page number to retrieve (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="searchTerm">Optional search term to filter by sender/receiver name or tracking number</param>
        /// <returns>Paged result of all shipment DTOs (excluding soft-deleted records)</returns>
        public async Task<PagedResult<ShippmentDto>> GetAllShipments(int pageNumber, int pageSize, string searchTerm = null)
        {
            try
            {
                // ✅ Build filter based on whether search term is provided
                Expression<Func<TbShippment, bool>> filter;
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    // No search term - just exclude soft-deleted records
                    filter = a => !a.IsDeleted;
                }
                else
                {
                    // With search term - filter by sender/receiver name or tracking number
                    var term = searchTerm.Trim().ToLower();
                    filter = a => !a.IsDeleted && (  // Exclude soft-deleted AND match search criteria
                                (a.Sender != null && a.Sender.SenderName != null && a.Sender.SenderName.ToLower().Contains(term)) ||  // Search in sender name
                                (a.Receiver != null && a.Receiver.ReceiverName != null && a.Receiver.ReceiverName.ToLower().Contains(term)) ||  // Search in receiver name
                                (a.TrackingNumber != null && a.TrackingNumber.ToString().Contains(searchTerm))  // Search in tracking number
                            );
                }

                // ✅ Execute paged query with projection and ordering
                var result = await _repo.GetPagedList(
                    filter: filter,  // Apply the search/soft-delete filter
                    selector: a => new ShippmentDto
                    {
                        Id = a.Id,
                        ShippingDate = a.ShippingDate,
                        DelivryDate = a.DelivryDate,
                        SenderId = a.SenderId,
                        ReceiverId = a.ReceiverId,
                        ShippingTypeId = a.ShippingTypeId,
                        ShipingPackgingId = a.ShipingPackgingId,
                        Width = a.Width,
                        Height = a.Height,
                        Weight = a.Weight,
                        Length = a.Length,
                        PackageValue = a.PackageValue,
                        ShippingRate = a.ShippingRate,
                        PaymentMethodId = a.PaymentMethodId,
                        PaymentMethodName = a.PaymentMethod != null ? a.PaymentMethod.MethodEname ?? a.PaymentMethod.MethdAname : null,
                        UserSubscriptionId = a.UserSubscriptionId,
                        TrackingNumber = a.TrackingNumber,
                        ReferenceId = a.ReferenceId,
                        CurrentState = a.CurrentState,

                        UserSender = new UserSenderDto
                        {
                            Id = a.Sender.Id,
                            SenderName = a.Sender.SenderName,
                            Email = a.Sender.Email,
                            Phone = a.Sender.Phone
                        },
                        UserReceiver = new UserReceiverDto
                        {
                            Id = a.Receiver.Id,
                            ReceiverName = a.Receiver.ReceiverName,
                            Email = a.Receiver.Email,
                            Phone = a.Receiver.Phone
                        }
                    },
                    orderBy: a => a.CreatedDate,  // Sort by creation date
                    isDescending: true,  // Newest first
                    page: pageNumber,  // Current page number
                    pageSize: pageSize,  // Items per page
                    a => a.Sender, a => a.Receiver, a => a.PaymentMethod  // Eager load navigation properties
                );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting shipments", ex);
            }
        }


        /// <summary>
        /// Retrieves a paged list of all shipments without search filtering.
        /// Simpler version of GetAllShipments - only excludes soft-deleted records.
        /// </summary>
        /// <param name="pageNumber">Page number to retrieve (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paged result of all shipment DTOs (excluding soft-deleted records)</returns>
        public async Task<PagedResult<ShippmentDto>> GetAllShipments(int pageNumber, int pageSize)
        {
            try
            {
                // ✅ Query all non-deleted shipments with paging
                var result = await _repo.GetPagedList(
                   filter: a => !a.IsDeleted,  // Exclude soft-deleted records only
                    selector: a => new ShippmentDto
                    {
                        Id = a.Id,
                        ShippingDate = a.ShippingDate,
                        DelivryDate = a.DelivryDate,
                        SenderId = a.SenderId,
                        ReceiverId = a.ReceiverId,
                        ShippingTypeId = a.ShippingTypeId,
                        ShipingPackgingId = a.ShipingPackgingId,
                        Width = a.Width,
                        Height = a.Height,
                        Weight = a.Weight,
                        Length = a.Length,
                        PackageValue = a.PackageValue,
                        ShippingRate = a.ShippingRate,
                        PaymentMethodId = a.PaymentMethodId,
                        PaymentMethodName = a.PaymentMethod != null ? a.PaymentMethod.MethodEname ?? a.PaymentMethod.MethdAname : null,
                        UserSubscriptionId = a.UserSubscriptionId,
                        TrackingNumber = a.TrackingNumber,
                        ReferenceId = a.ReferenceId,
                        CurrentState = a.CurrentState,

                        UserSender = new UserSenderDto
                        {
                            Id = a.Sender.Id,
                            SenderName = a.Sender.SenderName,
                            Email = a.Sender.Email,
                            Phone = a.Sender.Phone
                        },
                        UserReceiver = new UserReceiverDto
                        {
                            Id = a.Receiver.Id,
                            ReceiverName = a.Receiver.ReceiverName,
                            Email = a.Receiver.Email,
                            Phone = a.Receiver.Phone
                        }
                    },
                    orderBy: a => a.CreatedDate,  // Sort by creation date
                    isDescending: true,  // Newest first
                    page: pageNumber,  // Current page number
                    pageSize: pageSize,  // Items per page
                    a => a.Sender, a => a.Receiver, a => a.PaymentMethod  // Eager load navigation properties
                );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting shipments", ex);
            }
        }

        /// <summary>
        /// Retrieves a single shipment by its unique identifier.
        /// Includes full sender and receiver details with city and country information.
        /// </summary>
        /// <param name="Id">Unique identifier (GUID) of the shipment to retrieve</param>
        /// <returns>Shipment DTO with complete sender/receiver details, or null if not found</returns>
        public async Task<ShippmentDto> GetShipment(Guid Id)
        {
            try
            {
                // ✅ Query shipment by ID (note: not filtered by user - allows admin access)
                var shipment = await _repo.GetList(
                    filter: a => a.Id == Id,  // Find shipment by unique ID
                    selector: a => new ShippmentDto
                    {
                        Id = a.Id,
                        ShippingDate = a.ShippingDate,
                        DelivryDate = a.DelivryDate,
                        SenderId = a.SenderId,
                        ReceiverId = a.ReceiverId,
                        ShippingTypeId = a.ShippingTypeId,
                        ShipingPackgingId = a.ShipingPackgingId,
                        Width = a.Width,
                        Height = a.Height,
                        Weight = a.Weight,
                        Length = a.Length,
                        PackageValue = a.PackageValue,
                        ShippingRate = a.ShippingRate,
                        PaymentMethodId = a.PaymentMethodId,
                        PaymentMethodName = a.PaymentMethod != null ? a.PaymentMethod.MethodEname ?? a.PaymentMethod.MethdAname : null,
                        UserSubscriptionId = a.UserSubscriptionId,
                        TrackingNumber = a.TrackingNumber,
                        ReferenceId = a.ReferenceId,
                        CurrentState = a.CurrentState,

                        UserSender = new UserSenderDto
                        {
                            Id = a.Sender.Id,
                            SenderName = a.Sender.SenderName,
                            Email = a.Sender.Email,
                            Phone = a.Sender.Phone,
                            Address = a.Sender.Address,
                            Contact = a.Sender.Contact,
                            PostalCode = a.Sender.PostalCode,
                            OtherAddress = a.Sender.OtherAddress,
                            CityId = a.Sender.CityId,
                            CountryId = a.Sender.City.CountryId
                        },
                        UserReceiver = new UserReceiverDto
                        {
                            Id = a.Receiver.Id,
                            ReceiverName = a.Receiver.ReceiverName,
                            Email = a.Receiver.Email,
                            Phone = a.Receiver.Phone,
                            Address = a.Receiver.Address,
                            Contact = a.Receiver.Contact,
                            PostalCode = a.Receiver.PostalCode,
                            OtherAddress = a.Receiver.OtherAddress,
                            CityId = a.Receiver.CityId,
                            CountryId = a.Receiver.City.CountryId
                        }
                    },
                      orderBy: a => a.CreatedDate,  // Sort by creation date
                      isDescending: true,  // Newest first
                      // ✅ Eager load deeply nested navigation properties to get full address details
                      a => a.Sender,  // Load sender
                      a => a.Sender.City,  // Load sender's city
                      a => a.Sender.City.Country,  // Load sender's country
                      a => a.Receiver,  // Load receiver
                      a => a.Receiver.City,  // Load receiver's city
                      a => a.Receiver.City.Country,  // Load receiver's country
                      a => a.PaymentMethod  // Load payment method
                            );
                return shipment.FirstOrDefault();  // Return first match or null
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting shipments", ex);
            }
        }
        /// <summary>
        /// Retrieves a single shipment by ID with complete related entity information.
        /// Enhanced version that includes packaging, shipping type, carrier, and full address details.
        /// </summary>
        /// <param name="id">Unique identifier (GUID) of the shipment</param>
        /// <returns>Shipment DTO with all related entity names and details, or null if not found or ID is empty</returns>
        public async Task<ShippmentDto?> GetByIdAsync(Guid id)
        {
            // ✅ Validate input - return null for empty GUID
            if (id == Guid.Empty) return null;

            try
            {
                // ✅ Query shipment with extensive eager loading for complete data
                var list = await _repo.GetList(
                    filter: a => a.Id == id,  // Find shipment by unique ID
                    selector: a => new ShippmentDto
                    {
                        Id = a.Id,
                        ShippingDate = a.ShippingDate,
                        DelivryDate = a.DelivryDate,
                        SenderId = a.SenderId,
                        ReceiverId = a.ReceiverId,
                        CarrierId = a.CarrierId,
                        ShippingTypeId = a.ShippingTypeId,
                        ShipingPackgingId = a.ShipingPackgingId,
                        Width = a.Width,
                        Height = a.Height,
                        Weight = a.Weight,
                        Length = a.Length,
                        PackageValue = a.PackageValue,
                        ShippingRate = a.ShippingRate,
                        PaymentMethodId = a.PaymentMethodId,
                        UserSubscriptionId = a.UserSubscriptionId,
                        TrackingNumber = a.TrackingNumber,
                        ReferenceId = a.ReferenceId,
                        CurrentState = a.CurrentState,
                        IsPaid = a.IsPaid,

                        // ✅ Include related entity names for display (avoids additional queries)
                        PackagingName = a.ShipingPackging != null ? a.ShipingPackging.TbShipingPackginEname : null,  // Packaging type name
                        ShippingTypeName = a.ShippingType != null ? a.ShippingType.ShippingTypeEname : null,  // Shipping type name (e.g., Express, Standard)
                        CarrierName = a.Carrier != null ? a.Carrier.CarrierName : null,  // Carrier company name
                        PaymentMethodName = a.PaymentMethod != null ? a.PaymentMethod.MethodEname ?? a.PaymentMethod.MethdAname : null,  // Payment method name

                        UserSender = a.Sender == null ? null : new UserSenderDto
                        {
                            Id = a.Sender.Id,
                            SenderName = a.Sender.SenderName,
                            Email = a.Sender.Email,
                            Phone = a.Sender.Phone,
                            CityId = a.Sender.CityId,
                            Address = a.Sender.Address,
                            Contact = a.Sender.Contact,
                            OtherAddress = a.Sender.OtherAddress,
                            PostalCode = a.Sender.PostalCode,
                            IsDefault = a.Sender.IsDefault,
                            CityName = a.Sender.City != null ? a.Sender.City.CityEname ?? a.Sender.City.CityAname : null,
                            CountryName = a.Sender.City != null && a.Sender.City.Country != null ? a.Sender.City.Country.CountryEname ?? a.Sender.City.Country.CountryAname : null,
                            CountryId = a.Sender.City != null ? a.Sender.City.CountryId : Guid.Empty
                        },
                        UserReceiver = a.Receiver == null ? null : new UserReceiverDto
                        {
                            Id = a.Receiver.Id,
                            ReceiverName = a.Receiver.ReceiverName,
                            Email = a.Receiver.Email,
                            Phone = a.Receiver.Phone,
                            CityId = a.Receiver.CityId,
                            Address = a.Receiver.Address,
                            Contact = a.Receiver.Contact,
                            OtherAddress = a.Receiver.OtherAddress,
                            PostalCode = a.Receiver.PostalCode,
                            IsDefault = a.Receiver.IsDefault,
                            CityName = a.Receiver.City != null ? a.Receiver.City.CityEname ?? a.Receiver.City.CityAname : null,
                            CountryName = a.Receiver.City != null && a.Receiver.City.Country != null ? a.Receiver.City.Country.CountryEname ?? a.Receiver.City.Country.CountryAname : null,
                            CountryId = a.Receiver.City != null ? a.Receiver.City.CountryId : Guid.Empty
                        }
                    },
                    orderBy: null,  // No sorting needed for single-item query
                    isDescending: false,
                    // ✅ Include navigation chains - EF Core loads all related entities in one query (avoids N+1 problem)
                    a => a.Sender, a => a.Sender.City, a => a.Sender.City.Country,  // Sender's full address hierarchy
                    a => a.Receiver, a => a.Receiver.City, a => a.Receiver.City.Country,  // Receiver's full address hierarchy
                    a => a.ShipingPackging, a => a.ShippingType, a => a.Carrier, a => a.PaymentMethod  // Shipment configuration entities
                );
                return list?.FirstOrDefault();  // Return first match or null (should only be one result)
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting shipment by id", ex);
            }
        }

    }
}
