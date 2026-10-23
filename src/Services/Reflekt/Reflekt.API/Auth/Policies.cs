namespace Reflekt.API.Auth;

public static class Policies
{
    public const string SignedInUser = "SignedInUser";
    public const string Guest = "Guest";
    public const string SignedInUserOrGuest = "SignedInUserOrGuest";
}
