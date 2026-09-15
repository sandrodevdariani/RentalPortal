# Alder & Co. Rental Portal

ASP.NET Core MVC take-home: property managers maintain buildings and units; applicants apply for a unit; an approval issues a twelve-month lease.

This is a server-rendered MVC app (controllers, view models, Razor views, partials, and view components). There is no SPA framework.

## Stack

- .NET 10 / ASP.NET Core MVC + Razor
- Entity Framework Core (code-first) + SQL Server
- ASP.NET Identity (Applicant and Property Manager roles)
- xUnit tests for workflow rules

## Prerequisites (Windows, macOS, or Linux)

1. **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** — `dotnet --version` should report `10.0.x`.
2. **[Docker Desktop](https://www.docker.com/products/docker-desktop/)** — the default way to run SQL Server on every OS. On a Mac with Apple Silicon, enable **Settings → General → Use Rosetta for x86/amd64 emulation**.

The committed connection string talks to SQL Server on `localhost,1433`. That is what `docker compose` publishes on Windows, macOS, and Linux, so you do **not** need to change it if you use Docker.

There is no native SQL Server / Express installer for macOS. Docker is required on a Mac. On Windows you may use LocalDB or SQL Server Express instead; see [Windows without Docker](#windows-without-docker) below.

## Run locally

Same commands on **macOS, Windows (PowerShell or cmd), and Linux**, from the repo root. Wait until Docker Desktop is running, then:

```bash
docker compose up -d --wait
dotnet test
dotnet run --project src/RentalPortal --launch-profile http
```

`--wait` holds until SQL Server’s healthcheck passes (first pull is large; first start can take 30–60 seconds).

Then open [http://localhost:5088](http://localhost:5088).

On first run the app:

1. Creates the `RentalPortal` database if needed
2. Applies EF Core migrations
3. Seeds lookups, users, properties, units, and applications in every status (idempotent)

Stop the site with Ctrl+C. Stop the database with `docker compose down` (add `-v` only if you want to wipe the volume).

### macOS note

If the SDK was installed with the `dotnet-install` script rather than the pkg, add it to `PATH` for that terminal:

```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
```

### Windows note

If `dotnet` or `docker` is not found, restart the terminal after installing so `PATH` refreshes. Run the same three commands in PowerShell from the repo folder.

If port **1433** is already in use (a local SQL Server instance), either stop that instance or change the host port in `docker-compose.yml` and the connection string together (for example `localhost,14333`).

If port **5088** is in use, run with `--launch-profile https` and use the URL `dotnet` prints, or pick a free URL:

```bash
dotnet run --project src/RentalPortal --launch-profile http --urls http://localhost:5050
```

## Connection string (Docker — all platforms)

```
Server=localhost,1433;Database=RentalPortal;User Id=sa;Password=RentalPortal!123;TrustServerCertificate=True;MultipleActiveResultSets=True
```

This is already in `src/RentalPortal/appsettings.json` and `appsettings.Development.json`.

## Windows without Docker

Install [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads) or use LocalDB that ships with Visual Studio / the SQL Server tooling. Do not change the committed `appsettings.json`. Override only on that machine:

**PowerShell (current session):**

```powershell
# LocalDB
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\mssqllocaldb;Database=RentalPortal;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"

# SQL Server Express (Windows auth)
$env:ConnectionStrings__DefaultConnection = "Server=.\SQLEXPRESS;Database=RentalPortal;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

Then, without `docker compose`:

```powershell
dotnet test
dotnet run --project src/RentalPortal --launch-profile http
```

Migrations and seed still run on startup. SQL Server must allow a database create (or create an empty `RentalPortal` database first).

## Seeded accounts

Password for every seeded user: `Password1!`

| Email | Role |
| --- | --- |
| `manager@alder.test` | Property Manager |
| `applicant@alder.test` | Applicant |
| `pm1@alder.test` … `pm3@alder.test` | Property Manager |
| `applicant1@alder.test` … `applicant8@alder.test` | Applicant |

Seed data includes properties, units, an inactive unit type still attached to an existing penthouse, and applications in every status (Draft, Submitted, Returned, Approved, Denied, Withdrawn). The approved application has a lease covering today, so that unit is not available.

## What to click through for the video

1. Log in as `applicant@alder.test`. Open the existing **Draft** and **Returned** applications. Complete applicant info, add a residence through the modal, continue to summary, submit.
2. Log in as `manager@alder.test`. Filter applications by status/property. Open a **Submitted** application, complete the review modal (return with a comment, then later approve another).
3. After a return, log back in as the applicant, correct the application, and resubmit.
4. Approve an application and confirm a twelve-month lease; that unit disappears from Available units.
5. As a manager, add/edit a property and unit in modals. Confirm inactive types (Penthouse, Garden Suite) cannot be selected on a new unit, but Penthouse still displays on Harbor Court PH1.
6. Submit a modal form empty to show validation staying inside the modal.

## Project layout

- `src/RentalPortal` — MVC app, Identity, EF, Razor views / partials / view components
- `tests/RentalPortal.Tests` — unit tests for application and unit-type rules
- `docker-compose.yml` — SQL Server 2022 (works on Windows, macOS, and Linux)
