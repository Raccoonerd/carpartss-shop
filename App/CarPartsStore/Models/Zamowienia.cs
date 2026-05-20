using System;
using System.Collections.Generic;

namespace CarPartsStore.Models;

public partial class Zamowienia
{
    public int Id { get; set; }

    public DateTime DataZamowienia { get; set; }

    public int KlientId { get; set; }

    public virtual Klienci Klient { get; set; } = null!;

    public virtual ICollection<PozycjeZamowienia> PozycjeZamowienia { get; set; } = new List<PozycjeZamowienia>();
}
