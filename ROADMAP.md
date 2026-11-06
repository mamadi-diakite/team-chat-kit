# Roadmap

This is the working plan for team-chat-kit. Stories are grouped by epic and tagged with the milestone they belong to.

## Scope

In scope:
- A chat module (.NET libraries with a SignalR hub, and an Angular library) that a host application adds with a few lines and an identity adapter.
- Organization directory, invitations between organizations, contacts and presence.
- One-to-one and group conversations, group members and roles, saved and pinned messages.

Out of scope:
- No audio or video calls, no screen sharing.
- No end-to-end encryption. Messages are stored in the host's database.
- No file sharing in the first version.
- No mobile push notifications.
- No real user data. The demo runs on fictional organizations, accounts and messages.

## Integration model

The chat is split from the demo host on purpose. The chat projects (`TeamChat.*` on the back end, the `chat` library on the front end) never reference the demo host. A host application adds the chat with `AddTeamChat()` and `MapTeamChat()`, implements one identity adapter (current user, user details, organization), and shows the Angular chat component. The demo host in this repository is one example of such a host.

## Glossary

| Term | Meaning |
|------|---------|
| Host application | The existing app the chat is added to. It owns users and login |
| Chat module | The .NET libraries and the Angular library that make up the chat |
| Identity adapter | The interface the host implements to give the chat the current user and user details |
| Organization | A group of users, for example a company. Members of the same organization see each other in the directory |
| Directory | The list of members of my organization |
| Contact | A person from another organization who accepted my invitation |
| Invitation | A request to become contacts, which the other person accepts or declines |
| Presence | Online, away or offline, computed from live connections and activity |
| Conversation | A one-to-one chat or a group |
| Group | A conversation with a name and several members. Owners manage members |
| Saved message | A message I bookmark for myself |
| Pinned message | A message pinned in a conversation, visible to all its members |

## Milestones

| Milestone | Goal |
|-----------|------|
| M0 Foundation | Runnable demo host with accounts and organizations, chat module wired in, directory, invitations and contacts |
| M1 Messaging | One-to-one conversations, real-time messages, unread counts, presence |
| M2 Groups | Groups, members and roles, leave, system messages |
| M3 Message tools | Edit and delete, save, pin, search, typing indicator, read status |
| M4 Integration and quality | Integration guide, packaging, CI, tests, accessibility |

---

## EPIC 1 — Project setup

| ID | Story | Milestone |
|----|-------|-----------|
| SETUP-1 | Repository with README, LICENSE (MIT), .gitignore, .editorconfig | M0 |
| SETUP-2 | .NET solution: chat Domain, Application, Infrastructure and AspNetCore projects, demo host API, Tests projects | M0 |
| SETUP-3 | SQLite + EF Core, chat tables in their own DbContext and migrations, database created on startup in dev | M0 |
| SETUP-4 | Angular workspace with the `chat` library and the demo host app | M0 |
| SETUP-5 | GitHub Actions: build and test both back end and front end on each push | M4 |

## EPIC 2 — Integration contract

| ID | Story | Milestone |
|----|-------|-----------|
| HOST-1 | As a host developer, I add the chat with `AddTeamChat()` and `MapTeamChat()` and a connection string | M0 |
| HOST-2 | As a host developer, I implement an identity adapter: current user, user details, organization | M0 |
| HOST-3 | The chat reuses the host's authentication (JWT bearer) for its HTTP endpoints and its SignalR hub | M0 |
| HOST-4 | As a host developer, I set the route prefix and the table prefix of the chat | M0 |
| HOST-5 | As a host developer, I add the Angular chat with a provider function and one component | M1 |
| HOST-6 | As a host developer, I theme the chat with CSS variables | M4 |
| HOST-7 | Integration guide with a step-by-step example on a blank ASP.NET Core and Angular app | M4 |

Acceptance notes:
- The chat module has no reference to the demo host. A test host in the test suite proves it works with another adapter.

## EPIC 3 — Demo host accounts

| ID | Story | Milestone |
|----|-------|-----------|
| AUTH-1 | As a visitor, I create an account with name, email, password and organization | M0 |
| AUTH-2 | As a user, I log in and get a token | M0 |
| AUTH-3 | As a user, I log out, and I am sent back to login when my session expires | M0 |
| AUTH-4 | As a user, I change my password | M0 |
| AUTH-5 | Demo organizations and accounts are seeded with a documented demo password | M0 |

Acceptance notes:
- Passwords are hashed. Demo emails use `@example.com`.

## EPIC 4 — Directory and contacts

| ID | Story | Milestone |
|----|-------|-----------|
| CONTACT-1 | As a user, I see the directory of my organization and search it by name | M0 |
| CONTACT-2 | As a user, I invite someone from another organization by email, with an optional message | M0 |
| CONTACT-3 | As a user, I see my received and sent invitations, and accept, decline or cancel them | M0 |
| CONTACT-4 | As a user, I see my contacts and remove one | M0 |
| CONTACT-5 | As a user, I block someone. They cannot invite me or message me | M3 |

Acceptance notes:
- An invitation to an unknown email or to a member of my own organization is rejected with a clear message.
- Only one pending invitation per pair of users.

## EPIC 5 — Presence

| ID | Story | Milestone |
|----|-------|-----------|
| PRES-1 | As a user, I see who is online, away or offline in my directory, contacts and conversations | M1 |
| PRES-2 | Presence updates live when people connect or disconnect, across several tabs | M1 |
| PRES-3 | I become away after a period without activity | M1 |
| PRES-4 | As a user, I set my status to busy or appear offline | M3 |

## EPIC 6 — Messaging

| ID | Story | Milestone |
|----|-------|-----------|
| MSG-1 | As a user, I start a one-to-one conversation with a directory member or a contact | M1 |
| MSG-2 | As a user, I send a message and the other person gets it in real time | M1 |
| MSG-3 | As a user, I see my conversations ordered by last message, with unread counts | M1 |
| MSG-4 | As a user, I scroll back through older messages, loaded by page | M1 |
| MSG-5 | Messages sent while I was offline are there when I come back | M1 |
| MSG-6 | Message text is plain text with links made clickable; HTML is never rendered | M1 |
| MSG-7 | As a user, I see when the other person is typing | M3 |
| MSG-8 | As a user, I see whether my message was read | M3 |

## EPIC 7 — Groups

| ID | Story | Milestone |
|----|-------|-----------|
| GROUP-1 | As a user, I create a group with a name and members from my directory and contacts | M2 |
| GROUP-2 | As a group owner, I add and remove members | M2 |
| GROUP-3 | As a group owner, I rename the group and make another member owner | M2 |
| GROUP-4 | As a member, I leave a group. The last owner must name another owner first | M2 |
| GROUP-5 | Member changes appear in the conversation as system messages | M2 |
| GROUP-6 | A removed member no longer receives messages and cannot read new ones | M2 |

## EPIC 8 — Message tools

| ID | Story | Milestone |
|----|-------|-----------|
| TOOL-1 | As an author, I edit or delete my message. Others see "edited" or "deleted" | M3 |
| TOOL-2 | As a user, I save a message and find it in my saved messages list | M3 |
| TOOL-3 | As a member, I pin a message. Pinned messages are listed at the top of the conversation | M3 |
| TOOL-4 | As a member, I unpin a message. Only the person who pinned it or a group owner can unpin | M3 |
| TOOL-5 | As a user, I search messages in a conversation | M3 |

## EPIC 9 — Quality and docs

| ID | Story | Milestone |
|----|-------|-----------|
| QA-1 | Integration tests for the HTTP endpoints and the SignalR hub, including access rules | M4 |
| QA-2 | Unit tests for the Angular library and the demo host | M4 |
| QA-3 | End-to-end test: two users, invitation, conversation, real-time message (Playwright) | M4 |
| QA-4 | Accessibility pass on the chat panel (keyboard, screen reader announcements) | M4 |
| QA-5 | Docs: architecture, integration guide, demo accounts | M4 |

---

## Later ideas

- File and image sharing with previews
- Reactions and replies in threads
- Email digest of unread messages
- Mentions with notifications
- Packages on NuGet and npm
- French and English UI
