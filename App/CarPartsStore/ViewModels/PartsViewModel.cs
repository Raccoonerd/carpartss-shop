using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CarPartsStore.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace CarPartsStore.ViewModels
{
    public partial class PartsViewModel : ObservableObject
    {
        public ObservableCollection<Part> Parts { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();

        [ObservableProperty]
        private Part selectedPart;
        partial void OnSelectedPartChanged(Part value) => FillForm(value);

        [ObservableProperty]
        private string searchText;
        partial void OnSearchTextChanged(string value) => Load();

        [ObservableProperty] private string formName;
        [ObservableProperty] private string formCatalogNumber;
        [ObservableProperty] private int formCategoryId;
        [ObservableProperty] private string formPrice;
        [ObservableProperty] private string formStock;

        public PartsViewModel()
        {
            LoadCategories();
            Load();
        }

        private void Load()
        {
            if (Parts == null) return;

            try
            {
                using var db = new CarPartsStoreContext();
                var query = db.Parts.Include(p => p.Category).AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(p => p.Name.Contains(SearchText));
                }

                Parts.Clear();
                foreach (var p in query.ToList())
                    Parts.Add(p);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania części: " + ex.Message);
            }
        }

        private void LoadCategories()
        {
            if (Categories == null) return;

            try
            {
                using var db = new CarPartsStoreContext();
                Categories.Clear();
                foreach (var cat in db.Categories.ToList())
                {
                    Categories.Add(cat);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania kategorii: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Add()
        {
            if (!ValidateForm(out decimal price, out int stock))
                return;

            try
            {
                using var db = new CarPartsStoreContext();
                var newPart = new Part
                {
                    Name = FormName,
                    CatalogNumber = FormCatalogNumber,
                    CategoryId = FormCategoryId,
                    Price = price,
                    StockQuantity = stock
                };
                db.Parts.Add(newPart);
                db.SaveChanges();

                Load();
                Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd dodawania: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Save()
        {
            if (SelectedPart == null)
            {
                MessageBox.Show("Wybierz część z listy.");
                return;
            }

            if (!ValidateForm(out decimal price, out int stock))
                return;

            try
            {
                using var db = new CarPartsStoreContext();
                var part = db.Parts.Find(SelectedPart.Id);
                if (part == null)
                {
                    MessageBox.Show("Nie znaleziono części w bazie.");
                    return;
                }

                part.Name = FormName;
                part.CatalogNumber = FormCatalogNumber;
                part.CategoryId = FormCategoryId;
                part.Price = price;
                part.StockQuantity = stock;

                db.SaveChanges();

                Load();
                Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisu: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Delete()
        {
            if (SelectedPart == null)
            {
                MessageBox.Show("Wybierz część z listy.");
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno usunąć część \"{SelectedPart.Name}\"?",
                "Potwierdzenie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var db = new CarPartsStoreContext();
                var part = db.Parts.Find(SelectedPart.Id);
                if (part != null)
                {
                    db.Parts.Remove(part);
                    db.SaveChanges();
                }

                Load();
                Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd usuwania: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Clear()
        {
            FormName = string.Empty;
            FormCatalogNumber = string.Empty;
            FormCategoryId = 0;
            FormPrice = string.Empty;
            FormStock = string.Empty;
            SelectedPart = null;
        }

        private void FillForm(Part part)
        {
            if (part == null) return;

            FormName = part.Name;
            FormCatalogNumber = part.CatalogNumber;
            FormCategoryId = part.CategoryId;
            FormPrice = part.Price.ToString();
            FormStock = part.StockQuantity.ToString();
        }

        private bool ValidateForm(out decimal price, out int stock)
        {
            price = 0;
            stock = 0;

            if (string.IsNullOrWhiteSpace(FormName))
            {
                MessageBox.Show("Podaj nazwę.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(FormCatalogNumber))
            {
                MessageBox.Show("Podaj numer katalogowy.");
                return false;
            }
            if (FormCategoryId == 0)
            {
                MessageBox.Show("Wybierz kategorię.");
                return false;
            }
            if (!decimal.TryParse(FormPrice, out price) || price < 0)
            {
                MessageBox.Show("Podaj poprawną cenę (liczba >= 0).");
                return false;
            }
            if (!int.TryParse(FormStock, out stock) || stock < 0)
            {
                MessageBox.Show("Podaj poprawny stan magazynowy (liczba całkowita >= 0).");
                return false;
            }

            return true;
        }
    }
}