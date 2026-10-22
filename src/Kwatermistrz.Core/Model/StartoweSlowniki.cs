namespace Kwatermistrz.Core.Model;

/// <summary>Wartości startowe słowników z Załącznika B. Tworzą pustą aplikację gotową do konfiguracji (FR-1401, NFR-15).</summary>
public static class StartoweSlowniki
{
    /// <summary>Tworzy słowniki z wartościami startowymi Załącznika B.</summary>
    public static Slowniki Utworz()
    {
        var s = new Slowniki();

        s.Strony.AddRange(new[]
        {
            new WartoscSlownika { Nazwa = "Rosja" },
            new WartoscSlownika { Nazwa = "Polska" },
            new WartoscSlownika { Nazwa = "NATO" },
        });

        s.Miejsca.AddRange(new[]
        {
            new WartoscSlownika { Nazwa = "Piwnica" },
            new WartoscSlownika { Nazwa = "Dom" },
        });

        s.Role.AddRange(new[]
        {
            new WartoscSlownika { Nazwa = "Szturm" },
            new WartoscSlownika { Nazwa = "DMR" },
            new WartoscSlownika { Nazwa = "Wsparcie", Aliasy = new() { "LKM" } },
            new WartoscSlownika { Nazwa = "CQB" },
            new WartoscSlownika { Nazwa = "Boczna" },
        });

        s.TypyMagazynkow.AddRange(Nazwy(new[]
        {
            "AK", "AK 74M", "AK 74", "AK 12", "AK-47", "AKM", "RPK", "RPK-74/Drum", "VSS/VAL", "WSS/Wał",
            "PP-19 Vityaz", "PP Vityaz", "SWD", "PKM", "STANAG", "Beryl",
        }, new Dictionary<string, string[]> { ["PP Vityaz"] = new[] { "PP-19 Vityaz" } }));

        s.Napedy.AddRange(new[]
        {
            new WartoscSlownika { Nazwa = "Elektryczna (AEG)" },
            new WartoscSlownika { Nazwa = "HPA" },
        });

        s.StanyReplik.AddRange(Nazwy(new[]
        {
            "Stock", "Do zrobienia", "Do dokończenia", "Ideał", "Ideał – sprawdzony w boju", "Wyśmienity", "W częściach",
        }, null));

        s.Pochodzenie.AddRange(new[]
        {
            new WartoscSlownika { Nazwa = "Orgo/Reko", Aliasy = new() { "Orgo/reko" } },
            new WartoscSlownika { Nazwa = "Wojskowe" },
            new WartoscSlownika { Nazwa = "ASG niskiej jakości" },
        });

        s.RodzajeKosztow.AddRange(new[]
        {
            new WartoscSlownika { Nazwa = "Zakup repliki", Semantyka = nameof(SemantykaKosztu.Zakup) },
            new WartoscSlownika { Nazwa = "Ulepszenie", Semantyka = nameof(SemantykaKosztu.Ulepszenie) },
            new WartoscSlownika { Nazwa = "Serwis", Semantyka = nameof(SemantykaKosztu.Serwis) },
            new WartoscSlownika { Nazwa = "Zwrot / rabat / sprzedaż części", Semantyka = nameof(SemantykaKosztu.Zwrot) },
            new WartoscSlownika { Nazwa = "Inne", Semantyka = nameof(SemantykaKosztu.Inne) },
        });

        s.RodzajeZadan.AddRange(new[]
        {
            new WartoscSlownika { Nazwa = "Awaria", Semantyka = nameof(SemantykaZadania.Awaria) },
            new WartoscSlownika { Nazwa = "Do zrobienia", Semantyka = nameof(SemantykaZadania.DoZrobienia) },
            new WartoscSlownika { Nazwa = "Do kupienia", Semantyka = nameof(SemantykaZadania.DoKupienia) },
        });

        s.Kategorie.AddRange(new (string Nazwa, bool Nosnik, bool Mundur)[]
        {
            ("Czapki", false, false),
            ("Hełmy", false, false),
            ("Szalokominiarki", false, false),
            ("Mundury", false, true),
            ("Kamizelki", true, false),
            ("Pasoszelki i chest rigi", true, false),
            ("Pasy", true, false),
            ("Nakolanniki", false, false),
            ("Plecaki", true, false),
            ("Panele szturmowe", true, false),
            ("Ładownice", false, false),
            ("Rękawice", false, false),
            ("Gogle", false, false),
            ("Buty", false, false),
        }.Select((k, i) => new Kategoria { Nazwa = k.Nazwa, CzyNosnik = k.Nosnik, CzyMundur = k.Mundur, Kolejnosc = i }));

        s.Wzory.AddRange(new (string Nazwa, string[] Kolory, string[]? Aliasy, bool Kolor)[]
        {
            ("EMR", new[] { "#6B7A4F", "#8A9464", "#4A5540", "#A8B07A" }, null, false),
            ("EMR Wiosna/Jesień", new[] { "#9AA86B", "#B5BE8A", "#6B7A4F" }, new[] { "Wiosna/Jesień", "wiosna/jesien", "6sh122 wiosna" }, false),
            ("Chiński EMR", new[] { "#5E6B47", "#7A8455", "#43503A" }, null, false),
            ("Liuks EMR", new[] { "#77855A", "#93A06E", "#59663F" }, null, false),
            ("Multicam", new[] { "#B8A77E", "#8E9A6B", "#6E7A55", "#D6C9A8" }, null, false),
            ("Multicam Tropic G2", new[] { "#5E7A4A", "#7A9460", "#41603A", "#A8BE8A" }, null, false),
            ("Multicam G3", new[] { "#9A8E6E", "#7A8460", "#B5A988", "#5E6848" }, new[] { "ATACS G3", "Atacs g3" }, false),
            ("A-TACS", new[] { "#8A9464", "#6B7A55", "#B0BA8E", "#4A5540" }, new[] { "ATACS" }, false),
            ("A-TACS FG", new[] { "#7A8A5E", "#5E7048", "#9AA878", "#42502F" }, new[] { "ATACS-FG", "ATACS FG" }, false),
            ("Spektr", new[] { "#7E8C7A", "#9AAA98", "#5E6E5C", "#B5C0B0" }, new[] { "Spectre", "Spektr SKWO", "Spectra" }, false),
            ("VSR-98", new[] { "#8A9478", "#A8B098", "#6E7A60", "#C0C8B0" }, new[] { "VSR. 98", "VSR 98" }, false),
            ("Woodland", new[] { "#4A5A3A", "#6B7A50", "#2F3A28", "#8A9A70" }, null, false),
            ("KZS", new[] { "#5A6B7A", "#7A8A9A", "#42505E", "#9AA8B4" }, null, false),
            ("Palma", new[] { "#A89858", "#C0B070", "#7A6838", "#D8CC90" }, null, false),
            ("Izlom / Skol", new[] { "#8A8458", "#6B684A", "#A8A070", "#4A4838" }, null, false),
            ("Surpat Aride Savanna", new[] { "#B8A878", "#9A8A60", "#D0C090", "#7A6A48" }, new[] { "Aride", "Surpat Savanna", "SURPAT SAVANNA" }, false),
            ("SS Lieto", new[] { "#9AAA88", "#7A8A68", "#B5C0A0", "#5E6E50" }, new[] { "Gaszony" }, false),
            ("Flecktarn", new[] { "#8A9468", "#6B7050", "#A8A878", "#42482F" }, new[] { "Fleck", "Fleck tarn" }, false),
            ("WZ.93", new[] { "#8A8A6E", "#A0A084", "#6E6E58", "#5A5A48" }, new[] { "WZ. 93", "WZ 93" }, false),
            ("WZ.2019 124T", new[] { "#7A8A6E", "#98A88A", "#5E6E56", "#B0BC98" }, new[] { "WZ. 2019 124T", "WZ 2019 124T" }, false),
            ("WZ.2019 124P", new[] { "#A89A78", "#8A7A5E", "#C0B090", "#6E6248" }, new[] { "WZ. 2019 124P", "WZ 2019 124P" }, false),
            ("DKWS", new[] { "#4A5A6E", "#5E6E82", "#3A4656", "#7A8A9E" }, new[] { "Wz. 93 DKWS" }, false),
            ("Olive", new[] { "#6B7A3F" }, new[] { "Górka" }, true),
            ("Green", new[] { "#4A6B3A" }, null, true),
        }.Select(w => new WzorKamuflazu
        {
            Nazwa = w.Nazwa,
            KoloryProbki = w.Kolory.ToList(),
            Aliasy = w.Aliasy?.ToList() ?? new(),
            CzyKolorJednolity = w.Kolor,
        }));

        return s;
    }

    private static IEnumerable<WartoscSlownika> Nazwy(string[] nazwy, Dictionary<string, string[]>? aliasy)
    {
        foreach (var nazwa in nazwy)
            yield return new WartoscSlownika
            {
                Nazwa = nazwa,
                Aliasy = aliasy != null && aliasy.TryGetValue(nazwa, out var a) ? a.ToList() : new(),
            };
    }

    /// <summary>Szablon slotów zestawu z Załącznika B (FR-702). Domyślnie slot „Mundur” jest slotem munduru, a sloty broni są slotami broni głównej.</summary>
    public static List<SlotSzablonu> StartoweSloty()
    {
        string[] nazwy = { "Mundur", "Nakolanniki", "Kominiarka", "Rękawice", "Hełm", "Czapka", "Gogle", "Buty", "Kamizelka", "Plecak", "Pas", "Broń DMR", "Broń szturmowa", "Broń wsparcia", "Broń boczna" };
        string[] slotyBroni = { "Broń DMR", "Broń szturmowa", "Broń wsparcia" };
        return nazwy.Select((nazwa, i) => new SlotSzablonu
        {
            Nazwa = nazwa,
            Kolejnosc = i,
            CzySlotMunduru = nazwa == "Mundur",
            CzySlotBroniGlownej = slotyBroni.Contains(nazwa),
        }).ToList();
    }

    /// <summary>Szablon karty technicznej AEG z parametrami w kolejności z arkusza (FR-401) plus trzy domyślnie ukryte (FR-402).</summary>
    public static SzablonKarty SzablonKartyAeg(Slowniki slowniki)
    {
        var napedAeg = Slowniki.Znajdz(slowniki.Napedy, "Elektryczna (AEG)");
        string[] parametry =
        {
            "Body", "Lufa", "Gumka Hop-Up", "Komora Hop-Up", "Doszczelnienie", "Podkładkowanie", "Przewody",
            "Łożyska", "Zębatki", "Prowadnica sprężyny", "Tłok", "Głowica tłoka", "Cylinder",
            "Głowica cylindra", "Dysza", "Silnik", "MOSFET", "Wtyczka", "Sprężyna główna (aktualna)",
            "Kulki docelowe",
        };
        string[] ukryte = { "Długość lufy", "Szybkostrzelność (ROF)", "Akumulator" };
        var lista = parametry.Select((nazwa, i) => new ParametrKarty { Nazwa = nazwa, Kolejnosc = i })
            .Concat(ukryte.Select((nazwa, i) => new ParametrKarty { Nazwa = nazwa, Kolejnosc = parametry.Length + i, Ukryty = true }))
            .ToList();
        return new SzablonKarty { Nazwa = "AEG", NapedId = napedAeg?.Id, Parametry = lista };
    }

    /// <summary>Szablon karty HPA — parametry mechanizmu elektrycznego są w nim, ale import oznacza je jako „nie dotyczy” (FR-403, §13.5).</summary>
    public static SzablonKarty SzablonKartyHpa(Slowniki slowniki)
    {
        var napedHpa = Slowniki.Znajdz(slowniki.Napedy, "HPA");
        string[] parametry = { "Body", "Lufa", "Gumka Hop-Up", "Komora Hop-Up", "Sprężyna główna (aktualna)", "Kulki docelowe", "Zawór / regulator", "Butla / zbiornik" };
        return new SzablonKarty
        {
            Nazwa = "HPA",
            NapedId = napedHpa?.Id,
            Parametry = parametry.Select((nazwa, i) => new ParametrKarty { Nazwa = nazwa, Kolejnosc = i }).ToList(),
        };
    }
}
