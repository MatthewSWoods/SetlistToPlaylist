# SetlistToPlaylist

Paste a [Setlist.fm](https://www.setlist.fm) URL and get a Spotify playlist back. One button.

The app fetches the setlist, searches Spotify for each song, creates a private playlist in your account, and streams per-track progress back to the UI in real time.

---

## How it works

1. You paste a setlist.fm concert URL and click **Generate Playlist**
2. If you're not logged in, the app redirects you through Spotify's OAuth (PKCE — no client secret)
3. The API fetches the setlist, creates an empty Spotify playlist, and queues a background job
4. As each track is searched and added, SignalR pushes progress events to your browser
5. A final summary shows how many tracks were found, with a link to open the playlist on Spotify

---

## Tech stack

| Layer | Technology |
|---|---|
| Orchestration | [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) |
| Frontend | Blazor Server (Interactive Server Components) |
| Backend | ASP.NET Core Web API — MVC controllers + SignalR hub |
| Caching / Sessions | Redis (`IDistributedCache`) via Aspire |
| Auth | Spotify PKCE OAuth — no client secret required |
| Progress | SignalR (`/hubs/playlist`) |
| HTTP clients | Typed `HttpClient` via `IHttpClientFactory` |
| Results | `FluentResults` — no exceptions across service boundaries |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Aspire starts Redis in a container)
- A [Spotify Developer app](https://developer.spotify.com/dashboard) — free account is fine
- A [Setlist.fm API key](https://www.setlist.fm/settings/api) — free account is fine

---

## Spotify app setup

In the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard):

1. Create an app (or use an existing one)
2. Add `https://127.0.0.1:5001/auth/callback` as a **Redirect URI**
3. Note your **Client ID** — you do not need the Client Secret

Required scopes are requested automatically:
- `playlist-modify-private`
- `playlist-modify-public`
- `user-read-private`

---

## Configuration

Secrets are stored using `dotnet user-secrets` on the API project — never committed to source control.

```bash
dotnet user-secrets set "Spotify:ClientId" "<your-spotify-client-id>" --project src/Backend/Api
dotnet user-secrets set "SetlistFm:ApiKey" "<your-setlistfm-api-key>" --project src/Backend/Api
```

> `Spotify:CallbackUrl` defaults to `https://127.0.0.1:5001/auth/callback` in `appsettings.json` and does not need to be set manually for local development.

---

## Running the app

```bash
dotnet run --project src/AppHost
```

Aspire starts Redis, the API (fixed port **5001** for the OAuth redirect URI), and the Blazor frontend. The Aspire dashboard opens automatically and shows logs, traces, and health status for all services.

Navigate to the webfrontend URL shown in the dashboard (typically `http://localhost:5XXX`).

---

## Running the tests

```bash
# Unit tests (SetlistFm, Spotify, ApiService)
dotnet test src/Backend/Modules/SetlistFm/SetlistToPlaylist.Backend.Modules.SetlistFm.Tests
dotnet test src/Backend/Modules/Spotify/SetlistToPlaylist.Backend.Modules.Spotify.Tests
dotnet test src/Backend/Api.Tests

# Integration tests (requires Docker — starts the full Aspire app)
dotnet test src/Frontend/Web.Tests
```

Tests use **xUnit v3**, **Shouldly** for assertions, **NSubstitute** for mocking, and **RichardSzalay.MockHttp** for HTTP client testing.

---

## Project structure

```
src/
  AppHost/                          Aspire AppHost — composes all services
  ServiceDefaults/                  Shared OpenTelemetry, health checks, resilience
  Backend/
    Api/                            ASP.NET Core Web API
      Controllers/                  AuthController, SetlistToPlaylistController
      BackgroundServices/           Channel-backed queue + PlaylistPopulationWorker
      Hubs/                         PlaylistProgressHub (SignalR)
      Contracts/                    Request/response records
    Api.Tests/                      Controller and queue unit tests
    Modules/
      SetlistFm/
        ...Abstractions/            ISetlistFmService, ISetlistFmClient, DTOs
        .../                        SetlistFmClient, SetlistFmService implementations
        ...Tests/                   Unit tests
      Spotify/
        ...Abstractions/            ISpotifyService, ISpotifyApiClient, ISpotifyAuthClient, DTOs
        .../                        SpotifyApiClient, SpotifyAuthClient, SpotifyService
        ...Tests/                   Unit tests
  Frontend/
    Web/                            Blazor Server app
      Clients/                      SetlistToPlaylistApiClient (typed HTTP client)
      Components/Pages/             Home.razor
    Web.Tests/                      Aspire integration tests
  Docs/
    SetlistToPlaylist_TechnicalSpecificationDocument.md
```

---

## Notes

- Spotify tokens are stored **server-side in Redis only** — they never appear in API responses or request bodies
- Songs marked as `Tape` (backing tracks) in the setlist are silently skipped
- Track search uses a two-pass strategy: first with artist name, then title-only as a fallback
- Playlists are named `{Artist} @ {Venue} — {dd MMM yyyy}` and created as private by default
