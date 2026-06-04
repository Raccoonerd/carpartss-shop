using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CarPartsStore.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using CarPartsStore.Views;
using CarPartsStore.Services;

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
        partial void OnSearchTextChanged(string value) => _ = Load();

        [ObservableProperty] private string formName;
        [ObservableProperty] private string formCatalogNumber;
        [ObservableProperty] private int formCategoryId;
        [ObservableProperty] private string formPrice;
        [ObservableProperty] private string formStock;

        public PartsViewModel()
        {
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadCategories();
            await Load();
        }

        private async Task Load()
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

                var list = await query.ToListAsync();

                Parts.Clear();
                foreach (var p in list)
                    Parts.Add(p);
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", "Błąd wczytywania części: " + ex.Message);
            }
        }

        private async Task LoadCategories()
        {
            if (Categories == null) return;

            try
            {
                using var db = new CarPartsStoreContext();
                var list = await db.Categories.ToListAsync();

                Categories.Clear();
                foreach (var cat in list)
                    Categories.Add(cat);
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", "Błąd wczytywania kategorii: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task AddCategory()
        {
            var dialog = new AddCategoryDialog();
            var result = await DialogHost.Show(dialog, "MainDialogHost");

            if (result is not true)
                return;

            var name = dialog.CategoryName;
            if (string.IsNullOrWhiteSpace(name))
            {
                await DialogService.ShowInfoAsync("Błąd", "Nazwa kategorii nie może być pusta.");
                return;
            }

            try
            {
                using var db = new CarPartsStoreContext();

                if (await db.Categories.AnyAsync(c => c.Name == name))
                {
                    await DialogService.ShowInfoAsync("Błąd", $"Kategoria \"{name}\" już istnieje.");
                    return;
                }

                var newCategory = new Category { Name = name };
                db.Categories.Add(newCategory);
                await db.SaveChangesAsync();

                await LoadCategories();
                FormCategoryId = newCategory.Id;
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", "Błąd dodawania kategorii: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task Add()
        {
            var (isValid, price, stock) = await ValidateForm();
            if (!isValid)
                return;

            try
            {
                using var db = new CarPartsStoreContext();
                var newPart = new Part
                {
                    Name = FormName,
                    CatalogNumber = FormCatalogNumber,
                    CategoryId = FormCategoryId,
                    Price = price!.Value,
                    StockQuantity = stock!.Value
                };
                db.Parts.Add(newPart);
                await db.SaveChangesAsync();

                await Load();
                Clear();
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", "Błąd dodawania: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (SelectedPart == null)
            {
                await DialogService.ShowInfoAsync("Brak Danych", "Wybierz część z listy.");
                return;
            }

            var (isValid, price, stock) = await ValidateForm();
            if (!isValid)
                return;

            try
            {
                using var db = new CarPartsStoreContext();
                var part = await db.Parts.FindAsync(SelectedPart.Id);
                if (part == null)
                {
                    await DialogService.ShowInfoAsync("Brak Danych", "Nie znaleziono części w bazie.");
                    return;
                }

                part.Name = FormName;
                part.CatalogNumber = FormCatalogNumber;
                part.CategoryId = FormCategoryId;
                part.Price = price!.Value;
                part.StockQuantity = stock!.Value;

                await db.SaveChangesAsync();

                await Load();
                Clear();
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", "Błąd zapisu: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (SelectedPart == null)
            {
                await DialogService.ShowInfoAsync("Brak Danych", "Wybierz część z listy.");
                return;
            }

            var result = await DialogService.ShowConfirmAsync(
                "Potwierdzenie",
                $"Czy na pewno usunąć część \"{SelectedPart.Name}\"?");

            if (result == false)
                return;

            try
            {
                using var db = new CarPartsStoreContext();
                var part = await db.Parts.FindAsync(SelectedPart.Id);
                if (part != null)
                {
                    db.Parts.Remove(part);
                    await db.SaveChangesAsync();
                }

                await Load();
                Clear();
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", "Błąd usuwania: " + ex.Message);
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

        private async Task<(bool isValid, decimal? price, int? stock)> ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(FormName))
            {
                await DialogService.ShowInfoAsync("Brak Danych", "Podaj nazwę.");
                return (false, null, null);
            }
            if (string.IsNullOrWhiteSpace(FormCatalogNumber))
            {
                await DialogService.ShowInfoAsync("Brak Danych", "Podaj numer katalogowy.");
                return (false, null, null);
            }
            if (FormCategoryId == 0)
            {
                await DialogService.ShowInfoAsync("Brak Danych", "Wybierz kategorię.");
                return (false, null, null);
            }
            if (!decimal.TryParse(FormPrice, out var price) || price < 0)
            {
                await DialogService.ShowInfoAsync("Brak Danych", "Podaj poprawną cenę (liczba >= 0).");
                return (false, null, null);
            }
            if (!int.TryParse(FormStock, out var stock) || stock < 0)
            {
                await DialogService.ShowInfoAsync("Brak Danych", "Podaj poprawny stan magazynowy (liczba całkowita >= 0).");
                return (false, null, null);
            }

            return (true, price, stock);
        }
    }
}