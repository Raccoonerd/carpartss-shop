using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CarPartsStore.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPartsStore.ViewModels
{
    // Klasa pomocnicza dla pozycji koszyka (nie encja z bazy)
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

        private void LoadParts()
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

                AvailableParts.Clear();
                foreach (var p in query.ToList())
                {
                    AvailableParts.Add(p);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wczytywania części: {ex.Message}");
            }
        }

        private void LoadCustomers()
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
                MessageBox.Show($"Błąd wczytywania klientów: {ex.Message}");
            }
        }

        [RelayCommand]
        private void AddToCart()
        {
            if (SelectedPart == null)
            {
                MessageBox.Show("Wybierz część z listy.");
                return;
            }

            if (!int.TryParse(QuantityToAdd, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Podaj poprawną ilość (liczba całkowita > 0).");
                return;
            }

            int inCart = Cart.Where(c => c.PartId == SelectedPart.Id).Sum(c => c.Quantity);

            if (inCart + quantity > SelectedPart.StockQuantity)
            {
                MessageBox.Show(
                    $"Nie można dodać {quantity} sztuk. W magazynie jest tylko " +
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
        private void RemoveFromCart()
        {
            if (SelectedCartItem == null)
            {
                MessageBox.Show("Wybierz pozycję z koszyka do usunięcia.");
                return;
            }

            Cart.Remove(SelectedCartItem);
        }

        [RelayCommand]
        private void ClearCart()
        {
            if (Cart.Count == 0) return;

            var result = MessageBox.Show(
                "Czy na pewno wyczyścić cały koszyk?",
                "Potwierdzenie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Cart.Clear();
            }
        }

        [RelayCommand]
        private void Finalize()
        {
            if (SelectedCustomer == null)
            {
                MessageBox.Show("Wybierz klienta.");
                return;
            }

            if (Cart.Count == 0)
            {
                MessageBox.Show("Koszyk jest pusty.");
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
                        MessageBox.Show($"Nie znaleziono w bazie części: {item.PartName}");
                        return;
                    }
                    if (part.StockQuantity < item.Quantity)
                    {
                        MessageBox.Show(
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

                MessageBox.Show(
                    $"Zamówienie nr {order.Id} zostało zapisane.\n" +
                    $"Suma: {TotalAmount:N2} zł",
                    "Sukces",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Cart.Clear();
                SelectedCustomer = null;
                LoadParts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas finalizacji zamówienia: {ex.Message}");
            }
        }
    }
}