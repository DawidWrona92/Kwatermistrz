# Decyzja 06: sposób instalacji i aktualizacji

## Kontekst
NFR-10: instalacja i aktualizacja bez uprawnień administratora, możliwa dla osób bez wiedzy technicznej. NFR-06: aktualizacja nie niszczy danych. NFR-15: dane autora nie wchodzą do instalatora.

## Wybór
Publikacja **self-contained, pojedynczy plik wykonywalny** (`dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true`), dostarczany jako archiwum zip z instrukcją: rozpakować, uruchomić `Kwatermistrz.exe`, opcjonalnie skrót na pulpit. Dane aplikacji w `%AppData%\Kwatermistrz\` — poza katalogiem programu, więc aktualizacja (podmiana exe) nie rusza danych (NFR-06). Przy pierwszym starciu po aktualizacji — migracja schematu z kopią zapasową (FR-1107).

## Alternatywy
- **MSIX / ClickOnce**: wymagają certyfikatów lub mają ograniczenia poza Store; ClickOnce przestarzały. Odrzucone dla prywatnego użytku w gronie znajomych (Q18: kod niepubliczny, użytkownik prywatny).
- **Instalator Inno Setup**: wymaga admina przy instalacji do Program Files; zbędna warstwa przy jednym pliku exe.

## Konsekwencje
- Rozmiar paczki ~70–90 MB (runtime wbudowany) — akceptowalne offline.
- Aktualizacja = nowy zip; użytkownik zastępuje plik (dane osobno).
- Brak autoaktualizacji — celowe (offline, NFR-02, prywatne rozpowszechnianie).
