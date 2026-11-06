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

Early development. Setup instructions will be added as the code lands.

## License

[MIT](LICENSE)
