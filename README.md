# TaskMate — Student Task Manager

TaskMate is a full-stack task manager for students. Register an account, organize assignments, exams, projects, and personal tasks, and keep an eye on what is due soon or overdue.

It uses an ASP.NET Core 10 Web API, EF Core 10, SQLite, JWT bearer authentication, and a responsive HTML/CSS/JavaScript frontend served by the API. No separate frontend server, Node.js, or npm install is needed.

## Features

- Email and password registration and login. Passwords are salted PBKDF2-HMAC-SHA256 hashes.
- JWT-protected, user-scoped task endpoints.
- Create, view, edit, complete, and delete tasks with due dates, priority, status, category, and notes.
- Dashboard totals for all tasks, completed tasks, deadlines due within seven days, overdue tasks, and completion percentage.
- Server-side title search, status/category/priority filters, upcoming date filters, and pagination.
- Responsive dashboard with mobile navigation and `/` as a keyboard shortcut to search.
- EF Core migrations applied at startup, migration sources, and a generated SQL setup script.
- Per-IP registration/login rate limiting, security headers, production JWT secret checks, and optional trusted proxy support.
- Docker image and Compose setup with persistent SQLite storage.

## Requirements

- .NET SDK 10.0 or later.
- Docker Engine with Docker Compose v2 for container deployment.

The frontend uses browser APIs and has no JavaScript package dependencies.

## Run locally

From the repository root:

```powershell
dotnet restore backend/TaskMate.Api/TaskMate.Api.csproj
dotnet run --project backend/TaskMate.Api/TaskMate.Api.csproj
```

Or run `scripts/run.ps1` on Windows or `sh scripts/run.sh` on macOS/Linux. The launch profile listens at **http://localhost:5117**. Open the address, create an account, and add your first task. The database is created at `backend/TaskMate.Api/App_Data/taskmate.db` on first startup.

The development JWT signing key in `appsettings.json` is for local use only. Production startup refuses to use it. Set a unique key of at least 32 bytes before starting in Production:

```powershell
$env:Authentication__Jwt__SigningKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://localhost:8080"
dotnet run --no-launch-profile --project backend/TaskMate.Api/TaskMate.Api.csproj
```

For bash:

```bash
export Authentication__Jwt__SigningKey="$(openssl rand -base64 48)"
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://localhost:8080
dotnet run --no-launch-profile --project backend/TaskMate.Api/TaskMate.Api.csproj
```

Set `ConnectionStrings__DefaultConnection` to change the SQLite location. Relative paths resolve from the API content directory. Keep the database on durable storage and back it up before upgrades. SQLite here is intended for local use or a single app instance. Use a server database and its EF provider for horizontally scaled deployments.

## Database migrations

The app runs EF Core `Database.Migrate()` on startup. The initial migration is in `backend/TaskMate.Api/Data/Migrations/`. To manage migrations manually, install EF CLI and run from the repository root:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.13
dotnet ef database update --project backend/TaskMate.Api/TaskMate.Api.csproj
```

`database/schema.sql` is generated from the current migration and initializes a fresh SQLite database, for example:

```powershell
sqlite3 backend/TaskMate.Api/App_Data/taskmate.db ".read database/schema.sql"
```

SQLite does not support EF Core's idempotent migration scripts. Use the app's migrator or `dotnet ef database update` for an existing database.

## API overview

All JSON properties use camel case. Task statuses are `ToDo`, `InProgress`, and `Completed`; priorities are `Low`, `Medium`, and `High`; categories are `Assignment`, `Exam`, `Project`, and `Personal`. Dates use `yyyy-MM-dd` and represent calendar dates, not timestamps.

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `GET` | `/api/health` | No | Liveness response |
| `POST` | `/api/auth/register` | No | Create an account and return a bearer token |
| `POST` | `/api/auth/login` | No | Sign in and return a bearer token |
| `GET` | `/api/auth/me` | Yes | Return the signed-in profile |
| `GET` | `/api/dashboard?today=2026-10-22` | Yes | Summary counts using the supplied local date |
| `GET` | `/api/tasks` | Yes | List tasks with filters and pagination |
| `GET` | `/api/tasks/{id}` | Yes | Read one owned task |
| `POST` | `/api/tasks` | Yes | Create a task |
| `PUT` | `/api/tasks/{id}` | Yes | Update an owned task |
| `DELETE` | `/api/tasks/{id}` | Yes | Delete an owned task |

`GET /api/tasks` accepts `status`, `category`, `priority`, `search`, `dueFrom`, `dueThrough`, `excludeCompleted`, `page`, and `pageSize`. Page size is limited to 100. A response has this shape:

```json
{ "items": [], "page": 1, "pageSize": 20, "totalCount": 0, "totalPages": 0 }
```

Example registration:

```http
POST /api/auth/register
Content-Type: application/json

{
  "name": "Alex Morgan",
  "email": "alex@example.edu",
  "password": "A-long-private-passphrase"
}
```

Example task create/update body:

```json
{
  "title": "Biology lab report",
  "description": "Finish the results and discussion sections.",
  "dueDate": "2026-10-28",
  "priority": "High",
  "status": "InProgress",
  "category": "Assignment"
}
```

Send the returned token in `Authorization: Bearer <token>` for protected routes. Validation errors use ASP.NET Core Problem Details responses. A task ID that is missing or belongs to another user returns `404`.

## Configuration

| Setting | Default | Notes |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Data Source=App_Data/taskmate.db;...` | SQLite connection string |
| `Authentication__Jwt__SigningKey` | Local development key | Production requires a unique secret of at least 32 bytes |
| `Authentication__Jwt__Issuer` | `TaskMate` | Token issuer |
| `Authentication__Jwt__Audience` | `TaskMate.Web` | Token audience |
| `Authentication__Jwt__AccessTokenMinutes` | `720` | Must be between 5 and 1440 minutes |
| `ForwardedHeaders__KnownProxies__0` | Unset | Optional trusted proxy IP for client IP and HTTPS scheme |
| `AllowedHosts` | `*` | Set this to your production host names |

The frontend keeps the bearer token in `sessionStorage` for the current browser tab. Tokens expire; users sign in again after expiry. Use HTTPS in production. Behind a reverse proxy, add its IP under `ForwardedHeaders:KnownProxies` and have it set `X-Forwarded-For` and `X-Forwarded-Proto`. Do not trust forwarded headers from untrusted clients.

## Docker deployment

Set a production signing secret and public host name, then start the app:

```powershell
$env:TASKMATE_JWT_SECRET = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$env:TASKMATE_ALLOWED_HOSTS = "tasks.example.com"
docker compose up --build -d
```

The app listens on port 8080 inside the container. Compose publishes host port 8080 by default; set `TASKMATE_PORT` to change it. SQLite is stored in the persistent `taskmate-data` volume. Put the service behind a TLS-terminating reverse proxy, set `TASKMATE_ALLOWED_HOSTS`, configure the proxy IP if needed, and back up the volume.

## Project structure

```text
TaskMate/
├── backend/TaskMate.Api/
│   ├── Contracts/            # Request, response, and query DTOs
│   ├── Controllers/          # Authentication, dashboard, and task endpoints
│   ├── Data/                 # EF context, design-time factory, migrations
│   ├── Domain/               # User/task entities and enums
│   ├── Repositories/         # SQLite data access
│   ├── Services/             # Authentication, hashing, JWT, task logic
│   ├── appsettings.json
│   └── TaskMate.Api.csproj
├── Frontend/                 # Responsive browser UI
├── database/schema.sql       # SQL generated from the initial migration
├── scripts/                  # Local run helpers
├── Dockerfile
├── compose.yaml
├── TaskMate.slnx
└── README.md
```

## License

No license has been selected for this repository yet. Add one before redistributing the project.
