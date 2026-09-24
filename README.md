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
│   ├── Follow.cs             SQLite tabela "Follows" (ko koga prati)
│   ├── ReviewLike.cs         SQLite tabela "ReviewLikes" ("korisno" glasovi)
│   ├── CachedAnime.cs        SQLite tabela "CachedAnime" (offline keš Explore strane)
│   ├── CommunityRankItem.cs  Stavka top liste (prosek lokalnih ocena po animeu)
│   ├── KitsuModels.cs        DTO za JSON:API odgovore (KitsuAnimeResponse, KitsuAnimeData, KitsuAttributes, ...)
│   └── Anime.cs              UI model + mapiranje FromKitsu(...)
├── Services/
│   ├── DatabaseService.cs    SQLiteAsyncConnection, kreiranje tabela, seed admina, CRUD
│   ├── AuthService.cs        Registracija, login, sesija u Preferences, logout
│   └── KitsuApiService.cs    GetTrendingAnimeAsync / SearchAnimeAsync / GetAnimeDetailsAsync
├── ViewModels/
│   ├── BaseViewModel.cs      ObservableObject, IsBusy/IsNotBusy, Title, ErrorMessage
│   ├── LoginViewModel.cs / RegisterViewModel.cs
│   ├── ExploreViewModel.cs / AnimeDetailViewModel.cs
│   ├── MyReviewsViewModel.cs / ProfileViewModel.cs / AdminViewModel.cs
│   ├── CommunityViewModel.cs / UserProfileViewModel.cs / TopListViewModel.cs
├── Views/
│   ├── LoginView / RegisterView
│   ├── ExploreView / AnimeDetailView
│   ├── MyReviewsView / ProfileView / AdminView
│   ├── CommunityView / UserProfileView / TopListView
├── Controls/
│   ├── RatingBar.xaml(.cs)   Custom ContentView sa BindableProperty (RatingValue, MaxRating, ShowStars)
│   ├── RatingPicker.cs       Unos ocene tapom na 1–10 (two-way Value)
│   ├── ChipGroup.cs          Čipovi sa jednim izborom (ItemsSource + two-way SelectedItem)
│   ├── ErrorBanner.cs        Plutajuća poruka o grešci / offline stanju
│   ├── EmptyState.cs         Prazno stanje liste (ikonica, poruka, dugme)
│   └── SkeletonList.cs       Skeleton kartice pri prvom učitavanju
├── Converters/Converters.cs  NotEmptyBoolConverter, InvertedBoolConverter, InitialsConverter
├── Helpers/                  PrefKeys, SecurityHelper (SHA-256), ServiceHelper (DI most), ToastHelper, PasswordVisibility
├── Resources/Styles/
│   ├── Colors.xaml           (iz šablona, usklađen Primary/OffBlack)
│   ├── Styles.xaml           (iz šablona)
│   └── CustomStyles.xaml     Paleta, tipografija, ikonice, stilovi kartica, Trigger + DataTrigger za bedž ocene
├── AppShell.xaml(.cs)        Shell + TabBar (Istraži / Zajednica / Top lista / Profil / Admin* / Odjava)
├── App.xaml(.cs)            Merge ResourceDictionary-ja, kreira AppShell
└── MauiProgram.cs            DI registracija servisa, VM-ova i View-ova
```

---

## Pokrivenost zahteva sa predavanja

| # | Zahtev | Gde je implementirano |
|---|--------|-----------------------|
| 1 | **Shell + Tabbed navigacija** | `AppShell.xaml` — `Shell` sa `TabBar` i 6 `Tab` elemenata sa ikonicama (`FontImageSource`); auth ekrani kao zasebni `ShellContent`. Admin tab ima `IsVisible="{Binding IsAdmin}"`. |
| 2 | **MVVM + Data Binding + INotifyPropertyChanged / CommunityToolkit.Mvvm** | Svi `*ViewModel` nasleđuju `BaseViewModel : ObservableObject`; `[ObservableProperty]`, `[RelayCommand]`; View-ovi drže samo `BindingContext` + tanke event handler-e. |
| 3 | **SQLite (sqlite-net-pcl)** | `DatabaseService` — `SQLiteAsyncConnection`, `CreateTableAsync<User>/<Review>/<Follow>`, sve tražene CRUD metode (`InsertReviewAsync`, `GetReviewsForAnimeAsync`, `GetUserReviewsAsync`, `GetAllReviewsAsync`, `DeleteReviewAsync`, `GetAllUsersAsync`, `DeleteUserAsync`). Seed admina pri prvom pokretanju. |
| 4 | **Sesija u lokalne fajlove / Preferences** | `AuthService` čuva `CurrentUserId/Username/Role` u `Preferences`; `AppShell` pri startu čita sesiju i radi auto-login. SQLite baza (`anirank.db3`) je lokalni fajl u `FileSystem.AppDataDirectory`. |
| 5 | **Async klijent-server (HttpClient)** | `KitsuApiService` — `HttpClient` + `GetFromJsonAsync`, `CancellationToken`, poziva `trending/anime`, `anime?filter[text]=`, `anime/{id}`. Sve I/O je `async/await`. |
| 6 | **CollectionView + DataTemplate** | `ExploreView` (`GridItemsLayout`), `MyReviewsView`, `CommunityView` (2 liste), `TopListView`, `AdminView`, `AdminUserReviewsView` — svi koriste `CollectionView` sa `DataTemplate` i `EmptyView` (`EmptyState`). Recenzije na `AnimeDetailView` koriste `BindableLayout` + `DataTemplate`. |
| 7 | **Trigeri i Stilovi (ResourceDictionary)** | `Resources/Styles/CustomStyles.xaml` — `AnimeCardStyle`/`ReviewCardStyle` za kartice; `ScoreBadgeStyle` sa `DataTrigger` (zelena ≥8, žuta 5–7, crvena <5); property `Trigger` na `Entry` (fokus). |
| 8 | **Custom kontrola (ContentView + BindableProperty)** | `Controls/RatingBar.xaml(.cs)` — `BindableProperty` `RatingValue`, `MaxRating`, `ShowStars`; korišćena u `CollectionView` stavkama. Uz nju: `RatingPicker` (two-way `Value`), `ChipGroup` (two-way `SelectedItem`), `ErrorBanner`, `EmptyState`, `SkeletonList`. |

Dodatno: skeleton kartice i `ActivityIndicator` spinneri pri učitavanju;
`RefreshView` pull-to-refresh na listama.

---

## Ekrani

- **LoginView / RegisterView** — validacija polja; posle prijave `GoToAsync("//explore")`,
  Admin dodatno dobija Admin tab.
- **ExploreView** — `SearchBar` (Kitsu pretraga) + filter „pill"-ovi + `CollectionView`
  kartica (poster, naslov, „TV · 2013 · 25 ep · Završeno", obojeni bedž Kitsu ocene);
  na širokom prozoru mreža od 2–4 kolone.
- **AnimeDetailView** — hero (cover sa gradijentom + poster), kartica „Moja lista",
  opis sa „Prikaži više"; forma za recenziju u donjem panelu (čipovi za status,
  `RatingPicker` 1–10, epizode, `Editor`); lista SQLite recenzija ostalih korisnika.
- **MyReviewsView** — recenzije jednog profila (otvara se sa profila, ruta
  `reviews?userId=...&status=...`); brisanje samo na sopstvenoj listi.
- **CommunityView** — pretraga ostalih korisnika, čipovi Svi / Pratim / Prate me,
  dugme Zaprati/Otprati; klik na korisnika otvara njegov javni profil.
- **UserProfileView** — javni profil drugog korisnika: brojači pratilaca, dugme za
  praćenje i raspodela po statusu koja vodi na njegove recenzije.
- **ProfileView** — podaci o nalogu i ulozi, brojači pratilaca, klikabilna
  raspodela po statusu (vodi na te recenzije).
- **AdminView** (samo Admin) — pregled/brisanje svih korisnika i svih recenzija.
- **LogoutView** (tab „Odjava") — potvrda odjave; „Otkaži" vraća na prethodni tab.

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

## Dodatne funkcije (v1.4)

- **Tab „Zajednica" umesto taba „Recenzije"**: `CommunityView` prikazuje sve
  ostale (nebanovane) korisnike sa `SearchBar` pretragom po imenu i čipovima
  **Svi / Pratim / Prate me**.
- **Praćenje korisnika**: nova tabela `Follows` (`Models/Follow.cs`) i metode
  `FollowAsync` / `UnfollowAsync` / `GetFollowingIdsAsync` / `GetFollowerIdsAsync`
  u `DatabaseService`. Praćenje je jednosmerno — svako može da zaprati nazad,
  pa kartica dobije bedž „Prati te". Profil prikazuje brojače pratilaca.
- **Recenzije se otvaraju sa profila**: `MyReviewsView` je sad ruta
  `reviews?userId={id}&status={status}` — klik na red u raspodeli po statusu
  (npr. „Completed") otvara baš te recenzije tog profila. Ista strana služi i za
  tuđe profile, samo bez dugmeta „Obriši" (`CanDelete`).
- **Javni profil korisnika**: `UserProfileView` (`userprofile?userId={id}`) —
  brojači, dugme Zaprati/Otprati i raspodela po statusu.
- **Uklonjen izvoz recenzija** u `.json`/`.txt` sa profila (`FileExportService`
  je obrisan).

### Optimizacije (v1.4)

- **Nema više N+1 upita**: `AnimeDetailViewModel` i `AdminUserReviewsViewModel` su
  radili jedan `GetUserByIdAsync` po recenziji; sada se imena autora povuku odjednom
  (`DatabaseService.GetUsernamesAsync()` — projektovan upit, samo `Id, Username`).
- **Brojači preko SQL-a**: `CountFollowersAsync` / `CountFollowingAsync` rade
  `SELECT COUNT(*)` umesto učitavanja svih redova; `GetReviewCountsByUserAsync` i
  `GetFollowerCountsAsync` povlače samo jednu kolonu.
- **Ažuriranje recenzije** više ne povlači ceo spisak recenzija tog animea da bi
  našlo jednu (`GetReviewByIdAsync`).
- **Thread-safe inicijalizacija baze** (`SemaphoreSlim` + objava veze tek kad je
  šema spremna) — dva ekrana koja istovremeno krenu u bazu više ne mogu da naprave
  dve veze ni duplirani seed admina.
- **Unique indeks `(FollowerId, FollowingId)`** + `INSERT OR IGNORE` — dupli tap na
  „Zaprati" ne može da napravi duplu vezu.
- **Debounce na Explore filterima** (350 ms): promena sortiranja/tipa/statusa u
  nizu šalje jedan HTTP zahtev umesto tri.
- **Dedup paginacije u O(1)**: `HashSet` id-jeva se drži u ViewModel-u umesto da se
  gradi iznova pri svakoj sledećoj strani.
- **Keš detalja animea** (poslednjih 50) u `KitsuApiService` — povratak na već
  otvoren anime ne pravi nov HTTP poziv.
- **`ProfileViewModel.LoadAsync` je zaštićen `try/catch`** — poziva se iz
  `OnAppearing` (`async void`), gde bi izuzetak srušio aplikaciju.

## Dodatne funkcije (v1.5)

**Zajednica**
- **Feed** (prvi čip u tabu Zajednica): šta su poslednje ocenili korisnici koje pratiš
  (`DatabaseService.GetFeedAsync`, jedan upit sa `IN` listom). Klik na karticu vodi na taj anime.
- **„Korisno" glasovi na recenzijama**: tabela `ReviewLikes` (unique po `(ReviewId, UserId)`),
  dugme ♡/♥ na tuđim recenzijama i sortiranje recenzija **Najkorisnije / Najnovije /
  Najbolje ocenjene** na strani animea.
- **Bedž za nove pratioce**: tab dobije oznaku „Zajednica ●" kad te neko zaprati od
  poslednjeg pregleda; baner „Imaš N novih pratilaca" + dugme Pogledaj briše oznaku
  (stanje se pamti po nalogu u `Preferences`).
- **Privatan profil** (`User.IsPrivate`, prekidač na Profilu): listu vide samo pratioci
  (vlasnik i Admin uvek). Zaštita važi i na javnom profilu i na strani sa recenzijama.

**Anime**
- **Ocena zajednice** pored Kitsu ocene — prosek ocena datih *u ovoj aplikaciji*
  (`GetAnimeRatingStatsAsync`, `AVG` u SQL-u).
- **Top lista zajednice** (tab `//toplist`): rang-lista animea po lokalnim ocenama,
  sa čipovima „Najbolje ocenjeni" (po proseku) i „Najpopularniji" (po broju ocena).
- **Napredak po epizodama** (`Review.EpisodesWatched`): polje u formi (sakriveno za
  „Plan to Watch") i bedž „Odgledano: N ep." na karticama.
- **Offline režim**: poslednja učitana strana se čuva u tabelu `CachedAnime`; bez
  interneta (ili kad API ne odgovara) Explore prikazuje sačuvani spisak uz obaveštenje
  kada je snimljen.

**Sesija i UX**
- **Uloga se čita iz baze, ne iz `Preferences`**: `AuthService.RefreshSessionAsync()`
  pri startu osvežava `Role`/`Username` iz baze, pa ručno menjanje preferenci ne može
  da doda Admin tab. (Lozinke ostaju SHA-256, po dogovoru.)
- **`Review.UpdatedAt`**: izmena više ne prepisuje `CreatedAt` — datum unosa ostaje, a
  kartica prikazuje „Izmenjeno ...". Sortiranje koristi `LastActivityAt`.
- **Sortiranje moje liste**: Najnovije / Najstarije / Najveća ocena / Najmanja ocena /
  Naziv (A-Š).
- **Swipe-to-delete** na listi recenzija (isključen na tuđoj listi).
- **Toast umesto `DisplayAlert`** za potvrde (`Helpers/ToastHelper.cs` — native Android
  toast, bez dodatnog paketa).
- **Debounce i na Explore pretrazi** (350 ms), isto kao na filterima.

## UI/UX redizajn (v1.6)

- **Navigacija**: umesto bočnog menija — `TabBar` sa Material ikonicama (dole na
  Androidu, gore na Windowsu); Top lista je sopstveni tab; **Odjava** je poslednji tab
  (`LogoutView` sa potvrdom „Odjavi se" / „Otkaži").
- **Istraži**: filteri kao „pill" dugmad (action sheet, primenjuju se odmah), jedna
  ocena po kartici umesto dve, kompaktan red sa podacima, mreža na širokom ekranu.
- **Detalji**: hero zaglavlje, kartica „Moja lista" + donji panel sa formom, tap-ocena
  1–10 (`RatingPicker`) i čipovi za status umesto `Slider`-a i `Picker`-a.
- **Čitljivost**: tamni tekst na bedževima ocena (kontrast ≥ 7:1 umesto ~2:1), veći
  `Caption`, `SemanticProperties` na posterima i ikonicama.
- **Stanja**: skeleton kartice, plutajući `ErrorBanner` (ne pomera raspored), `EmptyState`
  sa akcijom, toast na Windowsu bez modalnog „OK".
- **Login/Registracija**: logo, prikaz/skrivanje lozinke, „Next" prelazi na sledeće
  polje, demo admin nalog se prikazuje samo u Debug buildu.
- **Brend**: nova ikonica, splash i logo; Poppins za naslove, OpenSans za tekst;
  pravilo boja `Primary` (površine) / `PrimaryAccent` (tekst i ikonice).
- **Profil**: avatar sa inicijalima i pločice sa brojevima.

## Napomene

- Query parametar (`AnimeDetailViewModel` `[QueryProperty("AnimeId","id")]`) se
  primenjuje na `BindingContext` jer se on postavlja u konstruktoru stranice.
- View-ovi imaju i konstruktor bez parametara (`ServiceHelper.GetService<T>()`)
  radi sigurne instancijacije iz `Shell` `ContentTemplate`-a, pored DI konstruktora.
- Lozinke se čuvaju kao SHA-256 heš (`SecurityHelper`).
