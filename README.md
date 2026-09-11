# AniRankApp

.NET MAUI (Android) klijent-server aplikacija za pretragu animea, ostavljanje
recenzija i upravljanje korisnicima.

- **Server podaci:** [Kitsu API](https://kitsu.io/api/edge) (`HttpClient`, `async/await`)
- **Lokalni podaci:** SQLite baza na uređaju (`sqlite-net-pcl`) + `Preferences` za sesiju
- **Arhitektura:** striktan MVVM, `CommunityToolkit.Mvvm`, AppShell + TabBar navigacija

Projekat: `AniRankApp/AniRankApp.csproj` (target `net10.0-android`), solucija `AniRankApp.slnx`.

---

## Pokretanje

1. Otvori `AniRankApp.slnx` u Visual Studio-u.
2. Izaberi Android emulator ili uređaj.
3. F5 (Debug).

Komandna linija:

```
dotnet build AniRankApp/AniRankApp.csproj -c Debug
dotnet build AniRankApp/AniRankApp.csproj -t:Run -f net10.0-android
```

**Podrazumevani Admin nalog** (kreira se pri prvom pokretanju):
`admin` / `admin123`

---

## Struktura projekta

```
AniRankApp/
├── Models/
│   ├── User.cs               SQLite tabela "Users"
│   ├── Review.cs             SQLite tabela "Reviews"
│   ├── KitsuModels.cs        DTO za JSON:API odgovore (KitsuAnimeResponse, KitsuAnimeData, KitsuAttributes, ...)
│   └── Anime.cs              UI model + mapiranje FromKitsu(...)
├── Services/
│   ├── DatabaseService.cs    SQLiteAsyncConnection, kreiranje tabela, seed admina, CRUD
│   ├── AuthService.cs        Registracija, login, sesija u Preferences, logout
│   └── KitsuApiService.cs    GetTrendingAnimeAsync / SearchAnimeAsync / GetAnimeDetailsAsync
│   └── FileExportService.cs  Izvoz recenzija u .json / .txt na lokalno skladište
├── ViewModels/
│   ├── BaseViewModel.cs      ObservableObject, IsBusy/IsNotBusy, Title, ErrorMessage
│   ├── LoginViewModel.cs / RegisterViewModel.cs
│   ├── ExploreViewModel.cs / AnimeDetailViewModel.cs
│   ├── MyReviewsViewModel.cs / ProfileViewModel.cs / AdminViewModel.cs
├── Views/
│   ├── LoginView / RegisterView
│   ├── ExploreView / AnimeDetailView
│   ├── MyReviewsView / ProfileView / AdminView
├── Controls/
│   └── RatingBar.xaml(.cs)   Custom ContentView sa BindableProperty (RatingValue, MaxRating, ShowStars)
├── Converters/Converters.cs  NotEmptyBoolConverter, InvertedBoolConverter
├── Helpers/                  PrefKeys, SecurityHelper (SHA-256), ServiceHelper (DI most)
├── Resources/Styles/
│   ├── Colors.xaml           (iz šablona)
│   ├── Styles.xaml           (iz šablona)
│   └── CustomStyles.xaml     Stilovi kartica, Trigger + DataTrigger za bedž ocene
├── AppShell.xaml(.cs)        Shell + TabBar (Istraži / Recenzije / Profil / Admin*)
├── App.xaml(.cs)            Merge ResourceDictionary-ja, kreira AppShell
└── MauiProgram.cs            DI registracija servisa, VM-ova i View-ova
```

---

## Pokrivenost zahteva sa predavanja

| # | Zahtev | Gde je implementirano |
|---|--------|-----------------------|
| 1 | **Shell + Tabbed navigacija** | `AppShell.xaml` — `Shell` sa `TabBar` i 4 `Tab` elementa; auth ekrani kao zasebni `ShellContent`. Admin tab ima `IsVisible="{Binding IsAdmin}"`. |
| 2 | **MVVM + Data Binding + INotifyPropertyChanged / CommunityToolkit.Mvvm** | Svi `*ViewModel` nasleđuju `BaseViewModel : ObservableObject`; `[ObservableProperty]`, `[RelayCommand]`; View-ovi drže samo `BindingContext` + tanke event handler-e. |
| 3 | **SQLite (sqlite-net-pcl)** | `DatabaseService` — `SQLiteAsyncConnection`, `CreateTableAsync<User>/<Review>`, sve tražene CRUD metode (`InsertReviewAsync`, `GetReviewsForAnimeAsync`, `GetUserReviewsAsync`, `GetAllReviewsAsync`, `DeleteReviewAsync`, `GetAllUsersAsync`, `DeleteUserAsync`). Seed admina pri prvom pokretanju. |
| 4 | **Sesija u lokalne fajlove / Preferences** | `AuthService` čuva `CurrentUserId/Username/Role` u `Preferences`; `AppShell` pri startu čita sesiju i radi auto-login. `FileExportService` piše `.json/.txt` u `FileSystem.AppDataDirectory`. |
| 5 | **Async klijent-server (HttpClient)** | `KitsuApiService` — `HttpClient` + `GetFromJsonAsync`, `CancellationToken`, poziva `trending/anime`, `anime?filter[text]=`, `anime/{id}`. Sve I/O je `async/await`. |
| 6 | **CollectionView + DataTemplate** | `ExploreView`, `MyReviewsView`, `AnimeDetailView` (lista recenzija), `AdminView` (2 liste) — svi koriste `CollectionView` sa `DataTemplate` i `EmptyView`. |
| 7 | **Trigeri i Stilovi (ResourceDictionary)** | `Resources/Styles/CustomStyles.xaml` — `AnimeCardStyle`/`ReviewCardStyle` za kartice; `ScoreBadgeStyle` sa `DataTrigger` (zelena ≥8, žuta 5–7, crvena <5); property `Trigger` na `Entry` (fokus). |
| 8 | **Custom kontrola (ContentView + BindableProperty)** | `Controls/RatingBar.xaml(.cs)` — `BindableProperty` `RatingValue`, `MaxRating`, `ShowStars`; korišćena u `CollectionView` stavkama i u formi za recenziju. |

Dodatno: `ActivityIndicator` spinneri na svim ekranima koji učitavaju sa API-ja;
`RefreshView` pull-to-refresh na listama.

---

## Ekrani

- **LoginView / RegisterView** — validacija polja; posle prijave `GoToAsync("//explore")`,
  Admin dodatno dobija Admin tab.
- **ExploreView** — `SearchBar` (Kitsu pretraga) + `CollectionView` kartica
  (poster, naslov, popularnost, API ocena kroz `RatingBar` i obojeni bedž).
- **AnimeDetailView** — banner + poster + synopsis + status/epizode; forma za
  recenziju (`Slider` 1–10 + `RatingBar` preview + `Editor`); lista SQLite recenzija
  ostalih korisnika.
- **MyReviewsView** — recenzije ulogovanog korisnika + brisanje sopstvenih.
- **ProfileView** — podaci o nalogu i ulozi; izvoz recenzija u `.json`/`.txt`; odjava.
- **AdminView** (samo Admin) — pregled/brisanje svih korisnika i svih recenzija.

---

## Dodatne funkcije (v1.1)

- **Status gledanja** (`Models/WatchStatus.cs`): svaka stavka u "mojoj listi" ima
  status — `Plan to Watch`, `Watching`, `Completed`, `On Hold`, `Dropped`.
  Bira se `Picker`-om u `AnimeDetailView`; ista forma sada radi *update-or-insert*
  (ako već imaš zapis za taj anime, menja se). Komentar je opcioni.
- **Filtriranje "Moje Recenzije"**: horizontalni chip-bar (`CollectionView` +
  `VisualStateManager` Selected state) filtrira listu po statusu; iznad je i
  kratak rezime broja po statusu.
- **Profil**: prikaz raspodele "moje liste" po statusu (`BindableLayout`).
- **Bogatija Explore strana**: `SearchBar` + tri `Picker`-a (Sortiranje /
  Tip / Status) + „Reset“ dugme. Novi `KitsuApiService.GetAnimeAsync(AnimeFilter)`
  gradi `sort=` i `filter[subtype]` / `filter[status]` parametre; kada nema
  filtera koristi se `trending/anime`.
- **Tamna tema**: `UserAppTheme = AppTheme.Dark` u `App.xaml.cs`, tamna paleta i
  Shell/TabBar boje u `CustomStyles.xaml` + `AppShell.xaml`.

## Dodatne funkcije (v1.2)

- **Paginacija na Explore strani**: `CollectionView.RemainingItemsThreshold` +
  `RemainingItemsThresholdReachedCommand` -> `LoadMoreCommand` dohvata sledeću
  stranu (`page[offset]`) i dopisuje je (dedup po `Id`), sa spinnerom u footeru.
  Prva strana je i dalje `trending/anime`, dalje strane idu sa `anime?sort=...`.
- **Ocena zaključana za „Plan to Watch" i „Dropped"**: `WatchStatus.IsRatable(...)`;
  u formi se sekcija ocene sakriva (`CanRate`), a `Rating` se čuva kao `0`.
  Svuda gde se prikazuje recenzija, ocena se prikazuje samo ako `HasRating`,
  inače „Bez ocene".
- **Admin – pretraga korisnika i recenzije po korisniku**: Admin panel sada
  prikazuje samo listu korisnika sa `SearchBar` pretragom (ime/email). Klik na
  „Pogledaj recenzije" otvara zasebnu stranu `AdminUserReviewsView`
  (`adminuserreviews?userId={id}`) sa recenzijama tog korisnika i brisanjem.
  Dugme „Sve recenzije u sistemu" otvara istu stranu sa `userId=0`.

## Dodatne funkcije (v1.3)

- **Banovanje korisnika umesto brisanja**: `User.IsBanned` (nova kolona, auto-migracija
  postojeće baze). Admin panel ima dugme „Banuj/Odbanuj" (`AdminViewModel.ToggleBanCommand`
  -> `DatabaseService.SetUserBannedAsync`) uz badž „BANOVAN" na kartici korisnika.
  Trajno brisanje (`Obriši`) i dalje postoji za slučaj kad je zaista potrebno.
- **Blokada prijave banovanog naloga**: `AuthService.LoginAsync` proverava `IsBanned`
  pre lozinke i vraća poruku „Vaš nalog je uklonjen/banovan od strane administratora.
  Prijava nije moguća." (ne otkriva ništa dodatno napadaču koji pogađa lozinke).
- **Revalidacija aktivne sesije**: ako je Admin banovao korisnika koji je već
  prijavljen na uređaju, `AppShell` pri sledećem pokretanju aplikacije proverava
  status naloga, automatski ga odjavljuje i prikazuje isto obaveštenje.

## Napomene

- Query parametar (`AnimeDetailViewModel` `[QueryProperty("AnimeId","id")]`) se
  primenjuje na `BindingContext` jer se on postavlja u konstruktoru stranice.
- View-ovi imaju i konstruktor bez parametara (`ServiceHelper.GetService<T>()`)
  radi sigurne instancijacije iz `Shell` `ContentTemplate`-a, pored DI konstruktora.
- Lozinke se čuvaju kao SHA-256 heš (`SecurityHelper`).
