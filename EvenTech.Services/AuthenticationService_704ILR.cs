namespace EvenTech.Services
{
    // Comparacion de hashes en tiempo constante para evitar timing attacks.
    public static class AuthenticationService_704ILR
    {
        public static bool CompareHashedPasswords_704ILR(string hashedInput_704ILR, string storedHash_704ILR)
        {
            if (hashedInput_704ILR == null || storedHash_704ILR == null) return false;
            return SlowEquals_704ILR(hashedInput_704ILR, storedHash_704ILR);
        }

        private static bool SlowEquals_704ILR(string a_704ILR, string b_704ILR)
        {
            int diff_704ILR = a_704ILR.Length ^ b_704ILR.Length;
            for (int i_704ILR = 0; i_704ILR < a_704ILR.Length && i_704ILR < b_704ILR.Length; i_704ILR++)
            {
                diff_704ILR |= a_704ILR[i_704ILR] ^ b_704ILR[i_704ILR];
            }
            return diff_704ILR == 0;
        }
    }
}
