using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Business.Services.Logging
{
    /// <summary>
    /// Custom Serilog filter that ONLY allows security-related events to write to security.txt.
    /// Blocks ALL non-security exceptions and framework noise.
    /// 
    /// RULE: Only allow if ExceptionCategory == "SecurityException" OR EventType is a security marker
    /// </summary>
    public class SecurityEventFilter : ILogEventFilter
    {
        private static readonly string[] SecurityNamespaces = new[]
        {
            "WebApi.Services.SecurityEventLogger",
            "WebApi.Services.SecurityEventLoggerBase",
            "Business.Services.SecurityEventLoggerBase",
            "UI.Services.SecurityEventLogger",
            "WebApi.Filters.RateLimitBreachLogger",
            "Business.Logging.Filters.ExceptionLoggingFilter"
        };

        private static readonly string[] SecurityEventMarkers = new[]
        {
            "LOGIN_SUCCESS", "LOGIN_FAILED", "REGISTRATION_SUCCESS", "REGISTRATION_FAILED",
            "TOKEN_ROTATION", "TOKEN_REUSE_DETECTED", "TOKEN_REFRESH_FAILED", "ALL_TOKENS_REVOKED",
            "PASSWORD_RESET", "PASSWORD_RESET_FAILED", "PASSWORD_RESET_REQUESTED",
            "EMAIL_CONFIRMED", "LOGOUT", "ACCOUNT_LOCKOUT", "ACCOUNT_LOCKOUT_ENDED","RATE_LIMIT_RECOVERY",
            "RATE_LIMIT_BREACH", "SUSPICIOUS_ACTIVITY", "account locked", "locked out", "Too many requests",
            "SECURITY_EXCEPTION","TOKEN_ROTATED","PASSWORD_RESET_SUCCESS","ACCOUNT_LOCKED","SHIPMENT_STATUS_CHANGED"
        };

        public bool IsEnabled(LogEvent logEvent)
        {
            if (logEvent == null)
                return false;

            // Extract properties
            var exceptionCategory = GetPropertyValue(logEvent, "ExceptionCategory");
            var eventType = GetPropertyValue(logEvent, "EventType");
            var sourceContext = GetPropertyValue(logEvent, "SourceContext");
            var message = logEvent.MessageTemplate.Text;


            // ✅ CRITICAL RULE: Block ALL errors/warnings that are NOT SecurityException
            if (logEvent.Level >= LogEventLevel.Error)
            {
                // ✅ ALLOW: If explicitly marked as SecurityException
                if (exceptionCategory.Contains("SecurityException", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // ❌ BLOCK: All other error-level events (non-security)
                Debug.WriteLine($"   ❌ BLOCK: Error level but NOT SecurityException category (category={exceptionCategory})");
                return false;
            }

            // ✅ For Warning level: Check if it's a security event
            if (logEvent.Level == LogEventLevel.Warning)
            {
                // ✅ ALLOW: Security event markers
                if (SecurityEventMarkers.Contains(eventType, StringComparer.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"   ✅ ALLOW: Security event marker (EventType={eventType})");
                    return true;
                }

                // ✅ ALLOW: From security namespace
                if (SecurityNamespaces.Any(ns => sourceContext.Contains(ns)))
                {
                    Debug.WriteLine($"   ✅ ALLOW: Security namespace");
                    return true;
                }

                // ✅ ALLOW: SecurityException category
                if (exceptionCategory.Contains("SecurityException", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"   ✅ ALLOW: SecurityException category (Warning)");
                    return true;
                }

                // ❌ BLOCK: Non-security warnings
                Debug.WriteLine($"   ❌ BLOCK: Warning but NOT security-related");
                return false;
            }

            // ✅ For Information level: Only security-specific events
            if (logEvent.Level == LogEventLevel.Information)
            {
                // ✅ ALLOW: Security event markers
                if (SecurityEventMarkers.Contains(eventType, StringComparer.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"   ✅ ALLOW: Security event marker (Info)");
                    return true;
                }

                // ✅ ALLOW: From security namespace
                if (SecurityNamespaces.Any(ns => sourceContext.Contains(ns)))
                {
                    Debug.WriteLine($"   ✅ ALLOW: Security namespace (Info)");
                    return true;
                }

                // ❌ BLOCK: Framework and operational info
                Debug.WriteLine($"   ❌ BLOCK: Info level but NOT security-related");
                return false;
            }

            // ❌ Block Debug and lower levels
            Debug.WriteLine($"   ❌ BLOCK: Below Information level");
            return false;
        }

        /// <summary>
        /// Helper to safely extract property value from LogEvent.
        /// </summary>
        private string GetPropertyValue(LogEvent logEvent, string propertyName)
        {
            if (logEvent.Properties.TryGetValue(propertyName, out var prop))
            {
                return prop.ToString().Trim('"');
            }
            return "NOT_SET";
        }
    }
}






