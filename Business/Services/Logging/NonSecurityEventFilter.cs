using Serilog.Core;
using Serilog.Events;

namespace Business.Services.Logging
{
    /// <summary>
    /// Custom Serilog filter that BLOCKS security-related events from app.txt.
    /// Allows ONLY non-security application events to be written to the app.txt log file.
    /// This is the INVERSE of SecurityEventFilter for proper log separation.
    /// 
    /// BLOCKS all events with:
    /// - EventType containing security markers (LOGIN_SUCCESS, LOGOUT, PASSWORD_RESET, etc.)
    /// - ExceptionCategory == "SecurityException"
    /// - Coming from security-related namespaces
    /// </summary>
    public class NonSecurityEventFilter : ILogEventFilter
    {
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
            var eventType = GetPropertyValue(logEvent, "EventType");
            var exceptionCategory = GetPropertyValue(logEvent, "ExceptionCategory");

            // ✅ BLOCK ALL security-related events at ANY log level

            // ❌ BLOCK: If EventType is a security marker
            if (SecurityEventMarkers.Contains(eventType, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // ❌ BLOCK: If ExceptionCategory is SecurityException
            if (exceptionCategory.Contains("SecurityException", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // ✅ ALLOW: Everything else (framework logs, operational events, non-security exceptions)
            return true;
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

















//using Serilog.Core;
//using Serilog.Events;

//namespace Business.Logging
//{
//    /// <summary>
//    /// Custom Serilog filter that BLOCKS security-related events from app.txt.
//    /// Allows ONLY non-security application events to be written to the app.txt log file.
//    /// This is the INVERSE of SecurityEventFilter for proper log separation.
//    /// </summary>
//    public class NonSecurityEventFilter : ILogEventFilter
//    {



//        public bool IsEnabled(LogEvent logEvent)
//        {
//            if (logEvent == null)
//                return false;

//            // ✅ BLOCK ALL security-related events at ANY log level


//            // ✅ Block ALL Error and above IF it's a security event
//            if (logEvent.Level >= LogEventLevel.Error)
//            {
//                // Check ExceptionCategory - if it's SecurityException, BLOCK it (return false)
//                if (logEvent.Properties.TryGetValue("ExceptionCategory", out var category))
//                {
//                    var categoryValue = category.ToString().Trim('"');
//                    if (categoryValue.Contains("SecurityException", StringComparison.OrdinalIgnoreCase))
//                    {
//                        return false;  // ❌ BLOCK security exceptions (go to security.txt)
//                    }
//                }

//                // ✅ Allow non-security errors
//                return true;
//            }

//            // ✅ Block ALL Warning level IF it's a security event
//            if (logEvent.Level == LogEventLevel.Warning)
//            {
//                // Check ExceptionCategory
//                if (logEvent.Properties.TryGetValue("ExceptionCategory", out var category))
//                {
//                    var categoryValue = category.ToString().Trim('"');
//                    if (categoryValue.Contains("SecurityException", StringComparison.OrdinalIgnoreCase))
//                    {
//                        return false;  // ❌ BLOCK security warnings
//                    }
//                }

//                // Check EventType for security markers
//                if (logEvent.Properties.TryGetValue("EventType", out var eventType))
//                {
//                    var eventValue = eventType.ToString().Trim('"');
//                    if (eventValue.Equals("SECURITY_EXCEPTION", StringComparison.OrdinalIgnoreCase))
//                    {
//                        return false;  // ❌ BLOCK security events
//                    }
//                }

//                // ✅ Allow non-security warnings
//                return true;
//            }

//            // ✅ Allow Information and below for non-security content
//            // (Framework logs, etc.)
//            return true;
//        }
//    }
//}














////using Serilog.Core;
////using Serilog.Events;

////namespace Business.Logging
////{
////    /// <summary>
////    /// Custom Serilog filter that BLOCKS security-related events.
////    /// Allows ONLY non-security application events to be written to the app.txt log file.
////    /// This is the INVERSE of SecurityEventFilter for proper log separation.
////    /// 
////    /// Non-security events that are ALLOWED:
////    /// - Database errors and connection issues
////    /// - Validation errors
////    /// - Business logic exceptions
////    /// - Infrastructure/timeout issues
////    /// - Any events NOT from security namespaces with security markers
////    /// 
////    /// Security events that are BLOCKED (go to security.txt instead):
////    /// - Authentication failures/successes
////    /// - Authorization failures
////    /// - Token-related operations
////    /// - Password reset events
////    /// - Account lockout events
////    /// - Rate limit breaches
////    /// - Suspicious activity alerts
////    /// </summary>
////    public class NonSecurityEventFilter : ILogEventFilter
////    {
////        private static readonly string[] SecurityNamespaces = new[]
////        {
////            "WebApi.Services.SecurityEventLogger",
////            "WebApi.Services.SecurityEventLoggerBase",
////            "Business.Services.SecurityEventLoggerBase",
////            "UI.Services.SecurityEventLogger",
////            "WebApi.Filters.RateLimitBreachLogger",
////            "WebApi.Filters.ExceptionLoggingFilter",
////            "Business.Services.SecurityEventLogger",
////            "Business.Logging.Filters.ExceptionLoggingFilter"
////        };

////        private static readonly string[] SecurityEventMarkers = new[]
////        {
////            // Authentication Events
////            "LOGIN_SUCCESS",
////            "LOGIN_FAILED",
////            "REGISTRATION_SUCCESS",
////            "REGISTRATION_FAILED",

////            // Token Events
////            "TOKEN_ROTATION",
////            "TOKEN_REUSE_DETECTED",
////            "TOKEN_REFRESH_FAILED",
////            "ALL_TOKENS_REVOKED",

////            // Password Events
////            "PASSWORD_RESET",
////            "PASSWORD_RESET_FAILED",
////            "PASSWORD_RESET_REQUESTED",

////            // Account Events
////            "EMAIL_CONFIRMED",
////            "LOGOUT",
////            "ACCOUNT_LOCKOUT",
////            "ACCOUNT_LOCKOUT_ENDED",

////            // Rate Limiting & Suspicious Activity
////            "RATE_LIMIT_BREACH",
////            "SUSPICIOUS_ACTIVITY",
////            "account locked",
////            "locked out",
////            "Too many requests",

////            // Exceptions
////            "Unhandled exception",
////            "CRITICAL EXCEPTION",
////            "ExceptionOccurred"
////        };

////        /// <summary>
////        /// INVERTED logic: Returns TRUE for NON-security events, FALSE for security events.
////        /// This ensures app.txt gets only non-security logs while security.txt gets security logs.
////        /// </summary>
////        public bool IsEnabled(LogEvent logEvent)
////        {
////            if (logEvent == null)
////                return false;

////            // ✅ BLOCK all errors and above (these should go to security.txt if they're security-related)
////            // We'll check if they're security events first
////            if (logEvent.Level >= LogEventLevel.Error)
////            {
////                // Check if this is a security-related exception
////                if (IsSecurityEvent(logEvent))
////                    return false;  // Block security exceptions

////                // Allow non-security errors
////                return true;
////            }

////            // ✅ BLOCK warnings (rate limits, token issues go to security.txt)
////            if (logEvent.Level == LogEventLevel.Warning)
////            {
////                if (IsSecurityEvent(logEvent))
////                    return false;  // Block security warnings

////                // Allow non-security warnings
////                return true;
////            }

////            // ✅ Information level: check if it's security-related
////            if (logEvent.Level == LogEventLevel.Information)
////            {
////                if (IsSecurityEvent(logEvent))
////                    return false;  // Block security info events

////                // Allow non-security info
////                return true;
////            }

////            // ✅ Block Debug and lower levels
////            return false;
////        }

////        /// <summary>
////        /// Helper method to determine if a log event is security-related.
////        /// Returns TRUE if the event should go to security.txt (and thus be blocked here).
////        /// </summary>
////        private bool IsSecurityEvent(LogEvent logEvent)
////        {
////            // Check EventType property first
////            if (logEvent.Properties.TryGetValue("EventType", out var eventType))
////            {
////                var eventValue = eventType.ToString().Trim('"');
////                if (SecurityEventMarkers.Contains(eventValue, StringComparer.OrdinalIgnoreCase))
////                    return true;
////            }

////            // Check SourceContext (namespace)
////            if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
////            {
////                var contextValue = sourceContext.ToString().Trim('"');
////                if (SecurityNamespaces.Any(ns => contextValue.Contains(ns)))
////                    return true;
////            }

////            // Check ExceptionCategory property
////            if (logEvent.Properties.TryGetValue("ExceptionCategory", out var category))
////            {
////                var categoryValue = category.ToString().Trim('"');
////                if (categoryValue.Contains("SecurityException", StringComparison.OrdinalIgnoreCase))
////                    return true;
////            }

////            return false;
////        }
////    }
////}
