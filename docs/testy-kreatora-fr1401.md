# FR-1401 — testy kreatora pierwszego uruchomienia

**Zakres:** poprawa widoczności etykiet i pól w jasnym/ciemnym motywie, poprawne powiązania opcji oraz natywny pasek tytułu.

**Uwaga:** testuj na kopii całego `%APPDATA%\Kwatermistrz`. Nie usuwaj rzeczywistej bazy tylko po to, by zobaczyć kreator; przygotuj osobny testowy profil systemu lub bezpieczną kopię i odtwórz dane po teście.

## Regresje

- [ ] W motywie **ciemnym** wyraźnie widać nagłówek, wszystkie etykiety, podpowiedzi, pola, pozycje listy oraz aktywny przycisk „Rozpocznij”.
- [ ] W motywie **jasnym** tekst i pola mają wyraźny kontrast (w tym zaznaczone i podświetlone pozycje listy).
- [ ] Trzy przyciski radiowe mają **widoczne napisy**: „Pusta (minimalna)”, „Przykład — współczesna Rosja (kategorie i sloty)”, „Import z arkusza Excel…”. To regresja `ValueTuple` → brak właściwości `Nazwa`/`Klucz` w WPF.
- [ ] Domyślnie jest zaznaczona opcja „Pusta”. Kliknięcie „Przykład” zmienia wybór, a „Import” pokazuje pole wyboru pliku.
- [ ] Kliknięcie poszczególnych radio nie wywołuje błędu `MultiBinding.ConvertBack` (wiązanie jest `Mode=OneWay`, zmianę obsługuje `Start_Checked`).
- [ ] Lista „Strona (opcjonalnie)” po rozwinięciu pokazuje tekst każdej strony. Napisy są czytelne również podczas podświetlenia i wyboru.
- [ ] Tekst w polu nazwy rekonstrukcji i znak kursora są czytelne, również przy aktywnym polu.
- [ ] „Rozpocznij” bez wymaganej nazwy wyświetla komunikat, a nie zamyka kreatora.
- [ ] Pasek tytułu daje się przeciągać myszą; klawisz **minimalizuj** działa i pozwala przywrócić okno z paska zadań; **X** anuluje kreator i zamyka aplikację bez zapisu pustej bazy.
- [ ] Kliknięcie „Rozpocznij” z poprawną nazwą zatwierdza kreator; po ponownym uruchomieniu widoczny jest pulpit, nie kreator.
- [ ] Poprzednie poprawki 1.1–1.3 nadal działają bez zmiany `SchemaVersion` i danych.

## Automatyczna weryfikacja

- W `tests/Kwatermistrz.App.Smoke/Program.cs` są kontrole trzech dostępnych etykiet przez **prawdziwy WPF Binding** oraz `WindowStyle`, `ResizeMode` i `ShowInTaskbar`.
- **Pełny build i test GUI na Windows wymagają wykonania po odzyskaniu CI albo lokalnie**. Z powodu limitu GitHub Actions Draft PR nie uruchamia joba automatycznie.
- Lokalny proces bez Visual Studio: `powershell -ExecutionPolicy Bypass -File .\scripts\build-windows-local.ps1`, po zainstalowaniu .NET SDK 10.
