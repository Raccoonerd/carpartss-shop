using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CarPartsStore.Models;
using CarPartsStore.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPartsStore.ViewModels
{
    public partial class CustomersViewModel : ObservableObject
    {
        public ObservableCollection<Customer> Customers { get; } = new();

        [ObservableProperty]
        private Customer selectedCustomer;
        partial void OnSelectedCustomerChanged(Customer value) => FillForm(value);

        [ObservableProperty] private string formCompanyName;
        [ObservableProperty] private string formNip;

        [ObservableProperty]
        private string searchText;
        partial void OnSearchTextChanged(string value) => Load();

        public CustomersViewModel()
        {
            Load();
        }

        private async Task Load()
        {
            if (Customers == null) return;

            try
            {
                using var db = new CarPartsStoreContext();
                var query = db.Customers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(c => c.CompanyName.Contains(SearchText));
                }

                Customers.Clear();
                foreach (var c in query.ToList())
                {
                    Customers.Add(c);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", $"Błąd wczytywania klientów: {ex.Message}");
            }
        }

        private async Task FillForm(Customer customer)
        {
            if (customer == null)
            {
                FormCompanyName = string.Empty;
                FormNip = string.Empty;
                return;
            }
            FormCompanyName = customer.CompanyName;
            FormNip = customer.Nip;
        }

        [RelayCommand]
        private async Task Add()
        {
            if (!await ValidateForm()) return;

            try
            {
                using var db = new CarPartsStoreContext();

                if (db.Customers.Any(c => c.Nip == FormNip))
                {
                    await DialogService.ShowInfoAsync("Duplikat", $"Klient z NIP '{FormNip}' już istnieje.");
                    return;
                }

                var newCustomer = new Customer
                {
                    CompanyName = FormCompanyName,
                    Nip = FormNip
                };
                db.Customers.Add(newCustomer);
                db.SaveChanges();
                Load();
                Clear();
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", $"Błąd dodawania klienta: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (SelectedCustomer == null)
            {
                await DialogService.ShowInfoAsync("Brak danych", "Proszę wybrać klienta z listy.");
                return;
            }
            if (!await ValidateForm()) return;

            try
            {
                using var db = new CarPartsStoreContext();
                var customer = db.Customers.Find(SelectedCustomer.Id);

                if (customer == null)
                {
                    await DialogService.ShowInfoAsync("Nie wczytano", $"Nie można znależć klienta o ID {SelectedCustomer.Id} w bazie danych");
                    return;
                }

                if (db.Customers.Any(c => c.Nip == FormNip && c.Id != SelectedCustomer.Id))
                {
                    await DialogService.ShowInfoAsync("Duplikat", $"Inny klient już ma NIP '{FormNip}'.");
                    return;
                }

                customer.CompanyName = FormCompanyName;
                customer.Nip = FormNip;
                db.SaveChanges();
                Clear();
                Load();
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd zapisu", $"Błąd zapisywania klienta: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Clear()
        {
            SelectedCustomer = null;
            FormCompanyName = string.Empty;
            FormNip = string.Empty;
        }

        private async Task<bool> ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(FormCompanyName))
            {
                await DialogService.ShowInfoAsync("Brak danych", "Proszę wpisać nazwę firmy.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(FormNip))
            {
                await DialogService.ShowInfoAsync("Brak danych", "Proszę wpisać NIP.");
                return false;
            }
            return true;
        }
    }
}