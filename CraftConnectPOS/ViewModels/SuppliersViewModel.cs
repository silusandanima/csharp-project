using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class SuppliersViewModel : ViewModelBase
    {
        private readonly MockDataStore _dataStore;
        private SupplierItem _selectedSupplier;
        private string _supplierName;
        private string _phone;
        private string _location;
        private string _materialsSupplied;
        private string _searchText;
        private string _selectedLocationFilter = "All locations";
        private string _errorMessage;

        public SuppliersViewModel(MockDataStore dataStore)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            Metrics = new ObservableCollection<MetricCard>();
            LocationFilters = new ObservableCollection<string>();
            SuppliersView = CollectionViewSource.GetDefaultView(_dataStore.Suppliers);
            SuppliersView.Filter = FilterSupplier;

            NewCommand = new RelayCommand(_ => StartNew());
            SaveCommand = new RelayCommand(_ => Save());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedSupplier != null);
            CancelCommand = new RelayCommand(_ => Cancel());

            _dataStore.RefreshSupplierMaterials();
            RefreshLocationFilters();
            RefreshMetrics();
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<string> LocationFilters { get; }
        public ICollectionView SuppliersView { get; }

        public SupplierItem SelectedSupplier
        {
            get { return _selectedSupplier; }
            set
            {
                if (SetProperty(ref _selectedSupplier, value) && value != null)
                {
                    LoadSupplier(value);
                }

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SupplierName
        {
            get { return _supplierName; }
            set
            {
                if (SetProperty(ref _supplierName, value)) ClearError();
            }
        }

        public string Phone
        {
            get { return _phone; }
            set
            {
                if (SetProperty(ref _phone, value)) ClearError();
            }
        }

        public string Location
        {
            get { return _location; }
            set
            {
                if (SetProperty(ref _location, value)) ClearError();
            }
        }

        public string MaterialsSupplied
        {
            get { return _materialsSupplied; }
            private set { SetProperty(ref _materialsSupplied, value); }
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

        public string SelectedLocationFilter
        {
            get { return _selectedLocationFilter; }
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
            get { return _errorMessage; }
            private set { SetProperty(ref _errorMessage, value); }
        }

        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        public void Refresh()
        {
            _dataStore.RefreshSupplierMaterials();
            RefreshLocationFilters();
            SuppliersView.Refresh();
            RefreshMetrics();
            if (SelectedSupplier != null)
            {
                LoadSupplier(SelectedSupplier);
            }
        }

        private bool FilterSupplier(object item)
        {
            var supplier = item as SupplierItem;
            if (supplier == null)
            {
                return false;
            }

            var query = (SearchText ?? string.Empty).Trim();
            var matchesSearch = query.Length == 0
                || Contains(supplier.Supplier, query)
                || Contains(supplier.Phone, query)
                || Contains(supplier.Location, query)
                || Contains(supplier.Materials, query);
            var matchesLocation = SelectedLocationFilter == "All locations"
                || supplier.Location == SelectedLocationFilter;

            return matchesSearch && matchesLocation;
        }

        private static bool Contains(string value, string query)
        {
            return (value ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
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

            if (SelectedSupplier == null)
            {
                _dataStore.Suppliers.Add(new SupplierItem
                {
                    Supplier = SupplierName.Trim(),
                    Phone = Phone.Trim(),
                    Location = Location.Trim()
                });
            }
            else
            {
                var previousName = SelectedSupplier.Supplier;
                SelectedSupplier.Supplier = SupplierName.Trim();
                SelectedSupplier.Phone = Phone.Trim();
                SelectedSupplier.Location = Location.Trim();
                foreach (var material in _dataStore.Materials.Where(item =>
                    string.Equals(item.Supplier, previousName, StringComparison.OrdinalIgnoreCase)))
                {
                    material.Supplier = SelectedSupplier.Supplier;
                }
            }

            _dataStore.RefreshSupplierMaterials();
            RefreshLocationFilters();
            SuppliersView.Refresh();
            RefreshMetrics();
            SelectedSupplier = null;
            ClearForm();
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

            var phone = Phone.Trim();
            var digitCount = phone.Count(char.IsDigit);
            if (!Regex.IsMatch(phone, @"^[0-9+\-\s]+$") || digitCount < 7 || digitCount > 15)
            {
                ErrorMessage = "Enter a valid phone number.";
                return false;
            }

            var duplicate = _dataStore.Suppliers.Any(item =>
                item != SelectedSupplier
                && string.Equals(item.Supplier, SupplierName.Trim(), StringComparison.OrdinalIgnoreCase));
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

            if (_dataStore.Materials.Any(item =>
                string.Equals(item.Supplier, SelectedSupplier.Supplier, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorMessage = "This supplier is linked to inventory materials and cannot be deleted.";
                return;
            }

            _dataStore.Suppliers.Remove(SelectedSupplier);
            SelectedSupplier = null;
            ClearForm();
            RefreshLocationFilters();
            RefreshMetrics();
            SuppliersView.Refresh();
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
                ? "No linked materials"
                : supplier.Materials;
            ErrorMessage = string.Empty;
        }

        private void ClearForm()
        {
            SupplierName = string.Empty;
            Phone = string.Empty;
            Location = string.Empty;
            MaterialsSupplied = "Linked materials appear here";
            ErrorMessage = string.Empty;
        }

        private void RefreshViewAndSelection()
        {
            SuppliersView.Refresh();
            if (SelectedSupplier != null && !SuppliersView.Contains(SelectedSupplier))
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
            var current = SelectedLocationFilter;
            LocationFilters.Clear();
            LocationFilters.Add("All locations");
            foreach (var location in _dataStore.Suppliers
                .Select(item => item.Location)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value))
            {
                LocationFilters.Add(location);
            }

            SelectedLocationFilter = LocationFilters.Contains(current) ? current : "All locations";
        }

        private void RefreshMetrics()
        {
            Metrics.Clear();
            Metrics.Add(new MetricCard { Title = "Total Suppliers", Value = _dataStore.Suppliers.Count.ToString(), Note = "Active contacts" });
            Metrics.Add(new MetricCard { Title = "Active Contracts", Value = "18", Note = "Mock value" });
            Metrics.Add(new MetricCard { Title = "Avg. Lead Time", Value = "4-5 days", Note = "Mock value" });
            Metrics.Add(new MetricCard { Title = "Material Coverage", Value = "94%", Note = "Mock value" });
        }
    }
}
