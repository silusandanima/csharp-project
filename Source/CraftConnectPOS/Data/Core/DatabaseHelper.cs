using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace CraftConnectPOS.Data
{
    public static class DatabaseHelper
    {
        private static string _connectionString;
        private static readonly object _lockObject = new object();
        private static bool _isInitialized = false;

        public static void Initialize(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be empty");

            lock (_lockObject)
            {
                if (_isInitialized)
                    throw new InvalidOperationException("DatabaseHelper is already initialized");
                _connectionString = connectionString;
                _isInitialized = true;
            }
        }

        public static string GetConnectionString()
        {
            EnsureInitialized();
            return _connectionString;
        }

        public static void TestConnection()
        {
            using (var connection = CreateConnection())
            {
                connection.Open();
            }
        }

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Database not initialized.");
        }

        private static SqliteConnection CreateConnection()
        {
            EnsureInitialized();
            return new SqliteConnection(_connectionString);
        }

        public static SqliteConnection OpenConnection()
        {
            SqliteConnection connection = CreateConnection();
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
                command.ExecuteNonQuery();
            }

            return connection;
        }

        public static DataTable ExecuteQuery(string query, SqliteParameter[] parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query cannot be empty");
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null) cmd.Parameters.AddRange(parameters);

                using (var reader = cmd.ExecuteReader())
                {
                    var table = new DataTable();
                    table.Load(reader);
                    return table;
                }
            }
        }

        public static int ExecuteNonQuery(string query, SqliteParameter[] parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query cannot be empty");
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public static object ExecuteScalar(string query, SqliteParameter[] parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query cannot be empty");
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }
    }
}
