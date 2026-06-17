namespace ClientAPI.Models.Exceptions;

public class ExampleException : Exception
{
    public ExampleException(string message) : base(message) { }

    public ExampleException(string message, Exception innerException) : base(message, innerException) { }
}
