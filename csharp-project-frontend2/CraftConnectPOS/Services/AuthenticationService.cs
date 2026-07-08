using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace CraftConnectPOS.Data
{
    // Encapsulated result object:
    // other code can read the outcome, but cannot freely change it.
    public sealed class AuthenticationResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public string Username { get; }

        private AuthenticationResult(
            bool isSuccess,
            string message,
            string username)
        {
            IsSuccess = isSuccess;
            Message = message;
            Username = username;
        }

        public static AuthenticationResult Success(string username)
        {
            return new AuthenticationResult(
                true,
                "Authentication successful.",
                username);
        }

        public static AuthenticationResult Failure(string message)
        {
            return new AuthenticationResult(
                false,
                message,
                string.Empty);
        }
    }

    // Encapsulated current-user session.
    // No other code can directly assign UserSession.Username.
    public static class UserSession
    {
        private static string _username = string.Empty;

        public static bool IsSignedIn
        {
            get { return !string.IsNullOrWhiteSpace(_username); }
        }

        public static string Username
        {
            get { return _username; }
        }

        public static void Start(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException(
                    "Username is required.",
                    nameof(username));
            }

            _username = username.Trim();
        }

        public static void Clear()
        {
            _username = string.Empty;
        }
    }

    public sealed class AuthenticationService
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public AuthenticationResult Register(
            string username,
            string password)
        {
            username = (username ?? string.Empty).Trim();

            string validationError =
                ValidateRegistration(username, password);

            if (!string.IsNullOrEmpty(validationError))
            {
                return AuthenticationResult.Failure(validationError);
            }

            try
            {
                const string duplicateQuery = @"
                    SELECT COUNT(*)
                    FROM dbo.Users
                    WHERE Username = @Username;";

                int existingUserCount = Convert.ToInt32(
                    DatabaseHelper.ExecuteScalar(
                        duplicateQuery,
                        new[]
                        {
                            new SqlParameter("@Username", username)
                        }));

                if (existingUserCount > 0)
                {
                    return AuthenticationResult.Failure(
                        "That username is already taken.");
                }

                string passwordHash = HashPassword(password);

                const string insertQuery = @"
                    INSERT INTO dbo.Users
                        (Username, Password)
                    VALUES
                        (@Username, @Password);";

                DatabaseHelper.ExecuteNonQuery(
                    insertQuery,
                    new[]
                    {
                        new SqlParameter("@Username", username),
                        new SqlParameter("@Password", passwordHash)
                    });

                return AuthenticationResult.Success(username);
            }
            catch (SqlException ex)
                when (ex.Number == 2601 || ex.Number == 2627)
            {
                return AuthenticationResult.Failure(
                    "That username is already taken.");
            }
        }

        public AuthenticationResult Login(
            string username,
            string password)
        {
            username = (username ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(password))
            {
                return AuthenticationResult.Failure(
                    "Username and password are required.");
            }

            const string query = @"
                SELECT Password
                FROM dbo.Users
                WHERE Username = @Username;";

            object result = DatabaseHelper.ExecuteScalar(
                query,
                new[]
                {
                    new SqlParameter("@Username", username)
                });

            if (result == null || result == DBNull.Value)
            {
                return AuthenticationResult.Failure(
                    "Invalid username or password.");
            }

            string storedPasswordHash = Convert.ToString(result);

            if (!VerifyPassword(password, storedPasswordHash))
            {
                return AuthenticationResult.Failure(
                    "Invalid username or password.");
            }

            return AuthenticationResult.Success(username);
        }

        private static string ValidateRegistration(
            string username,
            string password)
        {
            if (username.Length < 3 || username.Length > 50)
            {
                return "Username must contain 3 to 50 characters.";
            }

            foreach (char character in username)
            {
                bool isAllowed =
                    char.IsLetterOrDigit(character)
                    || character == '_'
                    || character == '-'
                    || character == '.';

                if (!isAllowed)
                {
                    return "Username can contain letters, numbers, dots, hyphens, and underscores only.";
                }
            }

            if (string.IsNullOrWhiteSpace(password)
                || password.Length < 8)
            {
                return "Password must contain at least 8 characters.";
            }

            return string.Empty;
        }

        private static string HashPassword(string password)
        {
            byte[] salt = new byte[SaltSize];

            using (RandomNumberGenerator random =
                   RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            byte[] hash = GetPbkdf2Hash(password, salt);

            return "PBKDF2$"
                + Iterations
                + "$"
                + Convert.ToBase64String(salt)
                + "$"
                + Convert.ToBase64String(hash);
        }

        private static bool VerifyPassword(
            string password,
            string storedPasswordHash)
        {
            try
            {
                string[] parts = storedPasswordHash.Split('$');

                if (parts.Length != 4 || parts[0] != "PBKDF2")
                {
                    return false;
                }

                int iterations;

                if (!int.TryParse(parts[1], out iterations)
                    || iterations <= 0)
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash =
                    Convert.FromBase64String(parts[3]);

                byte[] actualHash;

                using (var deriveBytes = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations))
                {
                    actualHash = deriveBytes.GetBytes(HashSize);
                }

                return FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }

        private static byte[] GetPbkdf2Hash(
            string password,
            byte[] salt)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations))
            {
                return deriveBytes.GetBytes(HashSize);
            }
        }

        private static bool FixedTimeEquals(
            byte[] first,
            byte[] second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            int difference = first.Length ^ second.Length;
            int length = Math.Min(first.Length, second.Length);

            for (int index = 0; index < length; index++)
            {
                difference |= first[index] ^ second[index];
            }

            return difference == 0;
        }
    }
}