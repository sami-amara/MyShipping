namespace Business.Contracts.Logging
{
    /// <summary>
    /// Contract for rate limit breach logging and suspicious activity detection.
    /// Logs rate limit events as security incidents for audit and threat detection.
    /// 
    /// SOLID Principle: Interface Segregation - Focused contract for rate limiting concerns.
    /// Separate from general security logging to allow specialized rate limit handling.
    /// </summary>
    public interface IRateLimitEventLogger
    {
        ///// <summary>
        ///// Logs when a client exceeds rate limit threshold.
        ///// Records IP, endpoint, request count, and window details.
        ///// </summary>
        ///// <param name="clientIdentifier">IP address, user identity, or API key being rate limited</param>
        ///// <param name="endpoint">API endpoint or path being accessed</param>
        ///// <param name="requestsCount">Number of requests exceeded limit by</param>
        ///// <param name="limitWindow">Time window for the limit (e.g., "5 minutes")</param>
        //Task LogRateLimitBreachAsync(string clientIdentifier, string endpoint, int requestsCount, string limitWindow);


        /// <summary>
        /// Logs when a client exceeds rate limit threshold.
        /// Records IP, endpoint, request count, and window details.
        /// Captures email and correlation ID for audit trail.
        /// </summary>
        /// <param name="clientIdentifier">IP address being rate limited</param>
        /// <param name="endpoint">API endpoint or path being accessed</param>
        /// <param name="requestsCount">Number of requests exceeded limit by</param>
        /// <param name="limitWindow">Time window for the limit (e.g. "5 minutes")</param>
        /// <param name="email">Optional: Email from login form for unauthenticated users (best practice: don't use UserId)</param>
        /// <param name="correlationId">Optional: Request correlation ID for tracing</param>
        Task LogRateLimitBreachAsync(
            string clientIdentifier,
            string endpoint,
            int requestsCount,
            string limitWindow,
            string? email = null,
            string? correlationId = null);

        /// <summary>
        /// Logs suspicious patterns indicating coordinated attacks.
        /// E.g., rapid rate limit breaches across multiple endpoints from same IP.
        /// </summary>
        /// <param name="clientIdentifier">IP address or user showing suspicious behavior</param>
        /// <param name="suspiciousPattern">Description of detected pattern</param>
        /// <param name="affectedEndpoints">Endpoints targeted by suspicious requests</param>
        Task LogSuspiciousActivityAsync(string clientIdentifier, string suspiciousPattern, string[] affectedEndpoints);

        /// <summary>
        /// Logs when a previously rate-limited client recovers and becomes normal again.
        /// Signals the end of a security incident.
        /// </summary>
        /// <param name="clientIdentifier">IP address or user that recovered</param>
        /// <param name="recoveryTime">When rate limiting restriction was lifted</param>
        Task LogRateLimitRecoveryAsync(string clientIdentifier, DateTime recoveryTime);
    }
}
