# AniRankApp — arhitektura (ko koga poziva)

Dijagrami su u [Mermaid](https://mermaid.js.org/) formatu. GitHub i Visual Studio Code
(sa Markdown Preview Mermaid ekstenzijom) ih prikazuju kao slike.

Strelica `A --> B` znači: **A poziva B** (ili A dobija B u konstruktoru i koristi ga).

---

## 1. Slojevi aplikacije (pregled)

```mermaid
flowchart TD
    MP["MauiProgram<br/>(DI kontejner)"]
    APP["App"]
    SHELL["AppShell<br/>(tabovi + rute)"]
    V["Views<br/>(XAML stranice)"]
    VM["ViewModels<br/>(logika ekrana)"]
    S["Services<br/>AuthService · DatabaseService · KitsuApiService"]
    H["Helpers<br/>ServiceHelper · SecurityHelper · ToastHelper · PrefKeys"]
    M["Models<br/>User · Review · Follow · ReviewLike · CachedAnime · Anime ..."]
    SQL[("SQLite baza<br/>anirank.db3")]
    KITSU[("Kitsu API<br/>kitsu.io/api/edge")]
    PREF[("Preferences<br/>sesija")]

    MP -. "registruje sve klase" .-> S
    MP -. registruje .-> VM
    MP -. registruje .-> V
    APP -->|"new AppShell()"| SHELL
    SHELL -->|"prikazuje / navigira"| V
    V -->|"BindingContext = vm<br/>binding + komande"| VM
    VM --> S
    VM --> H
    S --> H
    S --> M
    VM --> M
    S --> SQL
    S --> KITSU
    S --> PREF
```

Pravilo: **View ne zna za servise, ViewModel ne zna za XAML.** View samo drži svoj
ViewModel, a ViewModel dobija servise kroz konstruktor (dependency injection).

---

## 2. Pokretanje aplikacije

```mermaid
sequenceDiagram
    participant OS as Android
    participant MP as MauiProgram
    participant App
    participant Shell as AppShell
    participant Auth as AuthService
    participant Db as DatabaseService
    participant V as LoginView / ExploreView

    OS->>MP: CreateMauiApp()
    MP->>MP: registruje servise, ViewModele i View-ove
    MP->>App: new App()
    App->>Shell: CreateWindow → new AppShell()
    Shell->>Shell: ServiceHelper.GetService (AuthService, DatabaseService)
    Shell->>Shell: Routing.RegisterRoute(AnimeDetailView, reviews, userprofile, adminuserreviews)
    Shell->>Auth: IsLoggedIn / RefreshSessionAsync()
    Auth->>Db: GetUserByIdAsync()
    alt sesija postoji i korisnik nije banovan
        Shell->>V: prikaže tab "Istraži"
        Shell->>Db: CountFollowersSinceAsync() (bedž za nove pratioce)
    else nema sesije
        Shell->>V: GoToAsync("//login")
    end
```

---

## 3. View → ViewModel (1 na 1)

Svaki View u konstruktoru dobije svoj ViewModel. Kad Shell pravi stranicu iz
`ContentTemplate` (bez parametara), View ga uzme preko `ServiceHelper.GetService<...>()`.

```mermaid
flowchart LR
    subgraph Views
        LoginView
        RegisterView
        ExploreView
        AnimeDetailView
        CommunityView
        UserProfileView
        MyReviewsView
        TopListView
        ProfileView
        AdminView
        AdminUserReviewsView
        LogoutView
    end
    subgraph ViewModels
        LoginViewModel
        RegisterViewModel
        ExploreViewModel
        AnimeDetailViewModel
        CommunityViewModel
        UserProfileViewModel
        MyReviewsViewModel
        TopListViewModel
        ProfileViewModel
        AdminViewModel
        AdminUserReviewsViewModel
        LogoutViewModel
    end
    LoginView --> LoginViewModel
    RegisterView --> RegisterViewModel
    ExploreView --> ExploreViewModel
    AnimeDetailView --> AnimeDetailViewModel
    CommunityView --> CommunityViewModel
    UserProfileView --> UserProfileViewModel
    MyReviewsView --> MyReviewsViewModel
    TopListView --> TopListViewModel
    ProfileView --> ProfileViewModel
    AdminView --> AdminViewModel
    AdminUserReviewsView --> AdminUserReviewsViewModel
    LogoutView --> LogoutViewModel
```

Svi ViewModeli nasleđuju `BaseViewModel` (`IsBusy`, `Title`, `ErrorMessage`).

---

## 4. ViewModel → Servisi

```mermaid
flowchart LR
    LoginViewModel --> AuthService
    RegisterViewModel --> AuthService
    LogoutViewModel --> AuthService

    ExploreViewModel --> KitsuApiService
    ExploreViewModel --> DatabaseService

    AnimeDetailViewModel --> KitsuApiService
    AnimeDetailViewModel --> DatabaseService
    AnimeDetailViewModel --> AuthService

    CommunityViewModel --> DatabaseService
    CommunityViewModel --> AuthService

    UserProfileViewModel --> DatabaseService
    UserProfileViewModel --> AuthService

    MyReviewsViewModel --> DatabaseService
    MyReviewsViewModel --> AuthService

    ProfileViewModel --> DatabaseService
    ProfileViewModel --> AuthService

    TopListViewModel --> DatabaseService

    AdminViewModel --> DatabaseService
    AdminViewModel --> AuthService

    AdminUserReviewsViewModel --> DatabaseService

    AppShell --> AuthService
    AppShell --> DatabaseService

    AuthService --> DatabaseService
    KitsuApiService --> HttpClient
    DatabaseService --> SQLite[("SQLite")]
    HttpClient --> Kitsu[("Kitsu API")]
```

### Koje metode koji ViewModel poziva

| ViewModel | `AuthService` | `DatabaseService` | `KitsuApiService` |
|---|---|---|---|
| `LoginViewModel` | `LoginAsync` | — | — |
| `RegisterViewModel` | `RegisterAsync` | — | — |
| `LogoutViewModel` | `CurrentUsername`, `Logout` | — | — |
| `ExploreViewModel` | — | `GetCachedAnimeAsync`, `SaveAnimeCacheAsync`, `GetAnimeCacheTimeAsync` (offline keš) | `GetAnimeAsync` |
| `AnimeDetailViewModel` | `CurrentUserId` | `GetReviewsForAnimeAsync`, `GetReviewByIdAsync`, `InsertReviewAsync`, `UpdateReviewAsync`, `GetAnimeRatingStatsAsync`, `GetUsernamesAsync`, `LikeReviewAsync`, `UnlikeReviewAsync`, `GetLikeCountsAsync`, `GetLikedReviewIdsAsync` | `GetAnimeDetailsAsync` |
| `CommunityViewModel` | `CurrentUserId`, `FollowersSeenAt`, `MarkFollowersSeen` | `GetFeedAsync`, `GetOtherUsersAsync`, `FollowAsync`, `UnfollowAsync`, `GetFollowerIdsAsync`, `GetFollowingIdsAsync`, `GetFollowerCountsAsync`, `GetReviewCountsByUserAsync`, `CountFollowersSinceAsync`, `GetUsernamesAsync` | — |
| `UserProfileViewModel` | `CurrentUserId`, `IsAdmin` | `GetUserByIdAsync`, `GetUserReviewsAsync`, `IsFollowingAsync`, `FollowAsync`, `UnfollowAsync`, `CountFollowersAsync`, `CountFollowingAsync` | — |
| `MyReviewsViewModel` | `CurrentUserId`, `IsAdmin` | `GetUserReviewsAsync`, `DeleteReviewAsync`, `GetUserByIdAsync`, `IsFollowingAsync` | — |
| `ProfileViewModel` | `CurrentUserId`, `CurrentUsername`, `CurrentUserRole`, `LoginAt` | `GetUserByIdAsync`, `GetUserReviewsAsync`, `SetUserPrivateAsync`, `CountFollowersAsync`, `CountFollowingAsync` | — |
| `TopListViewModel` | — | `GetCommunityTopAsync` | — |
| `AdminViewModel` | `CurrentUserId`, `RegisterAsync` | `GetAllUsersAsync`, `SetUserBannedAsync`, `DeleteUserAsync` | — |
| `AdminUserReviewsViewModel` | — | `GetUserReviewsAsync`, `GetAllReviewsAsync`, `GetUserByIdAsync`, `GetUsernamesAsync`, `DeleteReviewAsync` | — |

---

## 5. Servisi → skladišta i helperi

```mermaid
flowchart TD
    AuthService -->|"GetUserByUsernameAsync<br/>GetUserByIdAsync<br/>InsertUserAsync"| DatabaseService
    AuthService -->|"Hash(lozinka)"| SecurityHelper
    AuthService -->|"UserId, Username, Role,<br/>LoginAt, FollowersSeenAt"| PrefKeys
    PrefKeys -.-> Preferences[("Preferences<br/>(sesija na uređaju)")]

    DatabaseService -->|"Hash (seed admin naloga)"| SecurityHelper
    DatabaseService --> SQLite[("SQLite: anirank.db3")]
    SQLite --- T1["Users"]
    SQLite --- T2["Reviews"]
    SQLite --- T3["Follows"]
    SQLite --- T4["ReviewLikes"]
    SQLite --- T5["CachedAnime"]

    KitsuApiService --> HttpClient
    HttpClient -->|"trending/anime<br/>anime?filter...<br/>anime/{id}"| Kitsu[("kitsu.io/api/edge")]
    KitsuApiService -->|"KitsuAnimeResponse → Anime"| KitsuModels
```

Ostali helperi koje zovu ViewModeli / View-ovi:

- `ToastHelper.ShowAsync` — `AnimeDetailViewModel`, `MyReviewsViewModel`, `ProfileViewModel`
- `ServiceHelper.GetService<T>()` — svi View-ovi i `AppShell` (pristup DI kontejneru)
- `PasswordVisibility.Toggle` — `LoginView`, `RegisterView` (oko za lozinku)
- `Connectivity` (MAUI) — `ExploreViewModel`, `AnimeDetailViewModel` (provera interneta)

---

## 6. Navigacija između ekrana

Punim linijama su tabovi (`//ruta`), isprekidanim stranice koje se otvaraju preko
registrovanih ruta (`GoToAsync("ruta?parametar=...")`).

```mermaid
flowchart LR
    login["LoginView<br/>//login"]
    register["RegisterView<br/>//register"]

    subgraph Tabovi
        explore["ExploreView<br/>//explore"]
        community["CommunityView<br/>//community"]
        toplist["TopListView<br/>//toplist"]
        profile["ProfileView<br/>//profile"]
        admin["AdminView<br/>//admin (samo Admin)"]
        logout["LogoutView<br/>//logout"]
    end

    detail["AnimeDetailView<br/>?id="]
    reviews["MyReviewsView<br/>reviews?userId=&status="]
    userprofile["UserProfileView<br/>userprofile?userId="]
    adminrev["AdminUserReviewsView<br/>adminuserreviews?userId="]

    login -->|"uspešna prijava"| explore
    login --> register
    register -->|"posle registracije"| login
    logout -->|"Odjavi se"| login

    explore -.-> detail
    toplist -.-> detail
    toplist -->|"prazna lista"| explore
    community -.->|"klik na recenziju u feedu"| detail
    community -.->|"klik na korisnika"| userprofile
    profile -.->|"moja lista / po statusu"| reviews
    userprofile -.->|"njegova lista / po statusu"| reviews
    admin -.->|"Pogledaj recenzije / Sve recenzije"| adminrev
```

---

## 7. Primer toka: korisnik oceni anime

```mermaid
sequenceDiagram
    actor K as Korisnik
    participant V as AnimeDetailView
    participant VM as AnimeDetailViewModel
    participant Api as KitsuApiService
    participant Auth as AuthService
    participant Db as DatabaseService

    K->>V: tap na anime (iz Istraži / Top liste)
    V->>VM: [QueryProperty] AnimeId = id → LoadAsync()
    VM->>Api: GetAnimeDetailsAsync(id)
    VM->>Db: GetReviewsForAnimeAsync(id)
    VM->>Db: GetAnimeRatingStatsAsync(id)
    VM->>Db: GetLikeCountsAsync / GetLikedReviewIdsAsync
    VM-->>V: popunjena svojstva (binding)
    K->>V: izabere ocenu i status, potvrdi formu
    V->>VM: SubmitReviewCommand
    VM->>Auth: CurrentUserId
    alt već postoji stavka
        VM->>Db: UpdateReviewAsync(review)
    else nova stavka
        VM->>Db: InsertReviewAsync(review)
    end
    VM->>VM: ToastHelper.ShowAsync("Vaša lista je ažurirana.")
```
