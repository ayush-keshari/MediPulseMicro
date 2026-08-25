using System.Collections.Generic;

namespace Shared.Models
{
    /// <summary>
    /// Standardized error response format for all API errors.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// A user-friendly message describing the error.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// An error code that clients can use to handle specific errors programmatically.
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Optional details about the error. For validation errors, this could be a list of field errors.
        /// For other errors, this might contain additional context (in development only).
        /// </summary>
        public object? Details { get; set; }

        /// <summary>
        /// Correlation ID for tracing the request across services and logs.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;
    }
}