public class AuthResult
{
    public bool IsSuccess;
    public string Error;

    public static AuthResult Success()
    {
        return new AuthResult
        {
            IsSuccess = true
        };
    }

    public static AuthResult Fail(string error)
    {
        return new AuthResult
        {
            IsSuccess = false,
            Error = error
        };
    }
}
