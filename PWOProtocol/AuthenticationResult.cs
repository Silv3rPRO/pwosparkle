namespace PWOProtocol
{
    public enum AuthenticationResult
    {
        InvalidUser = 1,
        InvalidPassword = 2,
        AlreadyLogged = 3,
        InvalidVersion = 4,
        Banned = 5,
        Locked = 6
    }
}
