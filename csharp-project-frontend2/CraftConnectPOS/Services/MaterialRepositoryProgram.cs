using System;
using System.Data;
using System.Data.SqlClient;

namespace CraftConnectPOS.Data
{
    // Uses EntityBase, GenericRepository, and DatabaseHelper
    // that already exist in your working ProductRepositoryProgram.cs file.

    public class Material : EntityBase
    {
        public string MaterialName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public int ReorderLevel { get; set; }
        public int SupplierId { get; set; }

        public Material()
        {
            MaterialName = string.Empty;
            Unit = string.Empty;
        }
    }

    public class MaterialRepository : GenericRepository<Material>
    {
        public MaterialRepository()
            : base("dbo.Materials", "MaterialID")
        {
        }

        protected override Material MapDataRowToEntity(DataRow row)
        {
            return new Material
            {
                Id = Convert.ToInt32(row["MaterialID"]),

                MaterialName = row["MaterialName"] == DBNull.Value
                    ? string.Empty
                    : row["MaterialName"].ToString(),

                Quantity = row["Quantity"] == DBNull.Value
                    ? 0m
                    : Convert.ToDecimal(row["Quantity"]),

                Unit = row["Unit"] == DBNull.Value
                    ? string.Empty
                    : row["Unit"].ToString(),

                ReorderLevel = row["ReorderLevel"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(row["ReorderLevel"]),

                SupplierId = row["SupplierID"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(row["SupplierID"])
            };
        }

        public override void Add(Material material)
        {
            const string query = @"
                INSERT INTO dbo.Materials
                    (MaterialName, Quantity, Unit, ReorderLevel, SupplierID)
                VALUES
                    (@MaterialName, @Quantity, @Unit, @ReorderLevel, @SupplierID);

                SELECT SCOPE_IDENTITY();";

            object result = DatabaseHelper.ExecuteScalar(
                query,
                new[]
                {
                    new SqlParameter("@MaterialName", material.MaterialName),
                    new SqlParameter("@Quantity", material.Quantity),
                    new SqlParameter("@Unit", material.Unit),
                    new SqlParameter("@ReorderLevel", material.ReorderLevel),
                    new SqlParameter("@SupplierID", material.SupplierId)
                });

            material.Id = Convert.ToInt32(result);
        }

        public override void Update(Material material)
        {
            const string query = @"
                UPDATE dbo.Materials
                SET
                    MaterialName = @MaterialName,
                    Quantity = @Quantity,
                    Unit = @Unit,
                    ReorderLevel = @ReorderLevel,
                    SupplierID = @SupplierID
                WHERE MaterialID = @MaterialID;";

            DatabaseHelper.ExecuteNonQuery(
                query,
                new[]
                {
                    new SqlParameter("@MaterialName", material.MaterialName),
                    new SqlParameter("@Quantity", material.Quantity),
                    new SqlParameter("@Unit", material.Unit),
                    new SqlParameter("@ReorderLevel", material.ReorderLevel),
                    new SqlParameter("@SupplierID", material.SupplierId),
                    new SqlParameter("@MaterialID", material.Id)
                });
        }

        public override void Delete(int id)
        {
            const string query = @"
                DELETE FROM dbo.Materials
                WHERE MaterialID = @MaterialID;";

            DatabaseHelper.ExecuteNonQuery(
                query,
                new[]
                {
                    new SqlParameter("@MaterialID", id)
                });
        }
    }
}