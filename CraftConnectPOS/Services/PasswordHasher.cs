using System;
using System.Security.Cryptography;

namespace CraftConnectPOS.Services
{
    public static class PasswordHasher
    {
        public const int DefaultIterations = 210000;

        public static PasswordHash Create(string password, int iterations = DefaultIterations)
        {
            var salt = new byte[16];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            return new PasswordHash
            {
                Salt = Convert.ToBase64String(salt),
                Hash = Hash(password, salt, iterations),
                Iterations = iterations
            };
        }

        public static bool Verify(string password, string saltText, string expectedHash, int iterations)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(saltText) || string.IsNullOrEmpty(expectedHash))
            {
                return false;
            }

            var salt = Convert.FromBase64String(saltText);
            var actual = Convert.FromBase64String(Hash(password, salt, iterations));
            var expected = Convert.FromBase64String(expectedHash);
            return FixedTimeEquals(actual, expected);
        }

        private static string Hash(string password, byte[] salt, int iterations)
        {
            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(derive.GetBytes(32));
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            var difference = 0;
            for (var i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }
    }

    public class PasswordHash
    {
        public string Hash { get; set; }
        public string Salt { get; set; }
        public int Iterations { get; set; }
    }
}
