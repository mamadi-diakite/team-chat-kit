# team-chat-kit

A real-time team chat built to be added to an existing web application.

The chat ships as a .NET module with a SignalR hub and an Angular library. The host application keeps its own users and login; it only tells the chat who the current user is. A demo host app shows the integration.

Members of the same organization find each other in a directory and talk directly. People from other organizations must accept an invitation first. Users see who is online, chat one to one or in groups, manage group members, and save or pin messages.

## Who it is for

Teams and product builders who want chat inside their own application instead of one more tool.

## What it is not

- No audio or video calls.
- No end-to-end encryption: messages are stored in the host's database.
- No file sharing yet.

## Stack

- Front end: Angular workspace with an embeddable chat library and a demo host app
- Back end: ASP.NET Core (C#), chat module as class libraries, SignalR
- Database: SQLite with EF Core

## Status

Early development (milestone M0). What works today:

- In the demo host: create an account that joins or creates an organization, log in, change your password, log out. Sessions expire after 60 minutes and you are sent back to the login page.
- In the chat: see the directory of your organization and search it by name, invite someone from another organization by email with an optional message, accept, decline or cancel invitations, see your contacts and remove one.

Messaging, the real-time hub, presence and groups are planned (see [ROADMAP.md](ROADMAP.md)).

## Getting started

### Prerequisites

- [.NET SDK 10.0.403](https://dotnet.microsoft.com/download) or a later 10.0 feature band
- [Node.js 24](https://nodejs.org/) with npm 11

### Run the demo host API

The access tokens are signed with a key that is never stored in the repository, and the API does not start without it. Set a base64 key of at least 32 bytes, then run the API.

Bash:

```bash
export Jwt__SigningKey="$(openssl rand -base64 32)"
cd src/backend
dotnet run --project src/TeamChat.DemoHost.Api --launch-profile http
```

PowerShell:

```powershell
$key = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($key)
$env:Jwt__SigningKey = [Convert]::ToBase64String($key)
cd src/backend
dotnet run --project src/TeamChat.DemoHost.Api --launch-profile http
```

The API listens on http://localhost:5086. In Development it creates the SQLite database `src/backend/src/TeamChat.DemoHost.Api/App_Data/team-chat-kit.db`, applies the chat migrations then the host migrations, and adds the demo data below when the database has no users. Keep the same key between restarts to stay logged in.

### Run the web app

```bash
cd src/frontend
npm ci
npm start
```

Open http://localhost:4200. Calls to `/api` (the host endpoints and the chat endpoints under `/api/chat`) are sent to the API on port 5086.

### Demo data

All demo accounts use the password `TestDemo123!`. They are fictional and only exist in Development.

| Name | Email | Organization |
|------|-------|--------------|
| Emma Laurent | emma.laurent@example.com | Bluebird Design |
| Hugo Bernard | hugo.bernard@example.com | Bluebird Design |
| Chloe Petit | chloe.petit@example.com | Bluebird Design |
| Liam Moreau | liam.moreau@example.com | Harbor Logistics |
| Sara Dubois | sara.dubois@example.com | Harbor Logistics |
| Noah Girard | noah.girard@example.com | Pine Valley Clinic |

Log in as Emma to see Hugo and Chloe in the directory, then invite liam.moreau@example.com and log in as Liam to accept.

To start again from an empty demo, stop the API and delete `src/backend/src/TeamChat.DemoHost.Api/App_Data/`.

### Run the tests

Back end (with coverage, migrations and generated code excluded):

```bash
cd src/backend
dotnet test TeamChat.slnx --settings coverage.runsettings --collect "XPlat Code Coverage"
```

Front end (Vitest, the run fails below 100% coverage):

```bash
cd src/frontend
npm run test:ci
```

## Integration overview

The chat never references the host. A host adds it in four steps; `TeamChat.DemoHost.Api` is a complete example.

1. Reference `TeamChat.AspNetCore` and implement `IChatIdentityAdapter` (from `TeamChat.Application.Identity`): read the user id from the authenticated principal, find users by id and by email, and list the members of an organization. The chat stores only user ids and asks the adapter for names, emails and organizations.
2. Register the chat with the adapter and its options:

   ```csharp
   builder.Services.AddTeamChat<MyChatIdentityAdapter>(options =>
   {
       options.ConnectionString = "Data Source=App_Data/app.db";
       options.TablePrefix = "Chat";        // the chat tables are ChatInvitations, ChatContacts, ...
       options.RoutePrefix = "/api/chat";   // where the chat endpoints are mapped
   });
   ```

3. Apply the chat migrations at startup with `await app.Services.ApplyTeamChatMigrationsAsync();`. The chat has its own DbContext and migrations history table (`{TablePrefix}MigrationsHistory`), so it can share the host database.
4. Map the endpoints after the host authentication with `app.MapTeamChat();`. Every chat endpoint but `{RoutePrefix}/health` uses the host's authentication (for example JWT bearer) and the adapter to know the current user.

On the front end, the `chat` library (`@team-chat-kit/chat`) does not depend on the demo app. The host calls `provideChat({ apiBaseUrl: '/api/chat' })` with its route prefix, sends its access token with an HTTP interceptor, and shows the `tck-chat-directory`, `tck-chat-invitations` and `tck-chat-contacts` components where it wants them.

## Project layout

```
src/backend/    TeamChat.Domain, TeamChat.Application, TeamChat.Infrastructure and TeamChat.AspNetCore (the chat module),
                TeamChat.DemoHost.Api (the example host), and their test projects
src/frontend/   Angular workspace: projects/libs/chat (the chat library), projects/libs/shared (demo app libraries)
                and projects/apps/team-chat-demo-app
```

## License

[MIT](LICENSE)
