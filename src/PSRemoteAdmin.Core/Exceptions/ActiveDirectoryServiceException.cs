namespace PSRemoteAdmin.Core.Exceptions;

public class ActiveDirectoryServiceException : Exception
{
    public ActiveDirectoryServiceException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
