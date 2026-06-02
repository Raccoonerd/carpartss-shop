using System;
using System.Collections.Generic;

namespace CarPartsStore.Models;

public partial class Customer
{
    public int Id { get; set; }

    public string CompanyName { get; set; } = null!;

    public string Nip { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
