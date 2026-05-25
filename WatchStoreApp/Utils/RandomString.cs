using System.Security.Cryptography;

namespace WatchStoreApp.Utils;

public class RandomString
{
    public static string GenerateSecureString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        return RandomNumberGenerator.GetString(chars, length);
    }
}