using Business.Contracts.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Business.Services.Logging
{
    /// <summary>
    /// Professional PII redactor for GDPR/CCPA/PCI DSS compliance.
    /// Redacts Personally Identifiable Information from logs and application data.
    /// 
    /// ORIGINALLY FROM: WebApi.Filters.SensitiveDataFilter
    /// MOVED TO: Business.Logging.PiiRedactor (shared between all projects)
    /// 
    /// Features:
    /// - Email, phone, credit card, SSN, tokens redaction
    /// - API key and password masking
    /// - Custom pattern registration for domain-specific PII
    /// - Zero external dependencies for portability
    /// 
    /// SOLID Compliance:
    /// - Single Responsibility: Only redacts PII, no logging/filtering logic
    /// - Dependency Inversion: Implements IPiiRedactor contract
    /// - Composition: Can be used in Serilog filters, middleware, or services
    /// </summary>
    public class PiiRedactor : IPiiRedactor
    {
        private const string DefaultRedactionPlaceholder = "[REDACTED]";
        private readonly string _redactionPlaceholder;

        // Default sensitive patterns covering GDPR/PCI DSS compliance
        private readonly Dictionary<string, string> _patterns = new()
        {
            // Contact Information
            { "Email", @"\b[A-Za-z0-9._%\+\-]+@[A-Za-z0-9.\-]+\.[A-Z|a-z]{2,}\b" },
            { "PhoneNumber", @"(\+\d{1,3}[-.\\s]?)?\(?([0-9]{3})\)?[-.\\s]?([0-9]{3})[-.\\s]?([0-9]{4})" },
            { "IPAddress", @"\b(?:192\.168|10\.|172\.(?:1[6-9]|2[0-9]|3[01]))\b[\d\.]+" },

            // Payment Methods (PCI DSS)
            { "CreditCard", @"\b(?:\d{4}[-.\\s]?){3}\d{4}\b" },
            { "CVV", @"CVV[^\d]*(\d{3,4})" },
            { "ExpiryDate", @"(?:expir(?:y|es)|exp|valid_thru)[^\d]*(\d{2}/\d{2})" },

            // Authentication
            { "JWTToken", @"Bearer\s+eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.?[A-Za-z0-9_-]*" },
            { "APIKey", @"(?:api[_-]?key|apikey|secret|api_secret)[:\s=]+([^\s;,]+)" },
            { "Password", @"(?:password|passwd|pwd|pass)[:\s=]+([^\s;,]+)" },
            { "Token", @"(?:token|refresh_token|accesstoken|session_token)[:\s=]+([^\s;,}]+)" },

            // Identity
            { "SSN", @"\b\d{3}-\d{2}-\d{4}\b" },
            { "DriversLicense", @"\b[A-Z0-9]{5,8}-\d{6}\b" },

            // Infrastructure
            { "ConnectionString", @"(?:server|host|password|user\s+id|Data\s+Source)=([^;]+)" },
            { "PrivateKey", @"-----BEGIN\s+(?:RSA\s+)?PRIVATE\s+KEY-----[\s\S]+?-----END\s+(?:RSA\s+)?PRIVATE\s+KEY-----" }
        };

        public PiiRedactor(string? redactionPlaceholder = null)
        {
            _redactionPlaceholder = redactionPlaceholder ?? DefaultRedactionPlaceholder;
        }

        /// <summary>
        /// Redacts all sensitive patterns from input text.
        /// </summary>
        public string Redact(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input ?? string.Empty;

            var result = input;

            // Apply all registered patterns
            foreach (var pattern in _patterns.Values)
            {
                try
                {
                    result = Regex.Replace(
                        result,
                        pattern,
                        _redactionPlaceholder,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
                catch (RegexMatchTimeoutException)
                {
                    // Skip patterns that timeout (prevent ReDoS)
                    continue;
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if input contains any sensitive patterns without redacting.
        /// </summary>
        public bool ContainsSensitiveData(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Check against key sensitive patterns only (performance optimization)
            var criticalPatterns = new[]
            {
                _patterns["Email"],
                _patterns["CreditCard"],
                _patterns["SSN"],
                _patterns["JWTToken"],
                _patterns["APIKey"]
            };

            foreach (var pattern in criticalPatterns)
            {
                try
                {
                    if (Regex.IsMatch(
                        input,
                        pattern,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    {
                        return true;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    continue;
                }
            }

            return false;
        }

        /// <summary>
        /// Registers custom redaction pattern for domain-specific PII.
        /// Allows applications to extend redaction for their own sensitive fields.
        /// </summary>
        public void RegisterPattern(string patternName, string regex)
        {
            if (string.IsNullOrWhiteSpace(patternName))
                throw new ArgumentException("Pattern name cannot be empty", nameof(patternName));

            if (string.IsNullOrWhiteSpace(regex))
                throw new ArgumentException("Regex pattern cannot be empty", nameof(regex));

            try
            {
                // Validate regex compiles
                Regex.Match(string.Empty, regex);

                // Register or override pattern
                _patterns[patternName] = regex;
            }
            catch (RegexParseException ex)
            {
                throw new ArgumentException($"Invalid regex pattern: {ex.Message}", nameof(regex), ex);
            }
        }

        /// <summary>
        /// Gets read-only view of all registered patterns.
        /// Useful for debugging or monitoring what data will be redacted.
        /// </summary>
        public IReadOnlyDictionary<string, string> GetRegisteredPatterns()
        {
            return _patterns.AsReadOnly();
        }
    }
}
