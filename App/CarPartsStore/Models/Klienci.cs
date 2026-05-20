using System;
using System.Collections.Generic;

namespace CarPartsStore.Models;

public partial class Klienci
{
    public int Id { get; set; }

    public string NazwaFirmy { get; set; } = null!;

    public string Nip { get; set; } = null!;

    public virtual ICollection<Zamowienia> Zamowienia { get; set; } = new List<Zamowienia>();
}
