using CarPartsStore.Models;
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
    internal partial class CzesciViewModel : ObservableObject
    {
        public ObservableCollection<Czesci> Czesci { get; } = new();
        public ObservableCollection<Kategorie> Kategorie { get; } = new();

        [ObservableProperty]
        private Czesci wybranaCzesc;

        partial void OnWybranaCzescChanged(Czesci value) => WypelnijFormularz(value);

        private string textWyszukiwania;

        [ObservableProperty] private string formNazwa;
        [ObservableProperty] private string formNrKatalogowy;
        [ObservableProperty] private int formKategoriaId;
        [ObservableProperty] private string formCena;
        [ObservableProperty] private string formStan;


        public CzesciViewModel ()
        {
            WczytajKategorie();
            Wczytaj();
        }

        private void Wczytaj()
        {
            if (Czesci == null) return;

            try
            {
                using var db = new CarPartsShopContext();
                var zapytanie = db.Czesci.Include(c => c.Kategoria).AsQueryable();

                if (!string.IsNullOrWhiteSpace(textWyszukiwania)) 
                { 
                    zapytanie = zapytanie.Where(c => c.Nazwa.Contains(textWyszukiwania));
                }

                Czesci.Clear();
                foreach (var c in zapytanie.ToList())
                    Czesci.Add(c);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania części: " + ex.Message);
            }
        }

        private void WczytajKategorie()
        {
            if (Kategorie == null) return;

            try
            {
                using var db = new CarPartsShopContext();
                Kategorie.Clear();
                foreach (var kat in db.Kategorie.ToList())
                {
                    Kategorie.Add(kat);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania kategorii: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Dodaj()
        {
            if (!WalidujFormularz(out decimal cena, out int stan))
                return;

            try
            {
                using var db = new CarPartsShopContext();
                var nowa = new Czesci
                {
                    Nazwa = FormNazwa,
                    NrKatalogowy = FormNrKatalogowy,
                    KategoriaId = FormKategoriaId,
                    Cena = cena,
                    StanMagazynowy = stan
                };
                db.Czesci.Add(nowa);
                db.SaveChanges();

                Wczytaj();
                Wyczysc();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd dodawania: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Zapisz()
        {
            if (WybranaCzesc == null)
            {
                MessageBox.Show("Wybierz część z listy.");
                return;
            }

            if (!WalidujFormularz(out decimal cena, out int stan))
                return;

            try
            {
                using var db = new CarPartsShopContext();
                var czesc = db.Czesci.Find(WybranaCzesc.Id);
                if (czesc == null)
                {
                    MessageBox.Show("Nie znaleziono części w bazie.");
                    return;
                }

                czesc.Nazwa = FormNazwa;
                czesc.NrKatalogowy = FormNrKatalogowy;
                czesc.KategoriaId = FormKategoriaId;
                czesc.Cena = cena;
                czesc.StanMagazynowy = stan;

                db.SaveChanges();

                Wczytaj();
                Wyczysc();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisu: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Usun()
        {
            if (WybranaCzesc == null)
            {
                MessageBox.Show("Wybierz część z listy.");
                return;
            }

            var wynik = MessageBox.Show(
                $"Czy na pewno usunąć część \"{WybranaCzesc.Nazwa}\"?",
                "Potwierdzenie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (wynik != MessageBoxResult.Yes)
                return;

            try
            {
                using var db = new CarPartsShopContext();
                var czesc = db.Czesci.Find(WybranaCzesc.Id);
                if (czesc != null)
                {
                    db.Czesci.Remove(czesc);
                    db.SaveChanges();
                }

                Wczytaj();
                Wyczysc();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd usuwania: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Wyczysc()
        {
            FormNazwa = string.Empty;
            FormNrKatalogowy = string.Empty;
            FormKategoriaId = 0;
            FormCena = string.Empty;
            FormStan = string.Empty;
            WybranaCzesc = null;
        }
        private void WypelnijFormularz(Czesci czesc)
        {
            if (czesc == null) return;

            FormNazwa = czesc.Nazwa;
            FormNrKatalogowy = czesc.NrKatalogowy;
            FormKategoriaId = czesc.KategoriaId;
            FormCena = czesc.Cena.ToString();
            FormStan = czesc.StanMagazynowy.ToString();
        }

        private bool WalidujFormularz(out decimal cena, out int stan)
        {
            cena = 0;
            stan = 0;

            if (string.IsNullOrWhiteSpace(FormNazwa))
            {
                MessageBox.Show("Podaj nazwę.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(FormNrKatalogowy))
            {
                MessageBox.Show("Podaj numer katalogowy.");
                return false;
            }
            if (FormKategoriaId == 0)
            {
                MessageBox.Show("Wybierz kategorię.");
                return false;
            }
            if (!decimal.TryParse(FormCena, out cena) || cena < 0)
            {
                MessageBox.Show("Podaj poprawną cenę (liczba >= 0).");
                return false;
            }
            if (!int.TryParse(FormStan, out stan) || stan < 0)
            {
                MessageBox.Show("Podaj poprawny stan magazynowy (liczba całkowita >= 0).");
                return false;
            }

            return true;
        }
    }
}
