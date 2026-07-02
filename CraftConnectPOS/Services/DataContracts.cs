using System.Collections.Generic;
using System.Threading.Tasks;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.Services
{
    public interface IDatabaseInitializer
    {
        string DatabasePath { get; }
        Task InitializeAsync();
    }

    public interface IAuthenticationService
    {
        Task<UserAccount> AuthenticateAsync(string username, string password);
    }

    public interface ISupplierRepository
    {
        Task<IList<SupplierItem>> GetSuppliersAsync();
        Task AddSupplierAsync(SupplierItem supplier);
        Task UpdateSupplierAsync(SupplierItem supplier);
        Task DeleteSupplierAsync(long id);
    }

    public interface IMaterialRepository
    {
        Task<IList<InventoryItem>> GetMaterialsAsync();
        Task AddMaterialAsync(InventoryItem material);
        Task UpdateMaterialAsync(InventoryItem material);
        Task DeleteMaterialAsync(long id);
    }

    public interface IProductRepository
    {
        Task<IList<ProductItem>> GetProductsAsync();
        Task AddProductAsync(ProductItem product);
        Task UpdateProductAsync(ProductItem product);
        Task DeleteProductAsync(long id);
    }

    public interface IOrderRepository
    {
        Task<IList<CustomerOrder>> GetOrdersAsync();
        Task AddOrderAsync(CustomerOrder order);
        Task UpdateOrderAsync(CustomerOrder order);
        Task DeleteOrderAsync(long id);
    }

    public interface IReportingService
    {
        Task<DashboardData> GetDashboardAsync();
        Task<StatisticsData> GetStatisticsAsync();
    }

    public interface IAppSession
    {
        UserAccount CurrentUser { get; set; }
        void Clear();
    }

    public class DataStoreException : System.Exception
    {
        public DataStoreException(string message)
            : base(message)
        {
        }

        public DataStoreException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
