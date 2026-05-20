using System;
using System.Collections.Generic;

namespace CarPartsStore.Models;

public partial class Czesci
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = null!;

    public string NrKatalogowy { get; set; } = null!;

    public int KategoriaId { get; set; }

    public decimal Cena { get; set; }

    public int StanMagazynowy { get; set; }

    public virtual Kategorie Kategoria { get; set; } = null!;

    public virtual ICollection<PozycjeZamowienia> PozycjeZamowienia { get; set; } = new List<PozycjeZamowienia>();
}
