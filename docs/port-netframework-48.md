# Kwatermistrz — port na .NET Framework 4.8

Ten branch to port aplikacji Kwatermistrz z .NET 10 na **.NET Framework 4.8**, bez zmian logiki, działania i funkcjonalności. Port jest eksperymentalnym wariantem — główne linia rozwoju (branch `main`) pozostaje na .NET 10.

## Co się zmieniło względem main

| Obszar | .NET 10 (main) | .NET Framework 4.8 (ten branch) |
|---|---|---|
| TargetFramework | `net10.0` / `net10.0-windows` | `net48` (Core, App, testy) |
| WPF-UI | 4.0.3 (tylko .NET 6+) | **3.0.5** — ostatnia wersja z targetem net472/48 |
| System.Text.Json | w BCL | pakiet `System.Text.Json` 8.0.5 |
| DI | Microsoft.Extensions.DependencyInjection 10.x | 2.2.0 (ostatnia z natywnym net48) |
| DateOnly | BCL | **shim** (`src/Kwatermistrz.Core/ShimsNet48.cs`) — struct o identycznej semantyce (parse/format yyyy-MM-dd, AddDays, FromDateTime) |
| ToHashSet / Contains(char) / StartsWith(char) / TryAdd | BCL | rozszerzenia (`RozszerzeniaNet48.cs`) |
| Range/Index (`[..n]`, `[n..]`, `[^n..]`) | C# 12 | zamienione na `Substring` (net48 nie ma Range/Index na string) |
| File.Move(overwrite) / Path.GetRelativePath | BCL | pomocniki `PlikiNet48.PrzeniesNadpisujac` / `PlikiNet48.SciezkaWzgledna` (przez Uri) |
| StringSplitOptions.TrimEntries | BCL | `Split(...).Select(c => c.Trim()).Where(c => c.Length > 0)` |
| `init`/`record` | natywnie | shim `IsExternalInit` (C# 9 na net48) |
| Analyzery / TreatWarningsAsErrors | włączone | wyłączone (net48 + analyzery SDK dają fałszywe alarmy) |

## Budowanie

Wymagania: .NET Framework 4.8 Developer Pack, SDK .NET (nowy format csproj działa z `dotnet build`, ale WPF + net48 najlepiej budować w **Visual Studio 2022**).

```powershell
dotnet build Kwatermistrz.slnx          # lub build w VS 2022
dotnet test tests/Kwatermistrz.Core.Tests/Kwatermistrz.Core.Tests.csproj
dotnet run --project src/Kwatermistrz.App/Kwatermistrz.App.csproj   # Windows
```

## Znane ograniczenia portu

- **WPF-UI 3.0.5** jest starsza niż 4.0.3 — wygląd może się minimalnie różnić; symbol ikon użytych w XAML istnieje w tej wersji (Fluent System Icons).
- Shim `DateOnly` obsługuje formaty używane w aplikacji (`yyyy-MM-dd` w JSON, `dd.MM.yyyy` w UI); pozostałe formaty deleguje do `DateTime.ToString`.
- Publikacja self-contained nie istnieje dla net48 — aplikacja wymaga zainstalowanego .NET Framework 4.8 (jest w Windows 10/11 domyślnie).
- `EnableWindowsTargeting` nie dotyczy net48 — build WPF wymaga Windows (lub `EnableWindowsTargeting` ze starszym SDK, co nie było testowane).

## Dane

Format danych (`%AppData%\Kwatermistrz\dane.json`) jest **identyczny** jak w wersji .NET 10 — ten sam schemat JSON, te same migracje. Katalog danych można przenosić między instalacjami obu wariantów.
