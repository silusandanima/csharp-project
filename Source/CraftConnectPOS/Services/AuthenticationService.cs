using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
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
        private const int Iterations = 210000;
        private const int MaximumAcceptedIterations = 1000000;
        private const int MaximumFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromSeconds(30);
        private static readonly object AttemptLock = new object();
        private static readonly Dictionary<string, AttemptState> Attempts =
            new Dictionary<string, AttemptState>(StringComparer.OrdinalIgnoreCase);

        private sealed class AttemptState
        {
            public int FailedCount { get; set; }
            public DateTime LockedUntilUtc { get; set; }
        }

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
                    FROM Users
                    WHERE Username = @Username;";

                int existingUserCount = Convert.ToInt32(
                    DatabaseHelper.ExecuteScalar(
                        duplicateQuery,
                        new[]
                        {
                            new SqliteParameter("@Username", username)
                        }));

                if (existingUserCount > 0)
                {
                    return AuthenticationResult.Failure(
                        "That username is already taken.");
                }

                string passwordHash = HashPassword(password);

                const string insertQuery = @"
                    INSERT INTO Users
                        (Username, Password)
                    VALUES
                        (@Username, @Password);";

                DatabaseHelper.ExecuteNonQuery(
                    insertQuery,
                    new[]
                    {
                        new SqliteParameter("@Username", username),
                        new SqliteParameter("@Password", passwordHash)
                    });

                return AuthenticationResult.Success(username);
            }
            catch (SqliteException ex)
                when (ex.SqliteErrorCode == 19)
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

            string throttleMessage = GetThrottleMessage(username);
            if (!string.IsNullOrEmpty(throttleMessage))
            {
                return AuthenticationResult.Failure(throttleMessage);
            }

            const string query = @"
                SELECT Password
                FROM Users
                WHERE Username = @Username;";

            object result = DatabaseHelper.ExecuteScalar(
                query,
                new[]
                {
                    new SqliteParameter("@Username", username)
                });

            if (result == null || result == DBNull.Value)
            {
                RecordFailedAttempt(username);
                return AuthenticationResult.Failure(
                    "Invalid username or password.");
            }

            string storedPasswordHash = Convert.ToString(result);

            if (!VerifyPassword(password, storedPasswordHash))
            {
                RecordFailedAttempt(username);
                return AuthenticationResult.Failure(
                    "Invalid username or password.");
            }

            ClearFailedAttempts(username);
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

            if (password.Length > 256)
            {
                return "Password cannot exceed 256 characters.";
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

            return "PBKDF2-SHA256$"
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

                bool isLegacySha1 =
                    parts.Length == 4 && parts[0] == "PBKDF2";

                bool isSha256 =
                    parts.Length == 4 && parts[0] == "PBKDF2-SHA256";

                if (!isLegacySha1 && !isSha256)
                {
                    return false;
                }

                int iterations;

                if (!int.TryParse(parts[1], out iterations)
                    || iterations <= 0
                    || iterations > MaximumAcceptedIterations)
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash =
                    Convert.FromBase64String(parts[3]);

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    isSha256
                        ? HashAlgorithmName.SHA256
                        : HashAlgorithmName.SHA1,
                    HashSize);

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
            return Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);
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

        private static string GetThrottleMessage(string username)
        {
            lock (AttemptLock)
            {
                AttemptState state;
                if (!Attempts.TryGetValue(username, out state))
                {
                    return string.Empty;
                }

                if (state.LockedUntilUtc > DateTime.UtcNow)
                {
                    int seconds = Math.Max(
                        1,
                        (int)Math.Ceiling(
                            (state.LockedUntilUtc - DateTime.UtcNow).TotalSeconds));

                    return "Too many failed attempts. Try again in "
                        + seconds
                        + " seconds.";
                }

                if (state.LockedUntilUtc != default(DateTime))
                {
                    Attempts.Remove(username);
                }

                return string.Empty;
            }
        }

        private static void RecordFailedAttempt(string username)
        {
            lock (AttemptLock)
            {
                AttemptState state;
                if (!Attempts.TryGetValue(username, out state))
                {
                    state = new AttemptState();
                    Attempts[username] = state;
                }

                state.FailedCount++;
                if (state.FailedCount >= MaximumFailedAttempts)
                {
                    state.LockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
                }
            }
        }

        private static void ClearFailedAttempts(string username)
        {
            lock (AttemptLock)
            {
                Attempts.Remove(username);
            }
        }
    }
}
