namespace Business.Contracts.Exceptions
{
    /// <summary>
    /// Base exception for all shipment-related errors.
    /// Use specific derived types for different error conditions.
    /// </summary>
    public class ShipmentException : Exception
    {
        public ShipmentException(string message) : base(message) { }
        public ShipmentException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Thrown when a shipment cannot be loaded from the database.
    /// This is expected to be caught by the controller and handled gracefully.
    /// </summary>
    public class ShipmentLoadException : ShipmentException
    {
        public ShipmentLoadException(string message) : base(message) { }
        public ShipmentLoadException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Thrown when a requested shipment is not found.
    /// This is expected to be caught by the controller and handled gracefully (404 response).
    /// </summary>
    public class ShipmentNotFoundException : ShipmentException
    {
        public string ShipmentId { get; }

        public ShipmentNotFoundException(string shipmentId) 
            : base($"Shipment with ID '{shipmentId}' not found.")
        {
            ShipmentId = shipmentId;
        }

        public ShipmentNotFoundException(string shipmentId, Exception innerException) 
            : base($"Shipment with ID '{shipmentId}' not found.", innerException)
        {
            ShipmentId = shipmentId;
        }
    }

    /// <summary>
    /// Thrown when a shipment operation fails due to invalid state.
    /// For example, trying to approve a shipment that's not in Created status.
    /// </summary>
    public class ShipmentInvalidStateException : ShipmentException
    {
        public string CurrentState { get; }
        public string RequestedOperation { get; }

        public ShipmentInvalidStateException(string shipmentId, string currentState, string requestedOperation)
            : base($"Cannot {requestedOperation} shipment '{shipmentId}' in state '{currentState}'.")
        {
            CurrentState = currentState;
            RequestedOperation = requestedOperation;
        }
    }

    /// <summary>
    /// Thrown when a shipment operation validation fails.
    /// For example, invalid payment method, missing required fields, etc.
    /// </summary>
    public class ShipmentValidationException : ShipmentException
    {
        public List<string> ValidationErrors { get; }

        public ShipmentValidationException(string message, List<string> errors) 
            : base(message)
        {
            ValidationErrors = errors ?? new List<string>();
        }

        public ShipmentValidationException(List<string> errors) 
            : base($"Shipment validation failed with {errors?.Count ?? 0} error(s).")
        {
            ValidationErrors = errors ?? new List<string>();
        }
    }
}
