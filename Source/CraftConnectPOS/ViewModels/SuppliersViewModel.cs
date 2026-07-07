using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Data;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.ViewModels
{
    public class SuppliersViewModel : ViewModelBase
    {
        private readonly SupplierRepository _repository = new SupplierRepository();
        private readonly MaterialRepository _materialRepository = new MaterialRepository();
        private readonly ObservableCollection<SupplierItem> _suppliers =
            new ObservableCollection<SupplierItem>();

        private SupplierItem _selectedSupplier;
        private string _supplierName;
        private string _phone;
        private string _location;
        private string _materialsSupplied;
        private string _searchText;
        private string _selectedLocationFilter = "All locations";
        private string _errorMessage;

        public SuppliersViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>();
            LocationFilters = new ObservableCollection<string>();

            SuppliersView = CollectionViewSource.GetDefaultView(_suppliers);
            SuppliersView.Filter = FilterSupplier;

            NewCommand = new RelayCommand(_ => StartNew());
            SaveCommand = new RelayCommand(_ => Save());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedSupplier != null);
            CancelCommand = new RelayCommand(_ => Cancel());

            ClearForm();
        }

        public ObservableCollection<MetricCard> Metrics { get; }

        public ObservableCollection<string> LocationFilters { get; }

        public ICollectionView SuppliersView { get; }

        public SupplierItem SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (SetProperty(ref _selectedSupplier, value))
                {
                    if (value != null)
                    {
                        LoadSupplier(value);
                    }

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string SupplierName
        {
            get => _supplierName;
            set
            {
                if (SetProperty(ref _supplierName, value))
                {
                    ClearError();
                }
            }
        }

        public string Phone
        {
            get => _phone;
            set
            {
                if (SetProperty(ref _phone, value))
                {
                    ClearError();
                }
            }
        }

        // Location in the UI is stored as Address in SQLite.
        public string Location
        {
            get => _location;
            set
            {
                if (SetProperty(ref _location, value))
                {
                    ClearError();
                }
            }
        }

        public string MaterialsSupplied
        {
            get => _materialsSupplied;
            private set => SetProperty(ref _materialsSupplied, value);
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

        public string SelectedLocationFilter
        {
            get => _selectedLocationFilter;
            set
            {
                if (SetProperty(ref _selectedLocationFilter, value))
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
            LoadSuppliers();
            SelectedSupplier = null;
            ClearForm();
        }

        private void LoadSuppliers()
        {
            try
            {
                _suppliers.Clear();
                var materials = _materialRepository.GetAll();

                foreach (Supplier supplier in _repository.GetAll())
                {
                    _suppliers.Add(new SupplierItem
                    {
                        Id = supplier.Id,
                        Supplier = supplier.SupplierName,
                        Phone = supplier.Phone,
                        Location = supplier.Address,

                        Materials = string.Join(", ", materials
                            .Where(material => material.SupplierId == supplier.Id)
                            .Select(material => material.MaterialName)
                            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                    });
                }

                RefreshLocationFilters();
                RefreshMetrics();
                SuppliersView.Refresh();

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not load suppliers: " + ex.Message;
            }
        }

        private bool FilterSupplier(object item)
        {
            var supplier = item as SupplierItem;

            if (supplier == null)
            {
                return false;
            }

            string query = (SearchText ?? string.Empty).Trim();

            bool matchesSearch =
                query.Length == 0
                || Contains(supplier.Supplier, query)
                || Contains(supplier.Phone, query)
                || Contains(supplier.Location, query)
                || Contains(supplier.Materials, query);

            bool matchesLocation =
                SelectedLocationFilter == "All locations"
                || string.Equals(
                    supplier.Location,
                    SelectedLocationFilter,
                    StringComparison.OrdinalIgnoreCase);

            return matchesSearch && matchesLocation;
        }

        private static bool Contains(string value, string query)
        {
            return (value ?? string.Empty)
                .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void StartNew()
        {
            SelectedSupplier = null;
            ClearForm();
        }

        private void Save()
        {
            if (!Validate())
            {
                return;
            }

            try
            {
                var supplier = new Supplier
                {
                    SupplierName = SupplierName.Trim(),
                    Phone = Phone.Trim(),
                    Address = Location.Trim()
                };

                if (SelectedSupplier == null)
                {
                    _repository.Add(supplier);
                }
                else
                {
                    supplier.Id = SelectedSupplier.Id;
                    _repository.Update(supplier);
                }

                LoadSuppliers();

                SelectedSupplier = null;
                ClearForm();

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not save supplier: " + ex.Message;
            }
        }

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(SupplierName)
                || string.IsNullOrWhiteSpace(Phone)
                || string.IsNullOrWhiteSpace(Location))
            {
                ErrorMessage = "Complete the supplier name, phone, and location.";
                return false;
            }

            if (SupplierName.Trim().Length > 100
                || Phone.Trim().Length > 20
                || Location.Trim().Length > 200)
            {
                ErrorMessage =
                    "Supplier name, phone, or location exceeds the database limit.";
                return false;
            }

            string phone = Phone.Trim();
            int digitCount = phone.Count(char.IsDigit);

            if (!Regex.IsMatch(phone, @"^[0-9+\-\s]+$")
                || digitCount < 7
                || digitCount > 15)
            {
                ErrorMessage = "Enter a valid phone number.";
                return false;
            }

            bool duplicate = _suppliers.Any(item =>
                item != SelectedSupplier
                && string.Equals(
                    item.Supplier,
                    SupplierName.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (duplicate)
            {
                ErrorMessage = "A supplier with this name already exists.";
                return false;
            }

            ErrorMessage = string.Empty;
            return true;
        }

        private void Delete()
        {
            if (SelectedSupplier == null)
            {
                return;
            }

            try
            {
                _repository.Delete(SelectedSupplier.Id);

                LoadSuppliers();

                SelectedSupplier = null;
                ClearForm();

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    "Could not delete supplier. It may be linked to a material. "
                    + ex.Message;
            }
        }

        private void Cancel()
        {
            if (SelectedSupplier != null)
            {
                LoadSupplier(SelectedSupplier);
                return;
            }

            ClearForm();
        }

        private void LoadSupplier(SupplierItem supplier)
        {
            SupplierName = supplier.Supplier;
            Phone = supplier.Phone;
            Location = supplier.Location;

            MaterialsSupplied = string.IsNullOrWhiteSpace(supplier.Materials)
                ? "No materials assigned"
                : supplier.Materials;

            ErrorMessage = string.Empty;
        }

        private void ClearForm()
        {
            SupplierName = string.Empty;
            Phone = string.Empty;
            Location = string.Empty;
            MaterialsSupplied = "No materials assigned";
            ErrorMessage = string.Empty;

            CommandManager.InvalidateRequerySuggested();
        }

        private void RefreshViewAndSelection()
        {
            SuppliersView.Refresh();

            if (SelectedSupplier != null
                && !SuppliersView.Contains(SelectedSupplier))
            {
                SelectedSupplier = null;
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

        private void RefreshLocationFilters()
        {
            string current = SelectedLocationFilter;

            LocationFilters.Clear();
            LocationFilters.Add("All locations");

            foreach (string location in _suppliers
                .Select(item => item.Location)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value))
            {
                LocationFilters.Add(location);
            }

            SelectedLocationFilter =
                LocationFilters.Contains(current)
                    ? current
                    : "All locations";
        }

        private void RefreshMetrics()
        {
            Metrics.Clear();

            Metrics.Add(new MetricCard
            {
                Title = "Total Suppliers",
                Value = _suppliers.Count.ToString(),
                Note = "Active contacts"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Active Contracts",
                Value = "-",
                Note = "Not tracked yet"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Avg. Lead Time",
                Value = "-",
                Note = "Not tracked yet"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Material Coverage",
                Value = "-",
                Note = "Connect Inventory"
            });
        }
    }
}
