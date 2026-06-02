using CarPartsStore.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPartsStore.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object currentView;

        public MainViewModel()
        {
            CurrentView = new SalesView();
        }

        [RelayCommand]
        private void ShowParts() => CurrentView = new PartsView();

        [RelayCommand]
        private void ShowCustomers() => CurrentView = new CustomersView();

        [RelayCommand]
        private void ShowSales() => CurrentView = new SalesView();
    }
}