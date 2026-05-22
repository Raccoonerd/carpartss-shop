using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

using CarPartsStore.Models;
using System.Windows;
using CommunityToolkit.Mvvm.Input;

namespace CarPartsStore.ViewModels;

public partial class PozycjeKoszyka : ObservableObject
{
    public int CzescId { get; set; }
    public string NazwaCzesci { get; set; } = string.Empty;
    public decimal CenaJednostkowa { get; set; }

    [ObservableProperty]
    private int ilosc;

    public decimal Suma => Ilosc * CenaJednostkowa;
    partial void OnIloscChanged(int value) => OnPropertyChanged(nameof(Suma));
}

public partial class SprzedazViewModel : ObservableObject
{
    public ObservableCollection<Klienci> Klienci { get; } = new();
    public ObservableCollection<Czesci> DostepneCzesci { get; } = new();
    public ObservableCollection<PozycjeKoszyka> Koszyk { get; } = new();

    [ObservableProperty] private Czesci wybranaCzesc;
    [ObservableProperty] private Klienci wybranyKlient;
    [ObservableProperty] private PozycjeKoszyka wybranaPozycja;

    [ObservableProperty] private string iloscDoDodania = "1";

    [ObservableProperty] private string tekstWyszukiwania;
    partial void OnTekstWyszukiwaniaChanged(string value) => WczytajCzesci();

    public int LiczbaPozycji => Koszyk.Count;
    public decimal SumaCalkowita => Koszyk.Sum(p => p.Suma);

    public SprzedazViewModel()
    {
        WczytajCzesci();
        WczytajKlientow();

        Koszyk.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(LiczbaPozycji));
            OnPropertyChanged(nameof(SumaCalkowita));
        };
    }

    private void WczytajCzesci()
    {
        if (DostepneCzesci == null) return;

        try
        {
            using var db = new CarPartsShopContext();
            var zapytanie = db.Czesci.AsQueryable();

            if (!string.IsNullOrWhiteSpace(tekstWyszukiwania))
            {
                zapytanie = zapytanie.Where(c => c.Nazwa.Contains(tekstWyszukiwania));
            }

            DostepneCzesci.Clear();
            foreach(var c in zapytanie.ToList())
            {
                DostepneCzesci.Add(c);
            }
        }
        catch(Exception ex)
        {
            MessageBox.Show($"Błąd wczytywania części: {ex.Message}");
        }
    }

    private void WczytajKlientow()
    {
        if (Klienci == null) return;

        try
        {
            using var db = new CarPartsShopContext();
            Klienci.Clear();
            foreach(var k in db.Klienci.ToList())
            {
                Klienci.Add(k);
            }
        }
        catch(Exception ex)
        {
            MessageBox.Show($"Błąd wczytywania klientów: {ex.Message}");
        }
    }

    [RelayCommand]
    private void DodajDoKoszyka()
    {
        if(WybranaCzesc ==null)
        {
            MessageBox.Show("Wybierz część z listy.");
            return;
        }

        if(!int.TryParse(IloscDoDodania, out int ilosc) || ilosc <= 0)
        {
            MessageBox.Show("Podaj poprawną ilość (liczba całkowita > 0).");
            return;
        }

        int wKoszyku = Koszyk.Where(p=> p.CzescId == WybranaCzesc.Id).Sum(p => p.Ilosc);

        if(wKoszyku + ilosc > WybranaCzesc.StanMagazynowy)
        {
            MessageBox.Show($"Nie można dodać {ilosc} sztuk. W magazynie jest tylko {WybranaCzesc.StanMagazynowy - wKoszyku} sztuk tej części.");
            return;
        }

        var istniejaca = Koszyk.FirstOrDefault(p => p.CzescId == WybranaCzesc.Id);
        if(istniejaca != null)
        {
            istniejaca.Ilosc += ilosc;
            OnPropertyChanged(nameof(SumaCalkowita));
        }
        else
        {
            Koszyk.Add(new PozycjeKoszyka
            {
                CzescId = WybranaCzesc.Id,
                NazwaCzesci = WybranaCzesc.Nazwa,
                CenaJednostkowa = WybranaCzesc.Cena,
                Ilosc = ilosc
            });
        }

        IloscDoDodania = "1";
    }

    [RelayCommand]
    private void UsunPozycje()
    {
        if(WybranaPozycja == null)
        {
            MessageBox.Show("Wybierz pozycję z koszyka do usunięcia.");
            return;
        }

        Koszyk.Remove(WybranaPozycja);
    }

    [RelayCommand]
    private void WyczyscKoszyk()
    {
        if (Koszyk.Count == 0) return;
        var wynik = MessageBox.Show(
            "Czy na pewno wyczyscić cały koszyk?",
            "Potwierdzenie",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if(wynik == MessageBoxResult.Yes)
        {
            Koszyk.Clear();
        }
    }

    [RelayCommand]
    private void Finalizuj()
    {
        if(WybranyKlient == null)
        {
            MessageBox.Show("Wybierz klienta.");
            return;
        }

        if(Koszyk.Count == 0)
        {
            MessageBox.Show("Koszyk jest pusty.");
            return;
        }

        try
        {
            using var db = new CarPartsShopContext();

            foreach (var pozycja in Koszyk)
            {
                var czesc = db.Czesci.Find(pozycja.CzescId);
                if (czesc == null)
                {
                    MessageBox.Show($"Nie znaleziono w bazie czesci: {pozycja.NazwaCzesci}");
                    return;
                }
                if (czesc.StanMagazynowy < pozycja.Ilosc)
                {
                    MessageBox.Show(
                        $"Brak wystarczającej ilości części \"{czesc.Nazwa}\".\n" +
                        $"W magazynie: {czesc.StanMagazynowy}, w koszyku: {pozycja.Ilosc}.");
                    return;
                }
            }

            var zamowienie = new Zamowienia
            {
                KlientId = WybranyKlient.Id,
                DataZamowienia = DateTime.Now,
            };
            db.Zamowienia.Add(zamowienie);
            db.SaveChanges();

            foreach (var pozycja in Koszyk)
            {
                db.PozycjeZamowienia.Add(new PozycjeZamowienia
                {
                    ZamowienieId = zamowienie.Id,
                    CzescId = pozycja.CzescId,
                    Ilosc = pozycja.Ilosc,
                    CenaJednostkowa = pozycja.CenaJednostkowa
                });

                var czesc = db.Czesci.Find(pozycja.CzescId);
                czesc.StanMagazynowy -= pozycja.Ilosc;
            }

            db.SaveChanges();

            MessageBox.Show(
                $"Zamówienie nr {zamowienie.Id} zostało zapisane.\n" +
                $"Suma: {SumaCalkowita:N2} zl",
                "Sukces",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Koszyk.Clear();
            WybranyKlient = null;
            WczytajCzesci();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd podczas finalizacji zamówienia: {ex.Message}");
        }
    }
}
