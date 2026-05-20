INSERT INTO Kategorie (Nazwa) VALUES (N'Hamulce'), (N'Filtry'), (N'Oleje');

INSERT INTO Czesci (Nazwa, NrKatalogowy, KategoriaId, Cena, StanMagazynowy)
VALUES (N'Klocki hamulcowe', N'BR-100', 1, 149.99, 20),
       (N'Filtr oleju', N'FO-22', 2, 29.50, 50),
       (N'Olej 5W30', N'OL-5W30', 3, 89.00, 15);

INSERT INTO Klienci (NazwaFirmy, NIP)
VALUES (N'Auto Serwis Kowalski', N'1234567890');
GO