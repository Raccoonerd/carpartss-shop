using System;
using System.Collections.Generic;

namespace CarPartsStore.Models;

public partial class PozycjeZamowienia
{
    public int Id { get; set; }

    public int ZamowienieId { get; set; }

    public int CzescId { get; set; }

    public int Ilosc { get; set; }

    public decimal CenaJednostkowa { get; set; }

    public virtual Czesci Czesc { get; set; } = null!;

    public virtual Zamowienia Zamowienie { get; set; } = null!;
}
