# OpenGameCloud — Quick Start Guide

## Installation

1. Add the following to your `Packages/manifest.json`:
```json
"com.endel.nativewebsocket": "https://github.com/endel/NativeWebSocket.git#upm",
"com.fapoli.opengamecloud": "https://github.com/fapoli/opengamecloud-unity.git"
```
Newtonsoft.Json is included automatically as a transitive dependency.

2. Add the `OgcClient` prefab to the **first scene** of your project. It will persist across all scenes automatically.

3. In the Inspector, fill in your **API Key** and **Project ID** from the [OpenGameCloud dashboard](https://opengamecloud.com).

---

## Setup

All functionality is accessed through static methods on `OgcClient`:

```csharp
OgcClient.Auth       // Authentication
OgcClient.Sessions   // Multiplayer sessions & real-time events
OgcClient.HighScores // Leaderboards
OgcClient.PlayerData // Per-user cloud saves
```

---

## Authentication

Before doing anything, authenticate your player. The simplest option is a **guest** account — no registration required.

```csharp
async void Start() {
    await OgcClient.Auth.Guest();
}
```

For registered users:

```csharp
// Register
await OgcClient.Auth.Register("player@email.com", "password123");

// Login
await OgcClient.Auth.Login("player@email.com", "password123");
```

---

## Steam Authentication

If your game is already using Steamworks, you can authenticate players with their Steam identity. This gives them a persistent OpenGameCloud account tied to their Steam ID — unlocking full access to cloud saves, leaderboards, and multiplayer sessions across sessions and devices.

The flow is simple: request a session ticket from Steamworks and pass it to OpenGameCloud. The API validates it directly with Steam.

```csharp
using Steamworks;
using Net.OpenGameCloud;

async void Start() {
    // Request an auth ticket from Steamworks
    var ticket = SteamUser.GetAuthSessionTicket();
    var ticketHex = BitConverter.ToString(ticket.Data, 0, (int)ticket.m_cbSize)
                                .Replace("-", "").ToLower();

    // Authenticate with OpenGameCloud
    var auth = await OgcClient.Auth.Steam(ticketHex);

    if (auth != null)
        Debug.Log($"Logged in as {auth.user.id}");
}
```

Once authenticated, the player's Steam identity is linked to their OpenGameCloud account. All subsequent calls to `OgcClient.PlayerData` and `OgcClient.HighScores` are automatically scoped to that player.

> **Note:** Steam authentication requires your project to have a Steam App ID configured in the OpenGameCloud dashboard.

---

## Sessions

Sessions are the core of OpenGameCloud. A session is a real-time multiplayer room where players can send events, share state, and track who is online.

### Creating or Joining a Session

**QuickJoin** — automatically joins an existing open session or creates one if none are available. Great for matchmaking.

```csharp
var session = await OgcClient.Sessions.QuickJoin();
```

**Create** — explicitly create a new session.

```csharp
var session = await OgcClient.Sessions.Create(label: "Room 1");
```

**List** — browse available sessions and let the player pick one.

```csharp
var sessions = await OgcClient.Sessions.List();
foreach (var s in sessions)
    Debug.Log($"{s.label} — {s.activeUserCount}/{s.playerLimit} players");
```

### Connecting

After getting a session, connect to it to start receiving real-time events. Subscribe to events **before** calling `Connect`.

```csharp
async void Start() {
    await OgcClient.Auth.Guest();

    var session = await OgcClient.Sessions.QuickJoin();

    OgcClient.Sessions.OnJoinedSession += msg => {
        Debug.Log($"Connected! {msg.activeUsers.Length} players online");
    };

    OgcClient.Sessions.OnPlayerJoin += msg => {
        Debug.Log($"{msg.userId} joined");
    };

    OgcClient.Sessions.OnPlayerLeft += msg => {
        Debug.Log($"{msg.userId} left");
    };

    OgcClient.Sessions.OnError += error => {
        Debug.LogError($"Session error: {error}");
    };

    await OgcClient.Sessions.Connect(session.id);
}
```

You can also pass **metadata** when connecting — useful for attaching a display name, character, or any player info visible to others in the session.

```csharp
await OgcClient.Sessions.Connect(session.id, new {
    name = "Player1",
    character = "warrior"
});
```

Other players receive this in `OnUserJoined`:

```csharp
public class PlayerInfo {
    public string name;
    public string character;
}

OgcClient.Sessions.OnUserJoined += msg => {
    var info = msg.GetMetadata<PlayerInfo>();
    Debug.Log($"{info.name} joined as {info.character}");
};
```

### Sending & Receiving Events

Events are the main way players communicate in a session. Use any `eventType` string to categorize them, and attach any serializable object as the payload.

```csharp
// Send to everyone
OgcClient.Sessions.SendEvent("player_moved", new {
    col = 4,
    row = 2
});

// Send to specific players only — the event type is the same, targetUserIds is what makes it private
OgcClient.Sessions.SendEvent("chat_message", new {
    text = "Hey!"
}, targetUserIds: new[] { "user-id-123" });
```

Receive events from other players:

```csharp
public class MovePayload {
    public int col;
    public int row;
}

OgcClient.Sessions.OnSessionEvent += msg => {
    switch (msg.sessionEvent.eventType) {
        case "player_moved":
            var move = msg.sessionEvent.GetPayload<MovePayload>();
            Debug.Log($"Player {msg.sessionEvent.userId} moved to ({move.col}, {move.row})");
            break;

        case "chat_message":
            var chat = msg.sessionEvent.GetPayload<ChatMessage>();
            Debug.Log($"{chat.sender}: {chat.text}");
            break;
    }
};
```

### Updating Your Metadata

Update your own presence metadata at any time — useful for status changes like ready state or current score.

```csharp
OgcClient.Sessions.UpdateMetadata(new {
    name = "Player1",
    ready = true
});
```

Others receive this via `OnMetadataUpdated`:

```csharp
OgcClient.Sessions.OnMetadataUpdated += msg => {
    var meta = msg.GetMetadata<PlayerInfo>();
    Debug.Log($"{msg.userId} is now ready: {meta.ready}");
};
```

### Disconnecting

```csharp
await OgcClient.Sessions.Disconnect();
```

---

## High Scores

Submitting scores requires a **registered account with a verified email, or Steam authentication** — guest accounts do not have access to this feature. The leaderboard itself is public and can be read by anyone.

```csharp
// Submit score (only the player's best score is kept)
await OgcClient.HighScores.Submit(4200f);

// Get top 10
var leaderboard = await OgcClient.HighScores.GetLeaderboard(limit: 10);
foreach (var entry in leaderboard)
    Debug.Log($"{entry.username}: {entry.bestScore}");
```

---

## Player Data

Per-user cloud saves. Store any JSON-serializable object under a string key. Requires a **registered account with a verified email, or Steam authentication** — guest accounts do not have access to this feature.

```csharp
[System.Serializable]
public class SaveData {
    public int level;
    public float health;
    public string[] unlockedItems;
}

// Save
await OgcClient.PlayerData.Save("save1", new SaveData {
    level = 5,
    health = 80f,
    unlockedItems = new[] { "sword", "shield" }
});

// Load
var save = await OgcClient.PlayerData.Load<SaveData>("save1");
Debug.Log($"Level: {save.level}");

// List saved keys
var keys = await OgcClient.PlayerData.ListKeys();

// Delete
await OgcClient.PlayerData.Delete("save1");
```

---

## Project Configuration

These settings are managed from the [OpenGameCloud dashboard](https://opengamecloud.com) and apply to your project globally. They cannot be changed at runtime from the client.

| Setting | Description | Default |
|---|---|---|
| **Player limit per session** | Maximum number of players allowed in a single session. | 6 |
| **Disconnect behavior** | What happens when all players leave a session. `end_session` deletes it immediately. `none` keeps the session alive for a while after everyone leaves. | `end_session` |
| **Empty session TTL** | How many seconds an empty session stays alive before being automatically deleted. Range: 0–3600. | 300 |
| **Join history limit** | Number of past session events sent to a player when they connect. Useful for catching up on recent activity (e.g. last 20 chat messages). Set to 0 to disable. Max 100. | 0 |
| **Steam App ID** | Your game's Steam App ID. Required to enable Steam authentication. | — |

---

## Full Example — Multiplayer Chat

```csharp
using Net.OpenGameCloud;
using UnityEngine;

public class ChatRoom : MonoBehaviour {

    public class ChatMessage {
        public string text;
        public string sender;
    }

    async void Start() {
        await OgcClient.Auth.Guest();

        var session = await OgcClient.Sessions.QuickJoin();

        OgcClient.Sessions.OnJoinedSession += msg =>
            Debug.Log($"Joined! {msg.activeUsers.Length} players online");

        OgcClient.Sessions.OnPlayerJoin += msg =>
            Debug.Log($"{msg.userId} joined");

        OgcClient.Sessions.OnPlayerLeft += msg =>
            Debug.Log($"{msg.userId} left");

        OgcClient.Sessions.OnSessionEvent += msg => {
            switch (msg.sessionEvent.eventType) {
                case "chat_message":
                    var chat = msg.sessionEvent.GetPayload<ChatMessage>();
                    Debug.Log($"{chat.sender}: {chat.text}");
                    break;
            }
        };

        await OgcClient.Sessions.Connect(session.id);
    }

    public void SendMessage(string text) {
        OgcClient.Sessions.SendEvent("chat_message", new ChatMessage {
            text = text,
            sender = "Player1"
        });
    }
}
```
