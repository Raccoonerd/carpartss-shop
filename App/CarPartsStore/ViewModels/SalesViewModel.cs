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
    public partial class CartItem : ObservableObject
    {
        public int PartId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        [ObservableProperty]
        private int quantity;

        public decimal Total => Quantity * UnitPrice;
        partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(Total));
    }

    public partial class SalesViewModel : ObservableObject
    {
        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<Part> AvailableParts { get; } = new();
        public ObservableCollection<CartItem> Cart { get; } = new();

        [ObservableProperty] private Part selectedPart;
        [ObservableProperty] private Customer selectedCustomer;
        [ObservableProperty] private CartItem selectedCartItem;

        [ObservableProperty] private string quantityToAdd = "1";

        [ObservableProperty] private string searchText;
        partial void OnSearchTextChanged(string value) => LoadParts();

        [ObservableProperty] private bool showOutOfStock;
        partial void OnShowOutOfStockChanged(bool value) => _ = LoadParts();

        public int ItemsCount => Cart.Count;
        public decimal TotalAmount => Cart.Sum(c => c.Total);

        public SalesViewModel()
        {
            LoadParts();
            LoadCustomers();

            Cart.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(ItemsCount));
                OnPropertyChanged(nameof(TotalAmount));
            };
        }

        private async Task LoadParts()
        {
            if (AvailableParts == null) return;

            try
            {
                using var db = new CarPartsStoreContext();
                var query = db.Parts.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(p => p.Name.Contains(SearchText));
                }

                if (!ShowOutOfStock)
                {
                    query = query.Where(p => p.StockQuantity > 0);
                }

                AvailableParts.Clear();
                foreach (var p in query.ToList())
                {
                    AvailableParts.Add(p);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", $"Błąd wczytywania części: {ex.Message}");
            }
        }

        private async Task LoadCustomers()
        {
            if (Customers == null) return;

            try
            {
                using var db = new CarPartsStoreContext();
                Customers.Clear();
                foreach (var c in db.Customers.ToList())
                {
                    Customers.Add(c);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", $"Błąd wczytywania klientów: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AddToCart()
        {
            if (SelectedPart == null)
            {
                await DialogService.ShowInfoAsync("Potrzebne Informacje", "Wybierz część z listy.");
                return;
            }

            if (!int.TryParse(QuantityToAdd, out int quantity) || quantity <= 0)
            {
                await DialogService.ShowInfoAsync("Potrzebne Informacje", "Podaj poprawną ilość (liczba całkowita > 0).");
                return;
            }

            int inCart = Cart.Where(c => c.PartId == SelectedPart.Id).Sum(c => c.Quantity);

            if (inCart + quantity > SelectedPart.StockQuantity)
            {
                await DialogService.ShowInfoAsync("Błąd", $"Nie można dodać {quantity} sztuk. W magazynie jest tylko " +
                    $"{SelectedPart.StockQuantity - inCart} sztuk tej części.");
                return;
            }

            var existing = Cart.FirstOrDefault(c => c.PartId == SelectedPart.Id);
            if (existing != null)
            {
                existing.Quantity += quantity;
                OnPropertyChanged(nameof(TotalAmount));
            }
            else
            {
                Cart.Add(new CartItem
                {
                    PartId = SelectedPart.Id,
                    PartName = SelectedPart.Name,
                    UnitPrice = SelectedPart.Price,
                    Quantity = quantity
                });
            }

            QuantityToAdd = "1";
        }

        [RelayCommand]
        private async Task RemoveFromCart()
        {
            if (SelectedCartItem == null)
            {
                await DialogService.ShowInfoAsync("Potrzebne Informacje", "Wybierz pozycję z koszyka do usunięcia.");
                return;
            }

            Cart.Remove(SelectedCartItem);
        }

        [RelayCommand]
        private async Task ClearCart()
        {
            if (Cart.Count == 0) return;

            var result = await DialogService.ShowConfirmAsync(
                "Potwierdzenie",
                "Czy na pewno wyczyścić cały koszyk?");

            if (result == true)
            {
                Cart.Clear();
            }
        }

        [RelayCommand]
        private async Task Finalize()
        {
            if (SelectedCustomer == null)
            {
                await DialogService.ShowInfoAsync("Potrzebne Informacje", "Wybierz klienta.");
                return;
            }

            if (Cart.Count == 0)
            {
                await DialogService.ShowInfoAsync("Potrzebne Informacje", "Koszyk jest pusty.");
                return;
            }

            try
            {
                using var db = new CarPartsStoreContext();

                // sprawdź stany magazynowe (mogły się zmienić)
                foreach (var item in Cart)
                {
                    var part = db.Parts.Find(item.PartId);
                    if (part == null)
                    {
                        await DialogService.ShowInfoAsync("Błąd", $"Nie znaleziono w bazie części: {item.PartName}");
                        return;
                    }
                    if (part.StockQuantity < item.Quantity)
                    {
                        await DialogService.ShowInfoAsync("Błąd",
                            $"Brak wystarczającej ilości części \"{part.Name}\".\n" +
                            $"W magazynie: {part.StockQuantity}, w koszyku: {item.Quantity}.");
                        return;
                    }
                }

                var order = new Order
                {
                    CustomerId = SelectedCustomer.Id,
                    OrderDate = DateTime.Now,
                };
                db.Orders.Add(order);
                db.SaveChanges();   // żeby order.Id został wypełniony

                foreach (var item in Cart)
                {
                    db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        PartId = item.PartId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });

                    var part = db.Parts.Find(item.PartId);
                    part.StockQuantity -= item.Quantity;
                }

                db.SaveChanges();

                await DialogService.ShowInfoAsync("Sukces",
                    $"Zamówienie nr {order.Id} zostało zapisane.\n" +
                    $"Suma: {TotalAmount:N2} zł");

                Cart.Clear();
                SelectedCustomer = null;
                LoadParts();
            }
            catch (Exception ex)
            {
                await DialogService.ShowInfoAsync("Błąd", $"Błąd podczas finalizacji zamówienia: {ex.Message}");
            }
        }
    }
}