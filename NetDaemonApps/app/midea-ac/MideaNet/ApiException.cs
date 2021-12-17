namespace MideaAcIntegration.MideaNet;

public class ApiException:Exception 
{
    public string ErrorCode { get; }

    public ApiException(string errorCode, string message):base(message)
    {
        ErrorCode = errorCode;
    }
}