using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class SuppliersViewModel : ViewModelBase, IRefreshable
    {
        private readonly ISupplierRepository _repository;
        private readonly ObservableCollection<SupplierItem> _supplierItems;
        private SupplierItem _selectedSupplier;
        private string _supplierName;
        private string _phone;
        private string _location;
        private string _materialsSupplied;
        private string _searchText;
        private string _selectedLocationFilter = "All locations";
        private string _errorMessage;
        private bool _isBusy;

        public SuppliersViewModel(ISupplierRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _supplierItems = new ObservableCollection<SupplierItem>();
            Metrics = new ObservableCollection<MetricCard>();
            LocationFilters = new ObservableCollection<string>();
            SuppliersView = CollectionViewSource.GetDefaultView(_supplierItems);
            SuppliersView.Filter = FilterSupplier;
            NewCommand = new RelayCommand(_ => StartNew(), _ => !IsBusy);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsBusy);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => !IsBusy && SelectedSupplier != null);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => !IsBusy);
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
            set { SetProperty(ref _supplierName, value); }
        }

        public string Phone
        {
            get { return _phone; }
            set { SetProperty(ref _phone, value); }
        }

        public string Location
        {
            get { return _location; }
            set { SetProperty(ref _location, value); }
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
                if (SetProperty(ref _searchText, value)) SuppliersView.Refresh();
            }
        }

        public string SelectedLocationFilter
        {
            get { return _selectedLocationFilter; }
            set
            {
                if (SetProperty(ref _selectedLocationFilter, value)) SuppliersView.Refresh();
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
                if (SetProperty(ref _isBusy, value)) CommandManager.InvalidateRequerySuggested();
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
                Replace(_supplierItems, await _repository.GetSuppliersAsync());
                RefreshLocationFilters();
                RefreshMetrics();
                SuppliersView.Refresh();
                ErrorMessage = string.Empty;
            }
            catch (Exception exception)
            {
                ErrorMessage = "Suppliers could not be loaded. " + exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAsync()
        {
            if (!Validate()) return;
            IsBusy = true;
            var succeeded = false;
            try
            {
                var supplier = new SupplierItem
                {
                    Id = SelectedSupplier == null ? 0 : SelectedSupplier.Id,
                    Supplier = SupplierName.Trim(),
                    Phone = Phone.Trim(),
                    Location = Location.Trim()
                };
                if (supplier.Id == 0) await _repository.AddSupplierAsync(supplier);
                else await _repository.UpdateSupplierAsync(supplier);
                succeeded = true;
                SelectedSupplier = null;
                ClearForm();
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
            if (succeeded) await RefreshAsync();
        }

        private async Task DeleteAsync()
        {
            if (SelectedSupplier == null) return;
            IsBusy = true;
            var succeeded = false;
            try
            {
                await _repository.DeleteSupplierAsync(SelectedSupplier.Id);
                succeeded = true;
                SelectedSupplier = null;
                ClearForm();
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
            if (succeeded) await RefreshAsync();
        }

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(SupplierName) ||
                string.IsNullOrWhiteSpace(Phone) ||
                string.IsNullOrWhiteSpace(Location))
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
            ErrorMessage = string.Empty;
            return true;
        }

        private bool FilterSupplier(object item)
        {
            var supplier = item as SupplierItem;
            if (supplier == null) return false;
            var query = (SearchText ?? string.Empty).Trim();
            var matchesSearch = query.Length == 0 ||
                Contains(supplier.Supplier, query) ||
                Contains(supplier.Phone, query) ||
                Contains(supplier.Location, query) ||
                Contains(supplier.Materials, query);
            var matchesLocation = SelectedLocationFilter == "All locations" ||
                supplier.Location == SelectedLocationFilter;
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

        private void Cancel()
        {
            if (SelectedSupplier != null) LoadSupplier(SelectedSupplier);
            else ClearForm();
        }

        private void LoadSupplier(SupplierItem supplier)
        {
            SupplierName = supplier.Supplier;
            Phone = supplier.Phone;
            Location = supplier.Location;
            MaterialsSupplied = string.IsNullOrWhiteSpace(supplier.Materials) ? "No linked materials" : supplier.Materials;
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

        private void RefreshLocationFilters()
        {
            var current = SelectedLocationFilter;
            LocationFilters.Clear();
            LocationFilters.Add("All locations");
            foreach (var location in _supplierItems.Select(item => item.Location)
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
            Metrics.Add(new MetricCard { Title = "Total Suppliers", Value = _supplierItems.Count.ToString(), Note = "Active contacts" });
            Metrics.Add(new MetricCard { Title = "Locations", Value = _supplierItems.Select(item => item.Location).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString(), Note = "Covered areas" });
            Metrics.Add(new MetricCard { Title = "Linked Materials", Value = _supplierItems.Count(item => !string.IsNullOrWhiteSpace(item.Materials)).ToString(), Note = "Supplying inventory" });
            Metrics.Add(new MetricCard { Title = "Database", Value = "Live", Note = "Persistent records" });
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source) target.Add(item);
        }
    }
}
