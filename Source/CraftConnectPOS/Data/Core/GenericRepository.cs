using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace CraftConnectPOS.Data
{
    public class GenericRepository<T> : IRepository<T> where T : EntityBase, new()
    {
        protected readonly string _tableName;
        protected readonly string _idColumnName;

        public GenericRepository(string tableName, string idColumnName = "Id")
        {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _idColumnName = idColumnName;
        }

        public virtual List<T> GetAll()
        {
            var entities = new List<T>();
            DataTable dt = DatabaseHelper.ExecuteQuery($"SELECT * FROM {_tableName}");
            foreach (DataRow row in dt.Rows)
                entities.Add(MapDataRowToEntity(row));
            return entities;
        }

        public virtual T GetById(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0");
            string query = $"SELECT * FROM {_tableName} WHERE {_idColumnName} = @Id";
            DataTable dt = DatabaseHelper.ExecuteQuery(query, new SqliteParameter[] { new SqliteParameter("@Id", id) });
            return dt.Rows.Count == 0 ? null : MapDataRowToEntity(dt.Rows[0]);
        }

        public virtual void Add(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var props = typeof(T).GetProperties().Where(p => p.Name != _idColumnName && p.Name != "Id").ToList();
            string columns = string.Join(", ", props.Select(p => p.Name));
            string values = string.Join(", ", props.Select(p => $"@{p.Name}"));
            string query = $"INSERT INTO {_tableName} ({columns}) VALUES ({values}); SELECT last_insert_rowid();";
            var parameters = props.Select(p => new SqliteParameter($"@{p.Name}", p.GetValue(entity) ?? DBNull.Value)).ToArray();
            object result = DatabaseHelper.ExecuteScalar(query, parameters);
            (typeof(T).GetProperty(_idColumnName) ?? typeof(T).GetProperty("Id"))?.SetValue(entity, Convert.ToInt32(result));
        }

        public virtual void Update(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var props = typeof(T).GetProperties().Where(p => p.Name != _idColumnName && p.Name != "Id").ToList();
            string setClause = string.Join(", ", props.Select(p => $"{p.Name} = @{p.Name}"));
            string query = $"UPDATE {_tableName} SET {setClause} WHERE {_idColumnName} = @Id";
            var parameters = props.Select(p => new SqliteParameter($"@{p.Name}", p.GetValue(entity) ?? DBNull.Value)).ToList();
            var idProperty = typeof(T).GetProperty(_idColumnName) ?? typeof(T).GetProperty("Id");
            parameters.Add(new SqliteParameter("@Id", idProperty?.GetValue(entity) ?? 0));
            DatabaseHelper.ExecuteNonQuery(query, parameters.ToArray());
        }

        public virtual void Delete(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0");
            DatabaseHelper.ExecuteNonQuery($"DELETE FROM {_tableName} WHERE {_idColumnName} = @Id",
                new SqliteParameter[] { new SqliteParameter("@Id", id) });
        }

        public virtual bool Exists(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0");
            int count = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                $"SELECT COUNT(*) FROM {_tableName} WHERE {_idColumnName} = @Id",
                new SqliteParameter[] { new SqliteParameter("@Id", id) }));
            return count > 0;
        }

        public virtual int GetCount()
            => Convert.ToInt32(DatabaseHelper.ExecuteScalar($"SELECT COUNT(*) FROM {_tableName}"));

        protected virtual T MapDataRowToEntity(DataRow row)
        {
            T entity = new T();
            foreach (var property in typeof(T).GetProperties())
            {
                if (row.Table.Columns.Contains(property.Name) && row[property.Name] != DBNull.Value)
                {
                    try { property.SetValue(entity, Convert.ChangeType(row[property.Name], property.PropertyType)); }
                    catch { /* skip unmappable columns */ }
                }
            }
            return entity;
        }
    }
}
