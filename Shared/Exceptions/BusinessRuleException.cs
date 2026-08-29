using System;

namespace Shared.Exceptions
{
    /// <summary>
    /// Exception thrown when a business rule validation fails.
    /// This is used to signal errors that should be returned to the client as a 409 Conflict.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message)
        {
        }
    }
}
