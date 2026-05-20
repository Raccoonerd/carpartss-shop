using System;
using System.Collections.Generic;

namespace CarPartsStore.Models;

public partial class Kategorie
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = null!;

    public virtual ICollection<Czesci> Czescis { get; set; } = new List<Czesci>();
}
