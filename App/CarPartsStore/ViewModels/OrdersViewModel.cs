using CarPartsStore.Models;
using CarPartsStore.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace CarPartsStore.ViewModels
{
    public partial class OrdersViewModel : ObservableObject
    {
        public ObservableCollection<Order> Orders { get; } = new();
        public ObservableCollection<OrderItem> SelectedOrderItems { get; } = new();
        public ObservableCollection<Customer> Customers { get; } = new();

        [ObservableProperty]
        private Order selectedOrder;
        partial void OnSelectedOrderChanged(Order value) => LoadOrderItems(value);

        [ObservableProperty]
        private Customer filterCustomer;
        partial void OnFilterCustomerChanged(Customer value) => LoadOrders();

        [ObservableProperty]
        private DateTime? filterDateFrom;
        partial void OnFilterDateFromChanged(DateTime? value) => LoadOrders();

        [ObservableProperty]
        private DateTime? filterDateTo;
        partial void OnFilterDateToChanged(DateTime? value) => LoadOrders();

        public decimal SelectedOrderTotal => SelectedOrderItems.Sum(i => i.Quantity * i.UnitPrice);
        public int SelectedOrderItemsCount => SelectedOrderItems.Count;

        public OrdersViewModel()
        {
            LoadCustomers();
            LoadOrders();

            SelectedOrderItems.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(SelectedOrderTotal));
                OnPropertyChanged(nameof(SelectedOrderItemsCount));
            };
        }

        private async Task LoadOrders()
        {
            if(Orders == null) return;

            try
            {
                using var db = new CarPartsStoreContext();
                var query = db.Orders.Include(o => o.Customer).AsQueryable();

                if (FilterCustomer != null)
                {
                    query = query.Where(o => o.CustomerId == FilterCustomer.Id);
                }

                if (FilterDateFrom.HasValue)
                {
                    var from = FilterDateFrom.Value.Date;
                    query = query.Where(o => o.OrderDate >= from);
                }

                if (FilterDateTo.HasValue)
                {
                    var to = FilterDateTo.Value.Date.AddDays(1);
                    query = query.Where(o => o.OrderDate < to);
                }

                Orders.Clear();
                foreach (var o in query.OrderByDescending(o => o.OrderDate).ToList())
                {
                    Orders.Add(o);
                }

                SelectedOrderItems.Clear();
                SelectedOrder = null;
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", "Błąd wczytywania zamówień: " + ex.Message);
            }
        }

        private async Task LoadOrderItems(Order order)
        {
            SelectedOrderItems.Clear();

            if(order == null) return;

            try
            {
                using var db = new CarPartsStoreContext();
                var items = db.OrderItems.Include(i => i.Part).Where(i => i.OrderId == order.Id).ToList();

                foreach(var item in items)
                {
                    SelectedOrderItems.Add(item);
                }

                OnPropertyChanged(nameof(SelectedOrderTotal));
                OnPropertyChanged(nameof(SelectedOrderItemsCount));
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Błąd", "Błąd wczytywania pozycji zamówienia: " + ex.Message);
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
                await DialogService.ShowErrorAsync("Błąd", "Błąd wczytywania klientów: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task ClearFilers()
        {
            FilterCustomer = null;
            FilterDateFrom = null;
            FilterDateTo = null;
        }

        [RelayCommand]
        private void ClearCustomerFilter()
        {
            FilterCustomer = null;
        }
    }
}
