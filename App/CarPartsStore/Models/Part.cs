using System;
using System.Collections.Generic;

namespace CarPartsStore.Models;

public partial class Part
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string CatalogNumber { get; set; } = null!;

    public int CategoryId { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
