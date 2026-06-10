

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs
{
    [Authorize]  // ✅ Uses default scheme (Bearer)
    public class YourHub : Hub
    {
        private readonly ILogger<YourHub> _logger;

        public YourHub(ILogger<YourHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            _logger.LogInformation("✅ SignalR connected: {User}", userName);

            // ✅ Send user info back
            await Clients.Caller.SendAsync("WhoAmI", new
            {
                User = userName,
                Authenticated = Context.User?.Identity?.IsAuthenticated ?? false
            });

            await base.OnConnectedAsync();
        }

        public async Task WhoAmI()
        {
            var name = Context.User?.Identity?.Name ?? "Anonymous";
            var claims = Context.User?.Claims
                .Select(c => $"{c.Type}: {c.Value}")
                .ToList();

            await Clients.Caller.SendAsync("WhoAmI", new
            {
                User = name,
                Claims = claims,
                Authenticated = Context.User?.Identity?.IsAuthenticated ?? false
            });
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            _logger.LogInformation("❌ SignalR disconnected: {User}", userName);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
