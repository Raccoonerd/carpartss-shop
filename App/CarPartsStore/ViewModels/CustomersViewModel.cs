using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CarPartsStore.Models;
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

        private void Load()
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
                MessageBox.Show("Błąd wczytywania klientów: " + ex.Message);
            }
        }

        private void FillForm(Customer customer)
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
        private void Add()
        {
            if (!ValidateForm()) return;

            try
            {
                using var db = new CarPartsStoreContext();

                if (db.Customers.Any(c => c.Nip == FormNip))
                {
                    MessageBox.Show($"Klient z NIP '{FormNip}' już istnieje.");
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
                MessageBox.Show("Błąd dodawania klienta: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Save()
        {
            if (SelectedCustomer == null)
            {
                MessageBox.Show("Proszę wybrać klienta z listy.");
                return;
            }
            if (!ValidateForm()) return;

            try
            {
                using var db = new CarPartsStoreContext();
                var customer = db.Customers.Find(SelectedCustomer.Id);

                if (customer == null)
                {
                    MessageBox.Show($"Nie można znaleźć klienta o ID {SelectedCustomer.Id} w bazie danych.");
                    return;
                }

                if (db.Customers.Any(c => c.Nip == FormNip && c.Id != SelectedCustomer.Id))
                {
                    MessageBox.Show($"Inny klient już ma NIP '{FormNip}'.");
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
                MessageBox.Show("Błąd zapisywania klienta: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Delete()
        {
            if (SelectedCustomer == null)
            {
                MessageBox.Show("Proszę wybrać klienta do usunięcia.");
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć klienta '{SelectedCustomer.CompanyName}'?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var db = new CarPartsStoreContext();
                var customer = db.Customers.Find(SelectedCustomer.Id);
                if (customer != null)
                {
                    db.Customers.Remove(customer);
                    db.SaveChanges();
                }

                Clear();
                Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd usuwania klienta: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Clear()
        {
            SelectedCustomer = null;
            FormCompanyName = string.Empty;
            FormNip = string.Empty;
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(FormCompanyName))
            {
                MessageBox.Show("Proszę wpisać nazwę firmy.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(FormNip))
            {
                MessageBox.Show("Proszę wpisać NIP.");
                return false;
            }
            return true;
        }
    }
}