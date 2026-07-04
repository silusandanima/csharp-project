using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class InventoryViewModel : ViewModelBase
    {
        private readonly MockDataStore _dataStore;
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

        public InventoryViewModel(MockDataStore dataStore)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            Metrics = new ObservableCollection<MetricCard>();
            StatusFilters = new[] { "All statuses", "In Stock", "Low Stock", "Out of Stock" };
            MaterialsView = CollectionViewSource.GetDefaultView(_dataStore.Materials);
            MaterialsView.Filter = FilterMaterial;

            NewCommand = new RelayCommand(_ => StartNew());
            SaveCommand = new RelayCommand(_ => Save());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedMaterial != null);
            CancelCommand = new RelayCommand(_ => Cancel());

            RefreshMetrics();
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<SupplierItem> SupplierOptions
        {
            get { return _dataStore.Suppliers; }
        }
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

        public string MaterialCode
        {
            get { return _materialCode; }
            set
            {
                if (SetProperty(ref _materialCode, value)) ClearError();
            }
        }

        public string MaterialName
        {
            get { return _materialName; }
            set
            {
                if (SetProperty(ref _materialName, value)) ClearError();
            }
        }

        public string QuantityText
        {
            get { return _quantityText; }
            set
            {
                if (SetProperty(ref _quantityText, value)) ClearError();
            }
        }

        public string Unit
        {
            get { return _unit; }
            set
            {
                if (SetProperty(ref _unit, value)) ClearError();
            }
        }

        public string ReorderLevelText
        {
            get { return _reorderLevelText; }
            set
            {
                if (SetProperty(ref _reorderLevelText, value)) ClearError();
            }
        }

        public SupplierItem SelectedSupplier
        {
            get { return _selectedSupplier; }
            set
            {
                if (SetProperty(ref _selectedSupplier, value)) ClearError();
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RefreshViewAndSelection();
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
                    RefreshViewAndSelection();
                }
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set { SetProperty(ref _errorMessage, value); }
        }

        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        private bool FilterMaterial(object item)
        {
            var material = item as InventoryItem;
            if (material == null)
            {
                return false;
            }

            var query = (SearchText ?? string.Empty).Trim();
            var matchesSearch = query.Length == 0
                || Contains(material.Code, query)
                || Contains(material.Material, query)
                || Contains(material.Supplier, query);
            var matchesStatus = SelectedStatusFilter == "All statuses"
                || material.Status == SelectedStatusFilter;

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

        private void Save()
        {
            int quantity;
            int reorderLevel;
            if (!Validate(out quantity, out reorderLevel))
            {
                return;
            }

            if (SelectedMaterial == null)
            {
                _dataStore.Materials.Add(new InventoryItem
                {
                    Code = MaterialCode.Trim(),
                    Material = MaterialName.Trim(),
                    Quantity = quantity,
                    Unit = Unit.Trim(),
                    ReorderLevel = reorderLevel,
                    Supplier = SelectedSupplier.Supplier
                });
            }
            else
            {
                SelectedMaterial.Code = MaterialCode.Trim();
                SelectedMaterial.Material = MaterialName.Trim();
                SelectedMaterial.Quantity = quantity;
                SelectedMaterial.Unit = Unit.Trim();
                SelectedMaterial.ReorderLevel = reorderLevel;
                SelectedMaterial.Supplier = SelectedSupplier.Supplier;
            }

            MaterialsView.Refresh();
            _dataStore.RefreshSupplierMaterials();
            RefreshMetrics();
            SelectedMaterial = null;
            ClearForm();
        }

        private bool Validate(out int quantity, out int reorderLevel)
        {
            quantity = 0;
            reorderLevel = 0;

            if (string.IsNullOrWhiteSpace(MaterialCode)
                || string.IsNullOrWhiteSpace(MaterialName)
                || string.IsNullOrWhiteSpace(Unit)
                || SelectedSupplier == null)
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

            var duplicate = _dataStore.Materials.Any(item =>
                item != SelectedMaterial
                && string.Equals(item.Code, MaterialCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                ErrorMessage = "A material with this code already exists.";
                return false;
            }

            ErrorMessage = string.Empty;
            return true;
        }

        private void Delete()
        {
            if (SelectedMaterial == null)
            {
                return;
            }

            _dataStore.Materials.Remove(SelectedMaterial);
            _dataStore.RefreshSupplierMaterials();
            SelectedMaterial = null;
            ClearForm();
            RefreshMetrics();
            MaterialsView.Refresh();
        }

        private void Cancel()
        {
            if (SelectedMaterial != null)
            {
                LoadMaterial(SelectedMaterial);
                return;
            }

            ClearForm();
        }

        private void LoadMaterial(InventoryItem material)
        {
            MaterialCode = material.Code;
            MaterialName = material.Material;
            QuantityText = material.Quantity.ToString();
            Unit = material.Unit;
            ReorderLevelText = material.ReorderLevel.ToString();
            SelectedSupplier = SupplierOptions.FirstOrDefault(item =>
                string.Equals(item.Supplier, material.Supplier, StringComparison.OrdinalIgnoreCase));
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

        private void RefreshViewAndSelection()
        {
            MaterialsView.Refresh();
            if (SelectedMaterial != null && !MaterialsView.Contains(SelectedMaterial))
            {
                SelectedMaterial = null;
                ClearForm();
            }
        }

        private void ClearError()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = string.Empty;
            }
        }

        private void RefreshMetrics()
        {
            Metrics.Clear();
            Metrics.Add(new MetricCard { Title = "Total Materials", Value = _dataStore.Materials.Count.ToString(), Note = "Tracked items" });
            Metrics.Add(new MetricCard { Title = "Out of Stock", Value = _dataStore.Materials.Count(item => item.Status == "Out of Stock").ToString(), Note = "Needs restock" });
            Metrics.Add(new MetricCard { Title = "Reorder Items", Value = _dataStore.Materials.Count(item => item.Status == "Low Stock").ToString(), Note = "Below level" });
            Metrics.Add(new MetricCard { Title = "Inventory Value", Value = "Rs. 185K", Note = "Mock value" });
        }

        public void Refresh()
        {
            _dataStore.RefreshSupplierMaterials();
            RefreshViewAndSelection();
            RefreshMetrics();
            if (SelectedMaterial != null)
            {
                LoadMaterial(SelectedMaterial);
            }
        }
    }
}
