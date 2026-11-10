namespace Business.Contracts.Logging
{
    /// <summary>
    /// Contract for Personally Identifiable Information (PII) and sensitive data redaction.
    /// Ensures GDPR/privacy compliance by redacting sensitive patterns from logs.
    /// 
    /// SOLID Principle: Single Responsibility - This class only redacts PII.
    /// Can be used by Serilog filters, middleware, or any logging component.
    /// </summary>
    public interface IPiiRedactor
    {
        /// <summary>
        /// Redacts sensitive information from text.
        /// Replaces matching patterns with asterisks or [REDACTED] placeholders.
        /// </summary>
        /// <param name="input">Input text that may contain sensitive data</param>
        /// <returns>Redacted text with patterns replaced</returns>
        string Redact(string? input);

        /// <summary>
        /// Registers a custom redaction pattern beyond defaults.
        /// Allows applications to define domain-specific PII patterns.
        /// </summary>
        /// <param name="patternName">Identifier for the pattern (e.g., "CreditCardNumber")</param>
        /// <param name="regex">Regex pattern to match sensitive data</param>
        void RegisterPattern(string patternName, string regex);

        /// <summary>
        /// Checks if a string contains any sensitive patterns without redacting.
        /// Useful for logging decisions or security scanning.
        /// </summary>
        bool ContainsSensitiveData(string? input);
    }
}
