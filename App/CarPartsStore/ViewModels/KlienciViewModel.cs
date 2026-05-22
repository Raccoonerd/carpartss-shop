using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using CarPartsStore.Models;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace CarPartsStore.ViewModels
{
    public partial class KlienciViewModel : ObservableObject
    {
        public ObservableCollection<Klienci> Klienci { get; } = new();

        [ObservableProperty]
        private Klienci wybranyKlient;

        partial void OnWybranyKlientChanged(Klienci value) => WypelnijFormularz(value);

        [ObservableProperty] private string formNazwa;
        [ObservableProperty] private string formNip;

        [ObservableProperty]
        private string tekstWyszukiwania;

        partial void OnTekstWyszukiwaniaChanged(string value) => Wczytaj();

        public KlienciViewModel()
        {
            Wczytaj();
        }

        private void Wczytaj()
        {
            if(Klienci == null) return;

            try
            {
                using var db = new CarPartsShopContext();
                var zapytanie = db.Klienci.AsQueryable();

                if(!string.IsNullOrWhiteSpace(tekstWyszukiwania))
                {
                    zapytanie = zapytanie.Where(k => k.NazwaFirmy.Contains(tekstWyszukiwania));
                }

                Klienci.Clear();
                foreach (var k in zapytanie.ToList())
                {
                    Klienci.Add(k);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania klientów: " + ex.Message);
            }
        }

        private void WypelnijFormularz(Klienci klienct)
        {
            if (klienct == null)
            {
                FormNazwa = string.Empty;
                FormNip = string.Empty;
                return;
            }
            FormNazwa = klienct.NazwaFirmy;
            FormNip = klienct.Nip;
        }

        [RelayCommand]
        private void Dodaj()
        {
            if (!WalidujFormularz()) return;

            try
            {
                using var db = new CarPartsShopContext();

                if (db.Klienci.Any(k => k.Nip == FormNip))
                {
                    MessageBox.Show($"Klient z NIP '{FormNip}' już istnieje.");
                    return;
                }

                var nowyKlient = new Klienci
                {
                    NazwaFirmy = FormNazwa,
                    Nip = FormNip
                };
                db.Klienci.Add(nowyKlient);
                db.SaveChanges();
                Wczytaj();
                Wyczysc();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd dodawania klienta: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Zapisz()
        {
            if (WybranyKlient == null)
            {
                MessageBox.Show($"Proszę wybrać klienta z listy.");
                return;
            }
            if (!WalidujFormularz()) return;

            try
            {
                using var db = new CarPartsShopContext();
                var klient = db.Klienci.Find(WybranyKlient.Id);

                if (klient == null)
                {
                    MessageBox.Show($"Nie można znaleźć klienta o ID {WybranyKlient.Id} w bazie danych.");
                    return;
                }

                if (db.Klienci.Any(k => k.Nip == FormNip && k.Id != WybranyKlient.Id))
                {
                    MessageBox.Show($"Inny klient już ma NIP '{FormNip}'.");
                    return;
                }

                klient.NazwaFirmy = FormNazwa;
                klient.Nip = FormNip;
                db.SaveChanges();
                Wyczysc();
                Wczytaj();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisywania klienta: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Usun()
        {
            if (WybranyKlient == null)
            {
                MessageBox.Show("Proszę wybrać klienta do usunięcia.");
                return;
            }

            var wynik = MessageBox.Show(
                $"Czy na pewno chcesz usunąć klienta '{WybranyKlient.NazwaFirmy}'?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (wynik != MessageBoxResult.Yes) return;

            try
            {
                using var db = new CarPartsShopContext();
                var klient = db.Klienci.Find(WybranyKlient.Id);
                if (klient != null)
                {
                    db.Klienci.Remove(klient);
                    db.SaveChanges();
                }

                Wyczysc();
                Wczytaj();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd usuwania klienta: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Wyczysc()
        {
            WybranyKlient = null;
            FormNazwa = string.Empty;
            FormNip = string.Empty;
        }

        private bool WalidujFormularz()
        {
            if (string.IsNullOrWhiteSpace(FormNazwa))
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
    