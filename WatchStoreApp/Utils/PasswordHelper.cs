namespace WatchStoreApp.Utils
{
    public static class PasswordHelper
    {
        public static bool IsBcryptHash(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (password.Length != 60)
                return false;

            if (!password.StartsWith("$2a$") && 
                !password.StartsWith("$2b$") && 
                !password.StartsWith("$2x$") && 
                !password.StartsWith("$2y$"))
                return false;

            return true;
        }

        public static bool VerifyPassword(string plainPassword, string bcryptHash)
        {
            try
            {
                if (!IsBcryptHash(bcryptHash))
                    return false;

                return BCrypt.Net.BCrypt.Verify(plainPassword, bcryptHash);
            }
            catch
            {
                return false;
            }
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}