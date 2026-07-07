using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Data;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.ViewModels
{
    public class InventoryViewModel : ViewModelBase
    {
        private readonly MaterialRepository _materialRepository =
            new MaterialRepository();

        private readonly SupplierRepository _supplierRepository =
            new SupplierRepository();

        private readonly ObservableCollection<InventoryItem> _materials =
            new ObservableCollection<InventoryItem>();

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

        public InventoryViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>();

            SupplierOptions = new ObservableCollection<SupplierItem>();

            StatusFilters = new[]
            {
                "All statuses",
                "In Stock",
                "Low Stock",
                "Out of Stock"
            };

            MaterialsView = CollectionViewSource.GetDefaultView(_materials);
            MaterialsView.Filter = FilterMaterial;

            NewCommand = new RelayCommand(_ => StartNew());
            SaveCommand = new RelayCommand(_ => Save());
            DeleteCommand = new RelayCommand(
                _ => Delete(),
                _ => SelectedMaterial != null);

            CancelCommand = new RelayCommand(_ => Cancel());

            ClearForm();
        }

        public ObservableCollection<MetricCard> Metrics { get; }

        public ObservableCollection<SupplierItem> SupplierOptions { get; }

        public ICollectionView MaterialsView { get; }

        public IEnumerable<string> StatusFilters { get; }

        public InventoryItem SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                if (SetProperty(ref _selectedMaterial, value))
                {
                    if (value != null)
                    {
                        LoadMaterial(value);
                    }

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string MaterialCode
        {
            get => _materialCode;
            set
            {
                if (SetProperty(ref _materialCode, value))
                {
                    ClearError();
                }
            }
        }

        public string MaterialName
        {
            get => _materialName;
            set
            {
                if (SetProperty(ref _materialName, value))
                {
                    ClearError();
                }
            }
        }

        public string QuantityText
        {
            get => _quantityText;
            set
            {
                if (SetProperty(ref _quantityText, value))
                {
                    ClearError();
                }
            }
        }

        public string Unit
        {
            get => _unit;
            set
            {
                if (SetProperty(ref _unit, value))
                {
                    ClearError();
                }
            }
        }

        public string ReorderLevelText
        {
            get => _reorderLevelText;
            set
            {
                if (SetProperty(ref _reorderLevelText, value))
                {
                    ClearError();
                }
            }
        }

        public SupplierItem SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (SetProperty(ref _selectedSupplier, value))
                {
                    ClearError();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
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
            get => _selectedStatusFilter;
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
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public ICommand NewCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand CancelCommand { get; }

        public void Refresh()
        {
            LoadData();
            SelectedMaterial = null;
            ClearForm();
        }

        private void LoadData()
        {
            try
            {
                SupplierOptions.Clear();

                foreach (Supplier supplier in _supplierRepository.GetAll())
                {
                    SupplierOptions.Add(new SupplierItem
                    {
                        Id = supplier.Id,
                        Supplier = supplier.SupplierName,
                        Phone = supplier.Phone,
                        Location = supplier.Address,
                        Materials = string.Empty
                    });
                }

                _materials.Clear();

                foreach (Material material in _materialRepository.GetAll())
                {
                    SupplierItem supplier = SupplierOptions.FirstOrDefault(
                        item => item.Id == material.SupplierId);

                    _materials.Add(new InventoryItem
                    {
                        Id = material.Id,
                        Material = material.MaterialName,
                        Quantity = material.Quantity,
                        Unit = material.Unit,
                        ReorderLevel = material.ReorderLevel,
                        SupplierId = material.SupplierId,
                        Supplier = supplier == null
                            ? "Unknown supplier"
                            : supplier.Supplier
                    });
                }

                MaterialsView.Refresh();
                RefreshMetrics();

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not load inventory: " + ex.Message;
            }
        }

        private bool FilterMaterial(object item)
        {
            InventoryItem material = item as InventoryItem;

            if (material == null)
            {
                return false;
            }

            string query = (SearchText ?? string.Empty).Trim();

            bool matchesSearch =
                query.Length == 0
                || Contains(material.Code, query)
                || Contains(material.Material, query)
                || Contains(material.Supplier, query);

            bool matchesStatus =
                SelectedStatusFilter == "All statuses"
                || material.Status == SelectedStatusFilter;

            return matchesSearch && matchesStatus;
        }

        private static bool Contains(string value, string query)
        {
            return (value ?? string.Empty)
                .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void StartNew()
        {
            SelectedMaterial = null;
            ClearForm();
        }

        private void Save()
        {
            decimal quantity;
            int reorderLevel;

            if (!Validate(out quantity, out reorderLevel))
            {
                return;
            }

            try
            {
                var material = new Material
                {
                    MaterialName = MaterialName.Trim(),
                    Quantity = quantity,
                    Unit = Unit.Trim(),
                    ReorderLevel = reorderLevel,
                    SupplierId = SelectedSupplier.Id
                };

                if (SelectedMaterial == null)
                {
                    _materialRepository.Add(material);
                }
                else
                {
                    material.Id = SelectedMaterial.Id;
                    _materialRepository.Update(material);
                }

                LoadData();

                SelectedMaterial = null;
                ClearForm();

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not save material: " + ex.Message;
            }
        }

        private bool Validate(out decimal quantity, out int reorderLevel)
        {
            quantity = 0m;
            reorderLevel = 0;

            if (string.IsNullOrWhiteSpace(MaterialName)
                || string.IsNullOrWhiteSpace(Unit)
                || SelectedSupplier == null)
            {
                ErrorMessage =
                    "Enter the material name, quantity, unit, reorder level, and supplier.";

                return false;
            }

            if (MaterialName.Trim().Length > 100 || Unit.Trim().Length > 20)
            {
                ErrorMessage = "Material name is limited to 100 characters and unit to 20.";
                return false;
            }

            if (!decimal.TryParse(
                    QuantityText,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out quantity)
                || quantity < 0)
            {
                ErrorMessage = "Quantity must be zero or greater.";
                return false;
            }

            if (!int.TryParse(ReorderLevelText, out reorderLevel)
                || reorderLevel < 0)
            {
                ErrorMessage =
                    "Reorder level must be a whole number of zero or greater.";

                return false;
            }

            bool duplicate = _materials.Any(item =>
                item != SelectedMaterial
                && item.SupplierId == SelectedSupplier.Id
                && string.Equals(
                    item.Material,
                    MaterialName.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (duplicate)
            {
                ErrorMessage =
                    "This supplier already has a material with that name.";

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

            try
            {
                _materialRepository.Delete(SelectedMaterial.Id);

                LoadData();

                SelectedMaterial = null;
                ClearForm();

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not delete material: " + ex.Message;
            }
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
            QuantityText = material.Quantity.ToString("0.##");
            Unit = material.Unit;
            ReorderLevelText = material.ReorderLevel.ToString();

            SelectedSupplier = SupplierOptions.FirstOrDefault(
                item => item.Id == material.SupplierId);

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

            CommandManager.InvalidateRequerySuggested();
        }

        private void RefreshViewAndSelection()
        {
            MaterialsView.Refresh();

            if (SelectedMaterial != null
                && !MaterialsView.Contains(SelectedMaterial))
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

            Metrics.Add(new MetricCard
            {
                Title = "Total Materials",
                Value = _materials.Count.ToString(),
                Note = "Tracked items"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Out of Stock",
                Value = _materials.Count(
                    item => item.Status == "Out of Stock").ToString(),
                Note = "Needs restock"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Reorder Items",
                Value = _materials.Count(
                    item => item.Status == "Low Stock").ToString(),
                Note = "Below level"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Inventory Value",
                Value = "-",
                Note = "Cost is not tracked"
            });
        }
    }
}
