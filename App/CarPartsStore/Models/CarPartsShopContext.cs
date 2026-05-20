using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CarPartsStore.Models;

public partial class CarPartsShopContext : DbContext
{
    public CarPartsShopContext()
    {
    }

    public CarPartsShopContext(DbContextOptions<CarPartsShopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Czesci> Czesci { get; set; }

    public virtual DbSet<Kategorie> Kategorie { get; set; }

    public virtual DbSet<Klienci> Klienci { get; set; }

    public virtual DbSet<PozycjeZamowienia> PozycjeZamowienia { get; set; }

    public virtual DbSet<Zamowienia> Zamowienia { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=CarPartsShop;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Czesci>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Czesci__3214EC07C42B5DB8");

            entity.ToTable("Czesci");

            entity.Property(e => e.Cena).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Nazwa).HasMaxLength(255);
            entity.Property(e => e.NrKatalogowy).HasMaxLength(255);

            entity.HasOne(d => d.Kategoria).WithMany(p => p.Czescis)
                .HasForeignKey(d => d.KategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Czesci_Kategorie");
        });

        modelBuilder.Entity<Kategorie>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Kategori__3214EC072DEBEDF0");

            entity.ToTable("Kategorie");

            entity.Property(e => e.Nazwa).HasMaxLength(255);
        });

        modelBuilder.Entity<Klienci>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Klienci__3214EC0768516E38");

            entity.ToTable("Klienci");

            entity.HasIndex(e => e.Nip, "UQ__Klienci__C7DEC3C64F635061").IsUnique();

            entity.Property(e => e.NazwaFirmy).HasMaxLength(255);
            entity.Property(e => e.Nip)
                .HasMaxLength(20)
                .HasColumnName("NIP");
        });

        modelBuilder.Entity<PozycjeZamowienia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PozycjeZ__3214EC07E36AB3C0");

            entity.Property(e => e.CenaJednostkowa).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Czesc).WithMany(p => p.PozycjeZamowienia)
                .HasForeignKey(d => d.CzescId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PozycjeZa__Czesc__6A30C649");

            entity.HasOne(d => d.Zamowienie).WithMany(p => p.PozycjeZamowienia)
                .HasForeignKey(d => d.ZamowienieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PozycjeZa__Zamow__693CA210");
        });

        modelBuilder.Entity<Zamowienia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Zamowien__3214EC0797E4DF96");

            entity.Property(e => e.DataZamowienia)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Klient).WithMany(p => p.Zamowienia)
                .HasForeignKey(d => d.KlientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Zamowieni__Klien__66603565");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
