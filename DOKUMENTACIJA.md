# AniRankApp — Tehnička dokumentacija

Ovaj fajl objašnjava **kako aplikacija radi iznutra**: tok podataka, arhitekturu i
ulogu svakog foldera/fajla. `README.md` je kratak pregled i uputstvo za pokretanje;
ovo je detaljan "vodič kroz kod" — pročitaj ga kad treba da nešto izmeniš ili
objasniš na odbrani projekta.

---

## 1. Princip rada aplikacije (u dve rečenice)

AniRankApp je **klijent-server** MAUI aplikacija: "server" deo su podaci o
animeima koji dolaze sa **Kitsu API-ja** (javni REST servis, samo za čitanje),
a sve što je vezano za **korisnike, sesiju i recenzije** živi **lokalno na
uređaju** u SQLite bazi. Aplikacija ne šalje ništa na sopstveni backend — jedini
mrežni pozivi idu ka `https://kitsu.io/api/edge`.

Arhitektura je striktan **MVVM**:

- **Model** — obične klase koje nose podatke (`Models/`).
- **View** — XAML stranice (`Views/`), bez logike, samo `BindingContext = ViewModel`.
- **ViewModel** — sav UI-state i logika (`ViewModels/`), izložen preko
  `[ObservableProperty]` i `[RelayCommand]` (CommunityToolkit.Mvvm), na koje se
  View vezuje Data Binding-om.
- **Services** — pristup podacima (baza, HTTP, fajl-sistem, sesija) — ViewModel-i
  ih dobijaju kroz **Dependency Injection** (registrovano u `MauiProgram.cs`),
  nikad ih sami ne instanciraju.

Navigacija ide preko **.NET MAUI Shell** — `AppShell.xaml` definiše sve rute i
`TabBar`. ViewModel-i navigiraju pozivom `Shell.Current.GoToAsync("ruta")`,
nikad direktno ne prave `new SomeView()`.

---

## 2. Tok podataka — glavni scenariji

### 2.1 Pokretanje aplikacije i sesija

1. `MauiProgram.CreateMauiApp()` gradi DI kontejner i registruje sve servise,
   ViewModel-e i View-ove kao servise (`AddSingleton`/`AddTransient`).
2. `App` (kroz `CreateWindow`) pravi `new AppShell()`.
3. `AppShell` konstruktor pita `AuthService.IsLoggedIn` — to samo čita da li u
   `Preferences` postoji sačuvan `current_user_id`. Ako postoji → odmah otvara
   `TabBar` (glavni ekrani); ako ne → otvara `LoginView`.
4. Ako sesija postoji, `AppShell` u pozadini (`ValidateSessionAsync`) zove
   `AuthService.RefreshSessionAsync()` — nalog se ponovo pročita **iz baze** i
   `Username`/`Role` u `Preferences` se osvežavaju. `Preferences` su samo keš:
   ručno izmenjena uloga u njima ne može da otključa Admin tab. Ako je korisnik
   u međuvremenu **banovan** ili obrisan, automatski se odjavljuje uz poruku.
5. `DatabaseService` se lenjo inicijalizuje pri prvom pristupu bazi (ne pri
   startu aplikacije) — tada se prave tabele `Users`/`Reviews`/`Follows` i
   ubacuje podrazumevani admin nalog (`admin` / `admin123`), ako još ne postoji.

### 2.2 Registracija / Prijava

1. `RegisterView` → `RegisterViewModel.RegisterCommand` → `AuthService.RegisterAsync`
   validira polja, proverava da li username već postoji (`DatabaseService`), pa
   ubacuje novog `User` reda sa heširanom lozinkom (SHA-256, `SecurityHelper`).
2. `LoginView` → `LoginViewModel.LoginCommand` → `AuthService.LoginAsync`:
   - ako korisnik ne postoji → greška,
   - ako je **banovan** → greška "nalog je uklonjen/banovan",
   - ako je lozinka pogrešna → greška,
   - inače `AuthService.SaveSession(user)` upiše `Id/Username/Role` u
     `Preferences` i vrati uspeh.
3. Nakon uspešnog logina, `AppShell.RefreshTabs()` se poziva da se Admin tab
   pokaže/sakrije zavisno od uloge, pa `GoToAsync("//explore")`.

### 2.3 Istraži (pretraga animea sa Kitsu API-ja)

1. `ExploreView` se pojavi → `OnAppearing` zove
   `ExploreViewModel.InitializeAsync()` → učitava prvu stranu (podrazumevano
   `trending/anime`).
2. Korisnik menja `SearchBar` tekst, ili bilo koji od tri `Picker`-a
   (Sortiranje / Tip / Status) → svaka promena okida ponovno učitavanje od
   prve strane (`LoadFirstPageAsync`), koje gradi `AnimeFilter` i zove
   `KitsuApiService.GetAnimeAsync(filter)`.
2a. Promene filtera i kucanje u pretrazi su **debounce-ovani** (350 ms), pa niz
   izmena šalje jedan zahtev umesto više njih.
2b. **Offline**: ako nema konekcije (`Connectivity`) ili API ne odgovara,
   prikazuje se poslednja sačuvana strana iz tabele `CachedAnime` uz obaveštenje
   kada je snimljena; svaka uspešna prva strana osvežava taj keš.
3. Kad korisnik doskroluje do kraja liste, `CollectionView` javlja
   `RemainingItemsThresholdReachedCommand` → `LoadMoreCommand` dovlači sledeću
   stranu (`page[offset]`) i **dodaje** je na postojeću listu (uz proveru
   duplikata po `Id`) — to je paginacija / infinite scroll.
4. Klik na karticu → `GoToDetailCommand` → `Shell.Current.GoToAsync("AnimeDetailView?id=...")`.

### 2.4 Detalji animea + "moja lista" / recenzija

1. `AnimeDetailView` je registrovan kao Shell ruta; `AnimeDetailViewModel` ima
   `[QueryProperty(nameof(AnimeId), "id")]` — kad Shell otvori stranicu, sam
   postavi `AnimeId`, što (preko `OnAnimeIdChanged`) pokreće `LoadAsync()`.
2. `LoadAsync` paralelno: (a) zove Kitsu `GetAnimeDetailsAsync(id)` za pun opis
   animea, (b) zove `DatabaseService.GetReviewsForAnimeAsync(id)` za sve lokalne
   recenzije tog animea i za svaku dovuče `Username` autora.
3. Ako **trenutni korisnik** već ima svoju recenziju/status za taj anime, forma
   se popuni njegovim postojećim podacima (status, ocena, komentar) i dugme
   promeni tekst u "Ažuriraj moju listu" — dalji Submit radi **update**, ne
   pravi duplikat.
4. Forma: `Picker` za status gledanja (`Plan to Watch / Watching / Completed /
   On Hold / Dropped`). Ako je status **Plan to Watch** ili **Dropped**,
   sekcija za ocenu (`Slider` + `RatingBar`) se sakriva (`CanRate` property) i
   pri snimanju se `Rating` upisuje kao `0` (što znači "bez ocene" —
   `Review.HasRating`).
5. Polje **„Odgledano epizoda"** (`Review.EpisodesWatched`) se prikazuje za sve
   statuse osim „Plan to Watch" (`CanTrackProgress`); prazno polje znači 0.
6. `SubmitReviewCommand` snima (insert ili update) u `Reviews` tabelu preko
   `DatabaseService`, pa ponovo učita listu recenzija za taj anime. **Izmena
   postavlja `UpdatedAt`, a `CreatedAt` ostaje originalni datum unosa.** Potvrda
   ide kao toast (`ToastHelper`), ne kao modalni `DisplayAlert`.
7. Pored Kitsu ocene stoji i **ocena zajednice** — prosek ocena datih u ovoj
   aplikaciji (`GetAnimeRatingStatsAsync`, `AVG` + `COUNT` u SQL-u).
8. Tuđe recenzije imaju dugme **„Korisno" (♡/♥)** → tabela `ReviewLikes`
   (`INSERT OR IGNORE`, unique par). Lista recenzija se sortira po
   **Najkorisnije / Najnovije / Najbolje ocenjene**, sve u memoriji.

### 2.5 Lista recenzija jednog profila

Ova stranica **nije više tab** — otvara se sa profila (svog ili tuđeg), kao
ruta `reviews?userId={id}&status={status}`.

1. `MyReviewsViewModel` je `IQueryAttributable`: iz rute pročita `userId` (ako
   ga nema → trenutni korisnik) i opcioni `status`, koji odmah postavi kao
   izabrani filter čip.
2. `LoadAsync()` povuče recenzije tog korisnika (`GetUserReviewsAsync`),
   izračuna rezime po statusu i primeni filter čip (Sve / Plan to Watch /
   Watching / Completed / On Hold / Dropped).
3. Klik na filter čip menja `SelectedStatusFilter` → lokalno filtrira već
   učitanu listu (bez novog poziva ka bazi).
4. `Picker` **Sortiranje** (Najnovije / Najstarije / Najveća ocena / Najmanja
   ocena / Naziv A-Š) uređuje već učitanu listu, bez novog upita.
5. `CanDelete` je tačno kad je to **moja** lista — samo tad se dugme "Obriši"
   prikazuje i **swipe ulevo** radi (`SwipeView.IsEnabled`). Tuđe recenzije su
   samo za gledanje.
6. Ako je vlasnik liste označio profil kao **privatan**, a ja ga ne pratim (i
   nisam Admin), lista se ne učitava — prikaže se samo poruka `PrivacyNotice`.

### 2.6 Profil

Prikazuje osnovne podatke o nalogu (iz `AuthService`, tj. iz `Preferences`),
broj pratilaca / koliko ih prati, ukupan broj stavki u "mojoj listi" i
raspodelu po statusu.

**Svaki red raspodele je klikabilan** (`TapGestureRecognizer` → `OnStatusTapped`
→ `OpenStatusCommand`): klik na npr. "Completed" otvara stranicu recenzija
(`reviews?userId={ja}&status=Completed`), tj. tačno one recenzije koje su sa
tog profila stavljene u taj status. Dugme "Sve moje recenzije" otvara istu
stranicu bez filtera. Prekidač **„Privatan profil"** (`User.IsPrivate`) se snima
odmah pri promeni, bez dugmeta Sačuvaj. Dugme Odjava briše sesiju iz
`Preferences` i vraća na Login.

### 2.7 Zajednica (feed + praćenje korisnika)

1. `CommunityView` (tab "Zajednica") → `CommunityViewModel.LoadAsync()` povuče
   sve ostale korisnike (`GetOtherUsersAsync` — bez mene i bez banovanih), moju
   listu praćenja (`GetFollowingIdsAsync`), moje pratioce (`GetFollowerIdsAsync`)
   i zbirne brojače (`GetReviewCountsByUserAsync`, `GetFollowerCountsAsync`) —
   sve u par upita, pa se spaja u `CommunityUserItem` stavke.
2. Čipovi biraju sadržaj: **Feed / Svi / Pratim / Prate me**.
   - **Feed** je posebna lista (`GetFeedAsync`) — poslednje stavke korisnika koje
     pratim, sortirane po `MAX(CreatedAt, UpdatedAt)`; klik vodi na taj anime.
   - Ostala tri segmenta prikazuju korisnike; `SearchBar` filtrira po imenu.
     Filtriranje je in-memory (`ApplyFilter`).
3. Dugme "Zaprati"/"Otprati" → `ToggleFollowCommand` → `FollowAsync` /
   `UnfollowAsync` upisuje ili briše red u tabeli `Follows`, pa se feed ponovo
   sastavlja. Praćenje je **jednosmerno**: to što ja pratim nekoga ne znači da on
   prati mene — on isto tako može da me zaprati nazad (bedž "Prati te").
4. **Bedž za nove pratioce**: `CountFollowersSinceAsync(me, FollowersSeenAt)`
   broji veze nastale posle poslednjeg pregleda; ako ih ima, tab postaje
   „Zajednica ●" (`AppShell.SetCommunityBadge`) i prikaže se baner. Dugme
   „Pogledaj" upisuje novi `FollowersSeenAt` (po nalogu, u `Preferences`),
   skida oznaku i prebacuje na segment „Prate me".
5. Klik na karticu korisnika → `userprofile?userId={id}` →
   `UserProfileViewModel`: javni profil sa brojačima, dugmetom za praćenje i
   istom klikabilnom raspodelom po statusu (vodi na recenzije tog korisnika).
   Ako je profil **privatan**, a ja ga ne pratim, umesto liste stoji poruka —
   čim ga zapratim, sadržaj se otključava (`CanSeeList`).
6. Dugme **„Top lista"** otvara `toplist` → `TopListViewModel`: rang-lista animea
   po proseku ocena datih u ovoj aplikaciji (`GetCommunityTopAsync`), sa
   filterom minimalnog broja ocena.

### 2.8 Admin panel (samo za `Role == "Admin"`)

1. `AdminView` prikazuje **samo listu korisnika**, sa pretragom po imenu/emailu
   (`UserSearch` filtrira in-memory listu, bez ponovnog upita ka bazi).
2. Za svakog korisnika: **"Pogledaj recenzije"** vodi na posebnu stranicu
   (`AdminUserReviewsView`, ruta `adminuserreviews?userId={id}`) — recenzije se
   ne prikazuju sve odjednom, već tek kad admin klikne na konkretnog korisnika.
   Dugme "Sve recenzije u sistemu" otvara istu stranicu sa `userId=0`.
3. **"Banuj/Odbanuj"** — umesto brisanja naloga, postavlja `User.IsBanned`.
   Banovan korisnik ostaje u bazi (sa svim recenzijama), ali `AuthService.LoginAsync`
   odbija njegovu sledeću prijavu. "Obriši" i dalje postoji za trajno brisanje
   (korisnik + sve njegove recenzije), ali je sad sekundarna opcija.

---

## 3. Struktura foldera — šta koji fajl radi

```
AniRankApp/
├── App.xaml(.cs)              Ulazna tačka aplikacije, tamna tema, ResourceDictionary
├── AppShell.xaml(.cs)         Shell: rute, TabBar, sesija pri startu
├── MauiProgram.cs             DI kontejner: registracija servisa/VM/View-ova
├── Models/
├── Services/
├── ViewModels/
├── Views/
├── Controls/
├── Converters/
├── Helpers/
├── Resources/Styles/
└── Platforms/Android/         Android-specifičan kod (manifest, MainActivity...)
```

### 3.1 `Models/` — podaci, bez logike osim čistih izračunavanja

| Fajl | Šta predstavlja |
|---|---|
| `User.cs` | SQLite tabela `Users`. Kolone: `Id, Username (unique), Email, PasswordHash, Role, IsBanned, CreatedAt`. `[Ignore]` propertiji (`IsAdmin`, `CreatedAtText`, `BanActionText`) su izračunati u memoriji, ne postoje kao kolone. |
| `Review.cs` | SQLite tabela `Reviews` — u suštini "moja stavka za jedan anime": `Id, UserId, AnimeKitsuId, AnimeTitle, AnimeImageUrl, Rating, Comment, Status, CreatedAt`. `Username` je popunjen tek u memoriji (ko je autor), nije kolona. `RatingTier`/`HasRating`/`StatusDisplay` su izračunati propertiji koje XAML koristi za bedževe i triger boje. |
| `Follow.cs` | SQLite tabela `Follows` — jedna veza "X prati Y": `Id, FollowerId, FollowingId, CreatedAt`. Veza je **usmerena**: da bi praćenje bilo uzajamno, moraju da postoje dva reda. |
| `ReviewLike.cs` | SQLite tabela `ReviewLikes` — "korisno" glas: `Id, ReviewId, UserId, CreatedAt`, par je unique. |
| `CachedAnime.cs` | SQLite tabela `CachedAnime` — offline kopija poslednje Explore strane; `From(Anime, sortOrder)` / `ToAnime()` mapiraju u oba smera. Nije korisnički podatak, briše se pri svakom novom uspešnom učitavanju. |
| `CommunityRankItem.cs` | Stavka top liste (nije tabela): anime + prosek lokalnih ocena + broj glasova + `Rank`. |
| `WatchStatus.cs` | Statičke konstante za status gledanja (`Plan to Watch, Watching, Completed, On Hold, Dropped`) + `IsRatable(status)` (da li sme da ima ocenu) + `Normalize(...)` (stari/NULL redovi → "Completed"). |
| `Anime.cs` | UI-model animea — **nije** SQLite tabela, ovo je "očišćen" oblik podataka sa Kitsu API-ja (`FromKitsu(...)` mapira iz DTO-a). Sadrži i izračunate tekstove za prikaz (`AverageRatingText, EpisodeText, PopularityText, RatingTier`...). |
| `KitsuModels.cs` | Sirovi DTO-ovi za deserijalizaciju Kitsu JSON:API odgovora: `KitsuAnimeResponse` (lista), `KitsuAnimeSingleResponse` (jedan anime), `KitsuAnimeData`, `KitsuAttributes`, `KitsuTitles`, `KitsuImage`. Ovi se **ne** koriste direktno u View-ovima — uvek se prvo mapiraju u `Anime`. |
| `AnimeFilter.cs` | Parametri pretrage za Explore stranu: tekst, sortiranje (`AnimeSort` enum), tip (`subtype`), status (`current/finished/upcoming`), `Limit`/`Offset` za paginaciju. `IsPlainTrending` odlučuje da li se koristi `trending/anime` endpoint ili opšti `anime?...` upit. |

### 3.2 `Services/` — sva "prljava" logika (I/O), uvek `async/await`

| Fajl | Odgovornost |
|---|---|
| `DatabaseService.cs` | Jedino mesto koje direktno priča sa SQLite bazom (`SQLiteAsyncConnection`). Lenja inicijalizacija (`InitAsync`) pravi fajl `anirank.db3` u `FileSystem.AppDataDirectory`, kreira tabele i seed-uje admin nalog. Sve CRUD metode za `User`/`Review`/`Follow` (insert, get, delete, ban, follow/unfollow, zbirni brojači) žive ovde — ViewModel-i nikad ne pišu SQL/LINQ nad bazom direktno. |
| `AuthService.cs` | "Poslovna" logika oko naloga: validacija pri registraciji, provera lozinke (SHA-256) i banovanosti pri loginu, čuvanje/brisanje sesije u `Preferences` (`CurrentUserId/Username/Role`). `RefreshSessionAsync()` ponovo čita nalog iz baze i osvežava keširanu ulogu; takođe pamti kad su pratioci poslednji put pregledani (`FollowersSeenAt`, po nalogu). Ne zna ništa o UI-ju. |
| `KitsuApiService.cs` | Jedini fajl koji zove spoljni HTTP API. Ima `HttpClient` sa `BaseAddress = https://kitsu.io/api/edge/`. Metode: `GetTrendingAnimeAsync`, `SearchAnimeAsync`, `GetAnimeDetailsAsync`, `GetAnimeAsync(AnimeFilter)` (gradi query string sa `filter[...]`, `sort=`, `page[limit]`/`page[offset]`). Sve deserijalizuje u `Anime` preko `Anime.FromKitsu`. |

### 3.3 `ViewModels/` — stanje ekrana + komande (CommunityToolkit.Mvvm)

Svi nasleđuju `BaseViewModel` (`IsBusy`, `IsNotBusy`, `Title`, `ErrorMessage`).
`[ObservableProperty]` generiše property + `INotifyPropertyChanged`;
`[RelayCommand]` generiše `ICommand` na koji se XAML vezuje (`{Binding NekiCommand}`).

| Fajl | Ekran | Ključna odgovornost |
|---|---|---|
| `BaseViewModel.cs` | — | Zajednički `IsBusy`/`ErrorMessage`/`Title` za sve ostale. |
| `LoginViewModel.cs` | Login | `LoginCommand`, `GoToRegisterCommand`. |
| `RegisterViewModel.cs` | Register | `RegisterCommand`, validacija lozinki, `GoToLoginCommand`. |
| `ExploreViewModel.cs` | Istraži | Filteri (sort/tip/status), pretraga, **paginacija** (`LoadFirstPageAsync`/`LoadMoreAsync`), navigacija na detalje. |
| `AnimeDetailViewModel.cs` | Detalji animea | Učitavanje detalja sa API-ja + lokalnih recenzija, forma za status/ocenu/komentar, update-or-insert logika, `CanRate`. |
| `MyReviewsViewModel.cs` | Recenzije jednog profila | `IQueryAttributable` prima `userId` + `status`; filtriranje po statusu, rezime, brisanje **samo** kad je lista moja (`CanDelete`). |
| `ProfileViewModel.cs` | Profil | Podaci o nalogu, brojači pratilaca, raspodela po statusu i navigacija sa nje na recenzije, odjava. |
| `CommunityViewModel.cs` | Zajednica | Feed korisnika koje pratim, pretraga po imenu, segmenti Feed/Svi/Pratim/Prate me, follow-unfollow, bedž za nove pratioce, otvaranje tuđeg profila i top liste. `CommunityUserItem` je stavka liste sa svojim follow stanjem. |
| `UserProfileViewModel.cs` | Tuđi profil | `IQueryAttributable` prima `userId`; brojači, follow dugme, provera privatnosti (`CanSeeList`) i raspodela po statusu koja vodi na recenzije tog korisnika. |
| `TopListViewModel.cs` | Top lista zajednice | Rangiranje animea po proseku lokalnih ocena (`GetCommunityTopAsync`), filter minimalnog broja ocena, otvaranje detalja animea. |
| `AdminViewModel.cs` | Admin | Pretraga korisnika, banovanje/odbanovanje, brisanje naloga, navigacija na recenzije jednog korisnika ili svih. |
| `AdminUserReviewsViewModel.cs` | Admin → recenzije korisnika | `IQueryAttributable` prima `userId` (0 = svi); učitava i briše recenzije. |

### 3.4 `Views/` — čist XAML + tanak code-behind

Svaki `XxxView.xaml.cs` radi samo dve stvari: (1) postavi
`BindingContext = viewModel` (VM dobijen kroz DI ili `ServiceHelper` ako Shell
pravi stranicu preko parameterless konstruktora), i (2) po potrebi prosledi
UI evente (npr. klik na dugme unutar `CollectionView` stavke, `OnAppearing`)
ka komandama na ViewModel-u — **nema poslovne logike u code-behind fajlovima**.

| Fajl | Prikazuje |
|---|---|
| `LoginView` / `RegisterView` | Forme za autentifikaciju. |
| `ExploreView` | `SearchBar` + 3 `Picker`-a (filteri) + `CollectionView` kartica animea sa paginacijom. |
| `AnimeDetailView` | Banner/poster/opis + forma za status/ocenu/komentar + lista recenzija (kroz `CollectionView.Header` + `CollectionView.ItemTemplate`). |
| `MyReviewsView` | Filter-čipovi po statusu + lista recenzija jednog profila (dugme "Obriši" vidljivo samo na sopstvenoj listi). |
| `ProfileView` | Info o nalogu, brojači pratilaca, klikabilna raspodela po statusu, odjava. |
| `CommunityView` | Feed pratilaca + pretraga korisnika, čipovi Feed/Svi/Pratim/Prate me, kartice sa dugmetom Zaprati/Otprati, baner za nove pratioce, dugme Top lista. |
| `UserProfileView` | Javni profil drugog korisnika: brojači, dugme za praćenje, klikabilna raspodela po statusu (sakrivena za privatan profil). |
| `TopListView` | Rang-lista animea po lokalnim ocenama sa posterima i bedževima. |
| `AdminView` | Pretraga i lista korisnika, dugmad Banuj/Obriši/Pogledaj recenzije. |
| `AdminUserReviewsView` | Recenzije jednog korisnika (ili svih, za `userId=0`) sa brisanjem. |

### 3.5 `Controls/RatingBar.xaml(.cs)` — custom kontrola

`ContentView` sa tri `BindableProperty`: `RatingValue` (double), `MaxRating`
(int), `ShowStars` (bool). U code-behind, svaka promena bilo kog od njih
(`Render()`) ručno iscrtava 5 zvezdica (★/☆) i bedž sa brojčanom vrednošću,
obojen po istoj logici kao i DataTrigger bedž (zeleno ≥80%, žuto ≥50%, crveno
ispod). Koristi se i u karticama liste i u formi za ocenjivanje.

### 3.6 `Converters/Converters.cs`

- `NotEmptyBoolConverter` — string → bool, koristi se da se poruke o grešci
  (`Label IsVisible="{Binding ErrorMessage, Converter=...}"`) prikažu samo kad
  imaju sadržaj.
- `InvertedBoolConverter` — obrće bool (npr. `CanRate` → prikaz napomene kad
  se **ne** može oceniti).

### 3.7 `Helpers/`

- `PrefKeys.cs` — imena ključeva koji se koriste u `Preferences` (da se ne
  kucaju stringovi na više mesta).
- `SecurityHelper.cs` — SHA-256 heš lozinke (`Hash(string)`).
- `ToastHelper.cs` — kratka potvrda bez prekidanja korisnika; na Androidu native
  `Toast`, na ostalim platformama fallback na alert. Koristi se umesto
  `DisplayAlert` za uspešne akcije (čuvanje liste, brisanje, promena privatnosti).
- `ServiceHelper.cs` — most do DI kontejnera (`IPlatformApplication.Current.Services`)
  za slučajeve kad MAUI/Shell napravi View bez parametara (npr. `ContentTemplate`);
  svaka `View` ima i bezparametarski konstruktor koji preko ovoga sam izvuče
  svoj ViewModel iz DI-ja.

### 3.8 `Resources/Styles/`

- `Colors.xaml`, `Styles.xaml` — deo standardnog MAUI šablona (osnovna paleta i
  stilovi); ostavljeni netaknuti kao fallback.
- `CustomStyles.xaml` — **ovde je sav vizuelni identitet aplikacije**: tamna
  paleta boja (pozadina, kartice, akcenat, boje za ocene), implicitni stilovi
  (`ContentPage, Label, Button, Entry, Picker...`), imenovani stilovi za
  kartice (`AnimeCardStyle, ReviewCardStyle`), bedževe (`ScoreBadgeStyle` sa
  `DataTrigger`-ima za boju po oceni, `StatusBadgeStyle`, `BanBadgeStyle`) i
  `Trigger` za fokusirano polje unosa.

### 3.9 `Platforms/Android/`

- `AndroidManifest.xml` — dozvole (`INTERNET`, `ACCESS_NETWORK_STATE`) i
  osnovne ikonice aplikacije.
- `MainActivity.cs` / `MainApplication.cs` — standardni Android ulazni delovi
  koje MAUI generiše; `MainApplication.CreateMauiApp()` samo pozove
  `MauiProgram.CreateMauiApp()`.

*(Folderi `Platforms/iOS`, `Platforms/MacCatalyst`, `Platforms/Windows`
postoje kao ostatak originalnog MAUI šablona, ali se **ne kompajliraju** — u
`AniRankApp.csproj` je `TargetFrameworks` postavljen samo na `net10.0-android`,
pa MSBuild te foldere ignoriše.)*

---

## 4. Baza podataka — šema

```
Users
  Id            INTEGER PK AUTOINCREMENT
  Username      TEXT UNIQUE
  Email         TEXT
  PasswordHash  TEXT      -- SHA-256 heš, nikad plain-text
  Role          TEXT      -- "Admin" | "User"
  IsBanned      INTEGER   -- 0/1
  IsPrivate     INTEGER   -- 0/1, privatan profil (listu vide samo pratioci)
  CreatedAt     TEXT      -- ISO datum

Reviews
  Id            INTEGER PK AUTOINCREMENT
  UserId        INTEGER   -- FK ka Users.Id (indeksirano)
  AnimeKitsuId  TEXT      -- id animea sa Kitsu API-ja (indeksirano)
  AnimeTitle    TEXT
  AnimeImageUrl TEXT
  Rating        REAL      -- 0 = bez ocene (Plan to Watch / Dropped), inače 1.0-10.0
  Comment       TEXT
  Status        TEXT      -- Plan to Watch | Watching | Completed | On Hold | Dropped
  EpisodesWatched INTEGER -- 0 = ne prati se napredak
  CreatedAt     TEXT      -- datum unosa, izmena ga NE menja
  UpdatedAt     TEXT      -- datum poslednje izmene (0 na starim redovima)

ReviewLikes
  Id            INTEGER PK AUTOINCREMENT
  ReviewId      INTEGER   -- FK ka Reviews.Id
  UserId        INTEGER   -- FK ka Users.Id    (par je UNIQUE)
  CreatedAt     TEXT

CachedAnime               -- offline keš poslednje Explore strane (nije korisnički podatak)
  Id            TEXT PK   -- Kitsu id
  SortOrder     INTEGER
  ...           (naslov, opis, slike, ocena, status, rangovi)
  CachedAt      TEXT

Follows
  Id            INTEGER PK AUTOINCREMENT
  FollowerId    INTEGER   -- ko prati (FK ka Users.Id, indeksirano)
  FollowingId   INTEGER   -- koga prati (FK ka Users.Id, indeksirano)
  CreatedAt     TEXT
```

Praćenje je usmereno: red `(A → B)` znači "A prati B". Uzajamno praćenje su dva
reda. Nad `(FollowerId, FollowingId)` stoji **unique indeks**, pa ista veza ne
može da postoji dvaput (upis ide kao `INSERT OR IGNORE`). Brisanje naloga
(`DeleteUserAsync`) čisti i njegove recenzije i sve njegove `Follows` redove u
oba smera.

### 4.1 Kako se izbegavaju suvišni upiti

- `GetUsernamesAsync()` — jedan projektovan upit (`SELECT Id, Username FROM Users`)
  umesto `GetUserByIdAsync` po svakoj recenziji (N+1) na detaljima animea i u
  admin pregledu recenzija.
- `CountFollowersAsync` / `CountFollowingAsync` — `SELECT COUNT(*)` po
  indeksiranoj koloni, bez materijalizacije redova.
- `GetReviewCountsByUserAsync` / `GetFollowerCountsAsync` — povlače samo jednu
  kolonu za ceo spisak korisnika (Zajednica ih zove jednom po učitavanju).
- `InitAsync` je zaštićen `SemaphoreSlim`-om i vezu objavljuje tek kad su tabele
  napravljene i admin seed-ovan.

Baza je jedan fajl `anirank.db3` u privatnom app-storage-u uređaja
(`FileSystem.AppDataDirectory`) — briše se samo ako se deinstalira aplikacija
ili ručno obriše podatke aplikacije u Android podešavanjima. `sqlite-net-pcl`
sam radi migraciju šeme (dodaje nove kolone, npr. `Status`/`IsBanned`, na
postojeću bazu pri sledećem pokretanju posle update-a koda).

---

## 5. Navigacija — mapa ruta (`AppShell.xaml` / `AppShell.xaml.cs`)

```
//login                    LoginView            (van TabBar-a)
//register                 RegisterView         (van TabBar-a)
//explore                  ExploreView          (Tab 1)
//community                CommunityView        (Tab 2)
//profile                  ProfileView          (Tab 3)
//admin                    AdminView            (Tab 4, samo Admin - IsVisible="{Binding IsAdmin}")

AnimeDetailView            guranje na tab stack, npr. "AnimeDetailView?id=1234"
reviews                    recenzije jednog profila, "reviews?userId=5&status=Completed"
                           (bez status parametra = sve; sa profila i sa tuđeg profila)
userprofile                javni profil korisnika, "userprofile?userId=5" (iz Zajednice)
toplist                    top lista zajednice (dugme "Top lista" u tabu Zajednica)
adminuserreviews           guranje na Admin tab stack, "adminuserreviews?userId=5" (0 = svi)
```

`//ruta` = apsolutna navigacija (menja ceo tab/ekran), `ruta?param=...` bez `//`
= relativna navigacija (gura novu stranicu na trenutni tab stack, "Nazad" radi
kako se očekuje).

---

## 6. Kitsu API — koji pozivi se koriste

Sve preko `KitsuApiService`, bazni URL `https://kitsu.io/api/edge/`:

- `GET trending/anime` — podrazumevana (početna) lista na Explore strani.
- `GET anime?filter[text]=...&filter[subtype]=...&filter[status]=...&sort=...&page[limit]=...&page[offset]=...` — pretraga/filtriranje/paginacija.
- `GET anime/{id}` — pun opis jednog animea za `AnimeDetailView`.

API ne traži autentifikaciju niti API ključ. Odgovori su u JSON:API formatu
(`{ "data": [...] }` ili `{ "data": {...} }`), mapirani u `Models/KitsuModels.cs`,
pa pretvoreni u prikazu-spreman `Anime` model (`Anime.FromKitsu`).

---

## 7. Gde šta tražiti kad nešto treba izmeniti

- **"Hoću novi ekran"** → napravi `ViewModels/XyzViewModel.cs` (nasledi
  `BaseViewModel`), `Views/XyzView.xaml(.cs)`, registruj oboje u
  `MauiProgram.cs`, dodaj `ShellContent`/rutu u `AppShell.xaml`.
- **"Hoću novo polje u recenziji/korisniku"** → dodaj property u
  `Models/Review.cs` ili `Models/User.cs` (sqlite-net sam migrira šemu), pa
  po potrebi metodu u `DatabaseService`.
- **"Hoću drugačiji izgled/boje"** → sve je u `Resources/Styles/CustomStyles.xaml`.
- **"Hoću da promenim šta se traži od Kitsu API-ja"** → `Services/KitsuApiService.cs`
  (+ eventualno `Models/AnimeFilter.cs` ako dodaješ nov filter).
- **"Hoću drugačiju logiku prijave/banovanja/sesije"** → `Services/AuthService.cs`.
