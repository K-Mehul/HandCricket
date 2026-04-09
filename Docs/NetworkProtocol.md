This document outlines the communication between the Unity Client and Nakama Server (Lua), along with implementation guides for C# integration.

---

## 1. Technical Architecture (Unity Side)

### NakamaService.cs (Initialization)
All networking flows through `NakamaService.cs`. This class initializes the `IClient` (HTTP) and `ISocket` (Real-time).

- **IClient**: Used for Request/Response (Login, RPCs, Storage).
- **ISocket**: Used for low-latency Real-time events (Matches, Notifications, Status).

---

## 2. Gameplay Match (match_handler.lua)
These OpCodes manage the flow of an individual match.

### Client -> Server (Actions)
| OpCode | Name | Payload Structure | Description |
| :--- | :--- | :--- | :--- |
| **1** | `TOSS_SELECT` | `{"selection": "Heads"}` | Pick Heads or Tails. |
| **2** | `DECISION_SELECT` | `{"choice": "Bat"}` | Winner of toss picks Bat or Bowl. |
| **3** | `PLAY_TURN` | `{"input": 4}` | Player submits hand number (1-6). |
| **4** | `GET_STATE` | `{}` | Request full state resync. |

### Server -> Client (Updates)
| OpCode | Name | Payload Structure | Description |
| :--- | :--- | :--- | :--- |
| **100** | `TOSS_START` | `{"initiator_id": "..."}` | Tells clients to show the Toss screen. |
| **101** | `TOSS_RESULT` | `{"toss_winner": "...", "outcome": "Heads"}` | Shows who won the toss. |
| **102** | `GAME_START` | `{"batting_player": "...", "p1_id": "..."}` | Switches to Gameplay UI. |
| **103** | `TURN_RESULT` | `{"event": "RUNS", "bat_input": 4, "bowl_input": 1}` | Result of a single ball. |
| **104** | `INNINGS_BREAK`| `{"target": 45, "message": "..." }` | Switch from Innings 1 to 2. |
| **105** | `GAME_OVER` | `{"winner": "max", "reason": "Defended"}` | Match has ended. |
| **106** | `MATCH_SUMMARY`| `{"coins_earned": 50, "xp_earned": 100}` | Personal rewards & XP updates. |

---

## 3. Tournament Manager (tournament_handle.lua)
These OpCodes manage the bracket and are distinct from individual match messages to avoid "cross-talk".

| OpCode | Name | Payload Structure | Description |
| :--- | :--- | :--- | :--- |
| **121** | `BRACKET_UPDATE`| `{"round": 1, "matches": [...]}` | Live updates to the tournament bracket. |
| **122** | `MATCH_READY` | `{"match_id": "...", "opponent": "myth"}` | Tells client a bracket match is ready to join. |
| **123** | `TOURN_SUMMARY`| `{"winner_name": "max", "prize": 500}` | Finale: Shows the overall winner and prize. |

---

## 4. Logical Game Flows

### A. Normal Match Flow (Global/Private)
1. **Matchmaking**: Clients add themselves to the matchmaker.
2. **Found**: Server sends `MatchmakingMatched`.
3. **Join**: Clients join the socket match.
4. **Toss**: Server sends `100 (Start)`, Clients send `1 (Select)`, Server sends `101 (Result)`.
5. **Gameplay**: Clients play turns via `Op 3`.

### B. Tournament Flow (Multi-Match)
1. **Lobby**: Players join the Manager Match and see the live bracket (**Op 121**).
2. **Match Ready**: When both opponents are available, manager spawns sub-match and sends **Op 122**.
3. **Sub-Match**: Players leave the manager match temporarily or open a second connection and play the game using **Op 100-106**.
4. **Advancing**: When a sub-match ends, it signals the Manager. The Manager updates the bracket (**Op 121**).
5. **Champion**: Final winner receives the prize and a summary (**Op 123**).

---

## 5. RPC & Social API Reference

### A. Custom RPCs
| RPC ID | Payload | Description |
| :--- | :--- | :--- |
| `list_tournaments` | `{}` | Returns all tournament metadata. |
| `join_tournament` | `{"tournament_id": "...", "host_id": "..."}` | Pays fee and registers user. |
| `send_social_notification` | `{"user_id": "...", "code": 20, "content": "{}"}` | Sends a direct real-time notification. |

---

## 6. UI Notification System (NotificationUI.cs)
- **Standard Popups**: Used for game alerts.
- **Auto-Dismiss**: All notifications automatically hide after **10 seconds** to prevent UI clutter.
- **Dismissible**: Users can click the screen or a button to hide manually.
