using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class InventoryViewModel : ViewModelBase, IRefreshable
    {
        private readonly IMaterialRepository _materials;
        private readonly ISupplierRepository _suppliers;
        private readonly ObservableCollection<InventoryItem> _materialItems;
        private InventoryItem _selectedMaterial;
        private SupplierItem _selectedSupplier;
        private string _materialCode;
        private string _materialName;
        private string _quantityText;
        private string _unit;
        private string _reorderLevelText;
        private string _searchText;
        private string _selectedStatusFilter = "All statuses";
        private string _errorMessage;
        private bool _isBusy;

        public InventoryViewModel(IMaterialRepository materials, ISupplierRepository suppliers)
        {
            _materials = materials ?? throw new ArgumentNullException(nameof(materials));
            _suppliers = suppliers ?? throw new ArgumentNullException(nameof(suppliers));
            _materialItems = new ObservableCollection<InventoryItem>();
            Metrics = new ObservableCollection<MetricCard>();
            SupplierOptions = new ObservableCollection<SupplierItem>();
            StatusFilters = new[] { "All statuses", "In Stock", "Low Stock", "Out of Stock" };
            MaterialsView = CollectionViewSource.GetDefaultView(_materialItems);
            MaterialsView.Filter = FilterMaterial;
            NewCommand = new RelayCommand(_ => StartNew(), _ => !IsBusy);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsBusy);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => !IsBusy && SelectedMaterial != null);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => !IsBusy);
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<SupplierItem> SupplierOptions { get; }
        public ICollectionView MaterialsView { get; }
        public IEnumerable<string> StatusFilters { get; }

        public InventoryItem SelectedMaterial
        {
            get { return _selectedMaterial; }
            set
            {
                if (SetProperty(ref _selectedMaterial, value) && value != null)
                {
                    LoadMaterial(value);
                }
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public SupplierItem SelectedSupplier
        {
            get { return _selectedSupplier; }
            set { SetProperty(ref _selectedSupplier, value); }
        }

        public string MaterialCode
        {
            get { return _materialCode; }
            set { SetProperty(ref _materialCode, value); }
        }

        public string MaterialName
        {
            get { return _materialName; }
            set { SetProperty(ref _materialName, value); }
        }

        public string QuantityText
        {
            get { return _quantityText; }
            set { SetProperty(ref _quantityText, value); }
        }

        public string Unit
        {
            get { return _unit; }
            set { SetProperty(ref _unit, value); }
        }

        public string ReorderLevelText
        {
            get { return _reorderLevelText; }
            set { SetProperty(ref _reorderLevelText, value); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    MaterialsView.Refresh();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get { return _selectedStatusFilter; }
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    MaterialsView.Refresh();
                }
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set { SetProperty(ref _errorMessage, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        public async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var supplierItems = await _suppliers.GetSuppliersAsync();
                var materials = await _materials.GetMaterialsAsync();
                Replace(SupplierOptions, supplierItems);
                Replace(_materialItems, materials);
                MaterialsView.Refresh();
                RefreshMetrics();
                ErrorMessage = string.Empty;
            }
            catch (Exception exception)
            {
                ErrorMessage = "Inventory could not be loaded. " + exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAsync()
        {
            int quantity;
            int reorderLevel;
            if (!Validate(out quantity, out reorderLevel))
            {
                return;
            }

            var succeeded = false;
            IsBusy = true;
            try
            {
                var material = new InventoryItem
                {
                    Id = SelectedMaterial == null ? 0 : SelectedMaterial.Id,
                    Code = MaterialCode.Trim(),
                    Material = MaterialName.Trim(),
                    Quantity = quantity,
                    Unit = Unit.Trim(),
                    ReorderLevel = reorderLevel,
                    SupplierId = SelectedSupplier.Id,
                    Supplier = SelectedSupplier.Supplier
                };

                if (material.Id == 0)
                {
                    await _materials.AddMaterialAsync(material);
                }
                else
                {
                    await _materials.UpdateMaterialAsync(material);
                }
                SelectedMaterial = null;
                ClearForm();
                succeeded = true;
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
            if (succeeded)
            {
                await RefreshAsync();
            }
        }

        private async Task DeleteAsync()
        {
            if (SelectedMaterial == null) return;
            var succeeded = false;
            IsBusy = true;
            try
            {
                await _materials.DeleteMaterialAsync(SelectedMaterial.Id);
                SelectedMaterial = null;
                ClearForm();
                succeeded = true;
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
            if (succeeded)
            {
                await RefreshAsync();
            }
        }

        private bool Validate(out int quantity, out int reorderLevel)
        {
            quantity = 0;
            reorderLevel = 0;
            if (string.IsNullOrWhiteSpace(MaterialCode) ||
                string.IsNullOrWhiteSpace(MaterialName) ||
                string.IsNullOrWhiteSpace(Unit) ||
                SelectedSupplier == null)
            {
                ErrorMessage = "Complete all material fields and select a supplier.";
                return false;
            }
            if (!int.TryParse(QuantityText, out quantity) || quantity < 0)
            {
                ErrorMessage = "Quantity must be a whole number of zero or more.";
                return false;
            }
            if (!int.TryParse(ReorderLevelText, out reorderLevel) || reorderLevel < 0)
            {
                ErrorMessage = "Reorder level must be a whole number of zero or more.";
                return false;
            }
            ErrorMessage = string.Empty;
            return true;
        }

        private bool FilterMaterial(object item)
        {
            var material = item as InventoryItem;
            if (material == null) return false;
            var query = (SearchText ?? string.Empty).Trim();
            var matchesSearch = query.Length == 0 ||
                Contains(material.Code, query) ||
                Contains(material.Material, query) ||
                Contains(material.Supplier, query);
            var matchesStatus = SelectedStatusFilter == "All statuses" ||
                material.Status == SelectedStatusFilter;
            return matchesSearch && matchesStatus;
        }

        private static bool Contains(string value, string query)
        {
            return (value ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void StartNew()
        {
            SelectedMaterial = null;
            ClearForm();
        }

        private void Cancel()
        {
            if (SelectedMaterial != null) LoadMaterial(SelectedMaterial);
            else ClearForm();
        }

        private void LoadMaterial(InventoryItem material)
        {
            MaterialCode = material.Code;
            MaterialName = material.Material;
            QuantityText = material.Quantity.ToString();
            Unit = material.Unit;
            ReorderLevelText = material.ReorderLevel.ToString();
            SelectedSupplier = SupplierOptions.FirstOrDefault(item => item.Id == material.SupplierId);
            ErrorMessage = string.Empty;
        }

        private void ClearForm()
        {
            MaterialCode = string.Empty;
            MaterialName = string.Empty;
            QuantityText = string.Empty;
            Unit = string.Empty;
            ReorderLevelText = string.Empty;
            SelectedSupplier = null;
            ErrorMessage = string.Empty;
        }

        private void RefreshMetrics()
        {
            Metrics.Clear();
            Metrics.Add(new MetricCard { Title = "Total Materials", Value = _materialItems.Count.ToString(), Note = "Tracked items" });
            Metrics.Add(new MetricCard { Title = "Out of Stock", Value = _materialItems.Count(item => item.Status == "Out of Stock").ToString(), Note = "Needs restock" });
            Metrics.Add(new MetricCard { Title = "Reorder Items", Value = _materialItems.Count(item => item.Status == "Low Stock").ToString(), Note = "Below level" });
            Metrics.Add(new MetricCard { Title = "Suppliers", Value = SupplierOptions.Count.ToString(), Note = "Linked records" });
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source) target.Add(item);
        }
    }
}
