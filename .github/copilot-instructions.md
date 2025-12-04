## Quick orientation for AI coding agents

This repository is a multi-front-end + multi-backend sample app that converts Setlist.fm pages into Spotify playlists.
The primary active components are:

- BackEnd/CSharp/SetlistToPlaylist.Api — main ASP.NET (dotnet 8) API (Controllers/, Services/, RestApiClients/).
- BackEnd/Python/SetlistToPlaylist.Api — alternate Python FastAPI implementation (app/, db/). Currently incomplete; treat with care.
- FrontEnd/react — the Docker-compose default front-end (Vite + React). Other frontends (vue, svelte, angular) are present for reference.

Key intent & architecture notes
- The canonical, containerised flow used by CI/docker-compose is the C# API + React client. See `docker-compose.yml` which builds
  the C# API image and the React client and wires ports: the C# image exposes 8081 and is mapped to host 5001; the React client runs on 3001.
- Secrets are intentionally NOT checked in. Populate `BackEnd/CSharp/SetlistToPlaylist.Api/ApiSecrets.json` locally (see C# README).
- C# project layout follows standard ASP.NET conventions: `Program.cs` wires services and controllers, `Controllers/` exposes HTTP endpoints,
  `Services/` contains business logic and `RestApiClients/` contains external API wrappers (Spotify, Setlist.fm).
- The Python FastAPI service mirrors the same domain concepts (app, db). It uses SQLAlchemy and uvicorn — but some files contain incomplete content
  (inspect `BackEnd/Python/SetlistToPlaylist.Api/SetlistToPlaylist.py` before editing or running).

What to run (local dev / debug)
- Full stack (containers): from repo root run `docker-compose up --build` (compose file is `docker-compose.yml`). The compose file builds the C# API and React client.
- C# API (dev): `cd BackEnd/CSharp/SetlistToPlaylist.Api` then `dotnet build` / `dotnet run` (or open the solution in Visual Studio). Tests: `dotnet test` in `SetlistToPlaylist.Api.Test`.
- React frontend (dev): `cd FrontEnd/react` then `npm install` and `npm run dev` (Vite). The client expects API at `VITE_API_BASE_URL` (set in docker-compose as an env var for container runs).
- Python API (dev): `cd BackEnd/Python/SetlistToPlaylist.Api` then `pip install -r requirements.txt` and `uvicorn SetlistToPlaylist:app --reload --port 5001` — but confirm `requirements.txt` and `SetlistToPlaylist.py` are valid first.

Important file references (examples)
- Docker compose: `docker-compose.yml` (root) — uses `BackEnd/CSharp/SetlistToPlaylist.Api/Dockerfile` and `FrontEnd/react/Dockerfile`.
- C# Dockerfile: `BackEnd/CSharp/SetlistToPlaylist.Api/Dockerfile` — copies `ApiSecrets.json` into the image, so local secrets file matters for container runs.
- C# solution: `BackEnd/CSharp/SetlistToPlaylist.sln` and project: `BackEnd/CSharp/SetlistToPlaylist.Api/SetlistToPlaylist.Api.csproj`.
- React entry: `FrontEnd/react/src/main.jsx` and config: `FrontEnd/react/package.json` (dev scripts, dependencies).
- Python entry: `BackEnd/Python/SetlistToPlaylist.Api/SetlistToPlaylist.py` and DB models: `BackEnd/Python/SetlistToPlaylist.Api/db/`.

Conventions & patterns to follow
- Prefer editing C# API for server-side changes when the task relates to production behavior; the docker-compose flow assumes the C# API is authoritative.
- Controller -> Service -> RestApiClient is the typical call path in C#; make minimal, well-scoped changes that keep this separation.
- Frontend apps are independent projects; the React app is used for container runs. If you change other frontends, document how they differ.
- Secrets: always look for an `ApiSecrets.json` in the C# project root or `appsettings.*.json` files for configuration keys.

Edge cases & gotchas discovered in the repo
- There are multiple frontends — confirm which one the user wants to change. `docker-compose.yml` defaults to the React client.
- Some files in the Python folder appear malformed (for example, `requirements.txt` contains Dockerfile text). Validate files before running the Python service.
- Paths in Docker/compose may use different capitalisation (Backend vs BackEnd). Windows is tolerant, CI or Linux runners may be case-sensitive.

Editing contract for AI agents (short)
- Inputs: the path(s) to change and a clear test or manual verification step (e.g., HTTP endpoint to hit, UI page to open).
- Outputs: a PR/patch containing small, focused changes + updated tests or a short manual verification note.
- Error modes: do not commit secrets; confirm container builds on Linux (case-sensitivity) before finalizing infra changes.

If anything here is incorrect or you want more detail (example requests, test commands, selected frontend), tell me which area to expand and I'll update this file.
