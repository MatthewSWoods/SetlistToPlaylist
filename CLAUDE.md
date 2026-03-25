# SetlistToPlaylist — Claude Code Instructions

## Project overview

Blazor Server + ASP.NET Core Web API application that converts a Setlist.fm setlist into a Spotify playlist.
See `src/Docs/SetlistToPlaylist_TechnicalSpecificationDocument.md` for the full architecture and design decisions.

---

## Spotify Web API rules

When writing any code that interacts with the Spotify Web API, follow these rules:

- **OpenAPI spec:** Refer to the Spotify OpenAPI specification at https://developer.spotify.com/reference/web-api/open-api-schema.yaml for all endpoint paths, parameters, and response schemas. Do not guess endpoints or field names.
- **Authorization:** Use the Authorization Code with PKCE flow (https://developer.spotify.com/documentation/web-api/tutorials/code-pkce-flow). The backend holds no client secret. Never use the Implicit Grant flow — it is deprecated.
- **Redirect URIs:** Use `https://` redirect URIs in production. Use `http://127.0.0.1` (not `http://localhost`) for local development. Never use wildcard URIs.
- **Scopes:** Request only the minimum scopes needed: `playlist-modify-private`, `playlist-modify-public`, `user-read-private`. Do not request additional scopes preemptively.
- **Token management:** Tokens are stored server-side in Redis (`IDistributedCache`). Never expose tokens in API responses, client-side code, or request bodies. Implement token refresh so the app does not break when access tokens expire.
- **Rate limits:** Implement exponential backoff and respect the `Retry-After` header on HTTP 429 responses. Do not retry immediately or in tight loops.
- **Deprecated endpoints:** Do not use deprecated endpoints. Use `/playlists/{id}/items` (not `/playlists/{id}/tracks`) for adding/removing tracks.
- **Error handling:** Handle all HTTP error codes from the OpenAPI schema. Surface meaningful error messages to the user via SignalR progress events.
- **Developer Terms:** Do not cache Spotify content beyond immediate use. Always attribute content to Spotify. Do not use the API to train ML models on Spotify data.

---

## General coding conventions

- All projects target `net10.0`.
- `TreatWarningsAsErrors = true` in all projects — fix warnings, do not suppress them.
- Nullable reference types enabled everywhere — no `!` null-forgiving operators without justification.
- Use `System.Text.Json` throughout. Do not introduce Newtonsoft.Json.
- Use `FluentResults` (`Result<T>`) for service and client return types. Do not throw exceptions across service boundaries.
- Use `FluentValidation` for request model validation in controllers.
- Prefer `sealed record` for DTOs and request/response contracts.
- Prefer `sealed class` for service/client implementations.
- Keep the layered separation: Controller → Service → HttpClient → External API.
