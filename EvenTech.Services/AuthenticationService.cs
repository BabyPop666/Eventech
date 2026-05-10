namespace EvenTech.Services
{
    // Comparacion de hashes en tiempo constante para evitar timing attacks.
    public static class AuthenticationService
    {
        public static bool CompareHashedPasswords(string hashedInput, string storedHash)
        {
            if (hashedInput == null || storedHash == null) return false;
            return SlowEquals(hashedInput, storedHash);
        }

        private static bool SlowEquals(string a, string b)
        {
            int diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
