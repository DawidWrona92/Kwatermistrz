using Kwatermistrz.Core.Konfiguracja;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Tests;

public class TransakcyjnyImportPakietuTests
{
    [Fact]
    public void ScaleniePakietu_WykonywaneNaKopii_NieDotykaOryginalnychAliasowAniSlotow()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var rola = dane.Slowniki.Role.Single(r => r.Nazwa == "Wsparcie");
        var rekonstrukcja = new Rekonstrukcja
        {
            Id = "rek-historyczny",
            Nazwa = "Historia",
            SzablonSlotow = new List<SlotSzablonu> { new() { Id = "slot1", Nazwa = "Mundur" } },
        };
        dane.Rekonstrukcje.Add(rekonstrukcja);
        var pakiet = new PakietKonfiguracji();
        pakiet.Slowniki.Add(new WartoscSlownikaPakietu
        {
            Slownik = "Role",
            Wartosc = new WartoscSlownika { Nazwa = "Wsparcie", Aliasy = new() { "MG-TEST" } },
        });
        pakiet.Rekonstrukcje.Add(new RekonstrukcjaPakietu
        {
            Id = "rek-pakiet",
            Nazwa = "Historia",
            SzablonSlotow = new()
            {
                new() { Nazwa = "Mundur" },
                new() { Nazwa = "Broń szturmowa" },
            },
        });

        var podglad = ImporterPakietuKonfiguracji.PrzygotujPodglad(dane, pakiet);
        var decyzja = podglad.Rekonstrukcje.Single();
        decyzja.Akcja = AkcjaRekonstrukcji.ScalSzablony;

        // Symulujemy awarię po scaleniu, ale jeszcze przed zatwierdzeniem transakcji.
        Assert.Throws<InvalidOperationException>(() =>
            TransakcjaDanych.Wykonaj<bool>(dane, kopia =>
            {
                ImporterPakietuKonfiguracji.Zastosuj(kopia, podglad);
                throw new InvalidOperationException("Zapis się nie udał");
            }));
        Assert.Same(rola, dane.Slowniki.Role.Single(r => r.Id == rola.Id));
        Assert.DoesNotContain("MG-TEST", rola.Aliasy);
        Assert.Single(rekonstrukcja.SzablonSlotow);
        Assert.Equal("slot1", rekonstrukcja.SzablonSlotow[0].Id);

        // Ten sam podgląd powinien zastosować się do kopii i zachować stare Id.
        TransakcjaDanych.Wykonaj(dane, kopia =>
        {
            ImporterPakietuKonfiguracji.Zastosuj(kopia, podglad);
            return true;
        });
        var rolaPoImporcie = dane.Slowniki.Role.Single(r => r.Id == rola.Id);
        Assert.Contains("MG-TEST", rolaPoImporcie.Aliasy);
        var rekPoImporcie = dane.Rekonstrukcje.Single(r => r.Id == "rek-historyczny");
        Assert.Equal(2, rekPoImporcie.SzablonSlotow.Count);
        Assert.Contains(rekPoImporcie.SzablonSlotow, s => s.Nazwa == "Broń szturmowa");
        Assert.Equal(1, dane.SchemaVersion);
    }
}
