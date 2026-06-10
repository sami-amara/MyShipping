using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace UI.Services;

public class DashboardService : IDashboardService
{
    private readonly ShippingContext _context;
    private readonly IUserService _userService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ShippingContext context,
        IUserService userService,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
    }

    public async Task<DashboardDto> GetDashboardDataAsync()
    {
        try
        {
            var dashboard = new DashboardDto();

            // Date calculations
            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);
            var last30Days = now.AddDays(-30);

            // ========================================
            // STATISTICS CARDS
            // ========================================

            // Shipments This Month
            dashboard.TotalShipmentsThisMonth = await _context.TbShippments
                .Where(s => s.ShippingDate >= startOfThisMonth)
                .CountAsync();

            var shipmentsLastMonth = await _context.TbShippments
                .Where(s => s.ShippingDate >= startOfLastMonth && s.ShippingDate < startOfThisMonth)
                .CountAsync();

            dashboard.ShipmentsChangePercentage = shipmentsLastMonth > 0
                ? Math.Round(((decimal)(dashboard.TotalShipmentsThisMonth - shipmentsLastMonth) / shipmentsLastMonth) * 100, 1)
                : 100;

            // Revenue This Month
            dashboard.TotalRevenueThisMonth = await _context.TbPaymentTransactions
                .Where(p => p.CreatedDate >= startOfThisMonth && p.TransactionStatus == 1)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0;

            var revenueLastMonth = await _context.TbPaymentTransactions
                .Where(p => p.CreatedDate >= startOfLastMonth && p.CreatedDate < startOfThisMonth && p.TransactionStatus == 1)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0;

            dashboard.RevenueChangePercentage = revenueLastMonth > 0
                ? Math.Round(((dashboard.TotalRevenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100, 1)
                : 100;

            // Users Statistics using UserService
            var allUsers = await _userService.GetAllUsersAsync();
            dashboard.TotalUsers = allUsers.Count();
            dashboard.NewUsersThisMonth = 0; // ApplicationUser doesn't have CreatedAt field

            // Active Shipments - All shipments that are approved, ready, or in transit (not delivered/cancelled/returned)
            dashboard.ActiveShipments = await _context.TbShippments
                .Where(s => s.CurrentState >= 3 && s.CurrentState <= 5) // Approved(3), ReadyForShipping(4), Shipped(5)
                .CountAsync();

            // ========================================
            // CHART DATA - Last 30 Days
            // ========================================

            // Shipments Per Day
            var shipmentsPerDay = await _context.TbShippments
                .Where(s => s.ShippingDate >= last30Days)
                .GroupBy(s => s.ShippingDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            dashboard.ShipmentsPerDay = shipmentsPerDay.ToDictionary(x => x.Date, x => x.Count);

            // Revenue Per Day
            var revenuePerDay = await _context.TbPaymentTransactions
                .Where(p => p.CreatedDate >= last30Days && p.TransactionStatus == 1)
                .GroupBy(p => p.CreatedDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(p => p.TotalAmount) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            dashboard.RevenuePerDay = revenuePerDay.ToDictionary(x => x.Date, x => x.Revenue);

            // Shipments By Status
            var shipmentsByStatus = await _context.TbShippments
                .GroupBy(s => s.CurrentState)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            dashboard.ShipmentsByStatus = shipmentsByStatus
                .GroupBy(x => GetStatusName(x.Status))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Count)
                );

            // Top 5 Destination Cities
            var topCities = await _context.TbShippments
                .Include(s => s.Receiver)
                    .ThenInclude(r => r.City)
                .Where(s => s.Receiver != null && s.Receiver.City != null)
                .GroupBy(s => s.Receiver.City.CityEname ?? "Unknown")
                .Select(g => new { City = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            dashboard.TopDestinationCities = topCities
                .GroupBy(x => x.City)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Count)
                );

            // ========================================
            // RECENT ACTIVITY
            // ========================================

            // Recent Shipments - Top 5 latest with sender/receiver info
            var recentShipmentsData = await _context.TbShippments
                .Include(s => s.Sender)
                .Include(s => s.Receiver)
                .OrderByDescending(s => s.ShippingDate)
                .Take(5)
                .ToListAsync();

            dashboard.RecentShipments = recentShipmentsData.Select(s => new ShippmentDto
            {
                Id = s.Id,
                ShippingDate = s.ShippingDate,
                Status = GetStatusName(s.CurrentState),
                UserSender = s.Sender != null ? new UserSenderDto
                {
                    SenderName = s.Sender.SenderName
                } : null,
                UserReceiver = s.Receiver != null ? new UserReceiverDto
                {
                    ReceiverName = s.Receiver.ReceiverName
                } : null
            }).ToList();

            // Recent Users using UserService
            var allUsersForRecent = await _userService.GetAllUsersAsync();
            dashboard.RecentUsers = allUsersForRecent
                .OrderByDescending(u => u.Email)
                .Take(5)
                .ToList();

            // ========================================
            // ADDITIONAL METRICS
            // ========================================

            dashboard.TotalShipmentsAllTime = await _context.TbShippments.CountAsync();
            dashboard.TotalRevenueAllTime = await _context.TbPaymentTransactions
                .Where(p => p.TransactionStatus == 1)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0;

            dashboard.AverageShipmentValue = dashboard.TotalShipmentsAllTime > 0
                ? dashboard.TotalRevenueAllTime / dashboard.TotalShipmentsAllTime
                : 0;

            dashboard.CompletedShipments = await _context.TbShippments
                .Where(s => s.CurrentState == 6) // Delivered
                .CountAsync();

            dashboard.PendingShipments = await _context.TbShippments
                .Where(s => s.CurrentState == 1 || s.CurrentState == 2) // Created or Updated
                .CountAsync();

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dashboard data");
            return new DashboardDto();
        }
    }

    private string GetStatusName(int statusCode)
    {
        return statusCode switch
        {
            0 => "Deleted",
            1 => "Created",
            2 => "Updated",
            3 => "Approved",
            4 => "Ready For Shipping",
            5 => "Shipped",
            6 => "Delivered",
            7 => "Cancelled",
            8 => "Returned",
            9 => "Refunded",
            _ => "Unknown"
        };
    }
}
