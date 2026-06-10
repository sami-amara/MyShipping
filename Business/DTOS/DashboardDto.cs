using System;
using System.Collections.Generic;

namespace Business.DTOS
{
    /// <summary>
    /// Dashboard DTO - Contains all statistics and data for the admin dashboard
    /// </summary>
    public class DashboardDto
    {
        // ========================================
        // STATISTICS CARDS
        // ========================================

        /// <summary>
        /// Total number of shipments this month
        /// </summary>
        public int TotalShipmentsThisMonth { get; set; }

        /// <summary>
        /// Percentage change compared to last month
        /// </summary>
        public decimal ShipmentsChangePercentage { get; set; }

        /// <summary>
        /// Total revenue this month (in dollars)
        /// </summary>
        public decimal TotalRevenueThisMonth { get; set; }

        /// <summary>
        /// Percentage change compared to last month
        /// </summary>
        public decimal RevenueChangePercentage { get; set; }

        /// <summary>
        /// Total number of registered users
        /// </summary>
        public int TotalUsers { get; set; }

        /// <summary>
        /// Number of new users this month
        /// </summary>
        public int NewUsersThisMonth { get; set; }

        /// <summary>
        /// Number of active shipments (in transit)
        /// </summary>
        public int ActiveShipments { get; set; }

        // ========================================
        // CHART DATA
        // ========================================

        /// <summary>
        /// Shipments per day for the last 30 days (for line chart)
        /// Key: Date, Value: Shipment count
        /// </summary>
        public Dictionary<DateTime, int> ShipmentsPerDay { get; set; } = new();

        /// <summary>
        /// Revenue per day for the last 30 days (for line chart)
        /// Key: Date, Value: Revenue amount
        /// </summary>
        public Dictionary<DateTime, decimal> RevenuePerDay { get; set; } = new();

        /// <summary>
        /// Shipments by status (for pie chart)
        /// Key: Status name, Value: Count
        /// </summary>
        public Dictionary<string, int> ShipmentsByStatus { get; set; } = new();

        /// <summary>
        /// Top 5 destination cities
        /// Key: City name, Value: Shipment count
        /// </summary>
        public Dictionary<string, int> TopDestinationCities { get; set; } = new();

        // ========================================
        // RECENT ACTIVITY
        // ========================================

        /// <summary>
        /// Latest 5 shipments
        /// </summary>
        public List<ShippmentDto> RecentShipments { get; set; } = new();

        /// <summary>
        /// Latest 5 payments
        /// </summary>
        public List<PaymentTransactionDto> RecentPayments { get; set; } = new();

        /// <summary>
        /// Latest 5 registered users
        /// </summary>
        public List<UserDto> RecentUsers { get; set; } = new();

        // ========================================
        // ADDITIONAL METRICS
        // ========================================

        /// <summary>
        /// Total revenue all time
        /// </summary>
        public decimal TotalRevenueAllTime { get; set; }

        /// <summary>
        /// Total shipments all time
        /// </summary>
        public int TotalShipmentsAllTime { get; set; }

        /// <summary>
        /// Average shipment value
        /// </summary>
        public decimal AverageShipmentValue { get; set; }

        /// <summary>
        /// Number of completed shipments
        /// </summary>
        public int CompletedShipments { get; set; }

        /// <summary>
        /// Number of pending shipments
        /// </summary>
        public int PendingShipments { get; set; }
    }
}
