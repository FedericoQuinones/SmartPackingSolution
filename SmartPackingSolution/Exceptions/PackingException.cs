namespace SmartPackingSolution.Exceptions;

/// <summary>
/// Exception thrown when a packing operation fails.
/// </summary>
public class PackingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackingException"/> class.
    /// </summary>
    public PackingException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackingException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public PackingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackingException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PackingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}