using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;


namespace Business.Services.Logging
{
    /// <summary>
    /// Background task queue for persisting security audit logs without blocking requests.
    /// Prevents DbContext disposal issues by queuing work items to be processed in background.
    /// </summary>
    public class SecurityAuditTaskQueue
    {
        private readonly Channel<SecurityAuditWorkItem> _queue;

        public SecurityAuditTaskQueue(int capacity = 100)
        {
            var options = new UnboundedChannelOptions();
            _queue = Channel.CreateUnbounded<SecurityAuditWorkItem>(options);
        }

        public async ValueTask QueueAuditLogAsync(
            string eventType,
            string severity,
            string? userId,
            string? email,
            string? ipAddress,

            //Guid? shipmentId,              // NEW
            //string? trackingNumber,        // NEW
            //string? shipmentEventType,     // NEW
            //string? reason,                // NEW



            string? endpoint,
            string? description,
            string? metadata,
            string? correlationId,

            Guid? shipmentId = null,          // NEW
            string? trackingNumber = null,    // NEW
            string? shipmentEventType = null, // NEW
            string? reason = null

            )
        {
            if (_queue is null)
                throw new InvalidOperationException("Audit task queue has been disposed");

            var item = new SecurityAuditWorkItem
            {
                EventType = eventType,
                Severity = severity,
                UserId = userId,
                Email = email,


                ShipmentId = shipmentId,
                TrackingNumber = trackingNumber,
                ShipmentEventType = shipmentEventType,
                Reason = reason,



                IpAddress = ipAddress,
                Endpoint = endpoint,
                Description = description,
                Metadata = metadata,
                CorrelationId = correlationId
            };

            await _queue.Writer.WriteAsync(item);
        }

        public async IAsyncEnumerable<SecurityAuditWorkItem> DequeueAsync(
            CancellationToken cancellationToken)
        {
            await foreach (var item in _queue.Reader.ReadAllAsync(cancellationToken))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Work item for background task queue.
    /// </summary>
    public class SecurityAuditWorkItem
    {
        public string EventType { get; set; }
        public string Severity { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? IpAddress { get; set; }
        public string? Endpoint { get; set; }
        public string? Description { get; set; }
        public string? Metadata { get; set; }
        public string? CorrelationId { get; set; }

        public Guid? ShipmentId { get; set; }

        public string? TrackingNumber { get; set; }

        public string? ShipmentEventType { get; set; }

        public string? Reason { get; set; }
    }

    /// <summary>
    /// Hosted service that processes background security audit tasks.
    /// Ensures each item gets a fresh DbContext scope.
    /// </summary>
    public class SecurityAuditBackgroundService : BackgroundService
    {
        private readonly SecurityAuditTaskQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SecurityAuditBackgroundService> _logger;

        public SecurityAuditBackgroundService(
            SecurityAuditTaskQueue queue,
            IServiceProvider serviceProvider,
            ILogger<SecurityAuditBackgroundService> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var item in _queue.DequeueAsync(stoppingToken))
            {
             

                try
                {
                    // Create a NEW scope for each work item to get a fresh DbContext
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var auditService = scope.ServiceProvider.GetRequiredService<SecurityAuditService>();

                        await auditService.LogSecurityEventAsync(
                            eventType: item.EventType,
                            severity: item.Severity,
                            userId: item.UserId,
                            email: item.Email,
                            ipAddress: item.IpAddress,
                            endpoint: item.Endpoint,
                            description: item.Description,
                            metadata: item.Metadata,
                            correlationId: item.CorrelationId,
                            shipmentId: item.ShipmentId,
                            trackingNumber: item.TrackingNumber,
                            shipmentEventType: item.ShipmentEventType,
                            reason: item.Reason);


                        // 🔎 Severity check
                        if (item.Severity?.Equals("Critical", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            _logger.LogWarning(
                                "🚨 CRITICAL SECURITY EVENT: {EventType} | Email: {Email} | IP: {IP}",
                                item.EventType, item.Email, item.IpAddress);

                            // For now: simulate a toast notification
                            // (In a real app, this would be sent to the UI via SignalR or another mechanism)
                            _logger.LogInformation("📢 Toast: Critical Security Event - {EventType} for {Email} from {IP}",
                                item.EventType, item.Email, item.IpAddress);
                        }

                        _logger.LogDebug("✅ Background audit task completed: {EventType}", item.EventType);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing background audit task: {EventType}", item.EventType);
                }
            }
        }
    }
}