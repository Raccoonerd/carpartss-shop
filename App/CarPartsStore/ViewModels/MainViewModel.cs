using CarPartsStore.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace CarPartsStore.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object aktualnyWidok;

        public MainViewModel()
        {
            AktualnyWidok = new SprzedazView();
        }

        [RelayCommand]
        private void PokazCzesci() => AktualnyWidok = new CzesciView();

        [RelayCommand]
        private void PokazKlientow() => AktualnyWidok = new KlienciView();

        [RelayCommand]
        private void PokazSprzedaz() => AktualnyWidok = new SprzedazView();

    }
}
