# ComRouter — Documentazione Architetturale

## Panoramica

**ComRouter** è un router di comunicazione seriale/TCP/UDP/Process.
Permette di definire regole (**Match**) del tipo: *"quando il Listener X riceve il comando A, manda B e C al Receiver Y"*.

Riscrittura completa dell'applicazione legacy (WinForms .NET Framework 4.8 monolitico) con architettura moderna:

| Layer | Tecnologia | Ruolo |
|---|---|---|
| **WebServer** | ASP.NET Core 9, SignalR | Logica di routing, API REST, hosting SPA |
| **Frontend** | React 19, Vite 6, TypeScript | UI web (matrix, listeners, receivers, log) |
| **Client desktop** | WinForms .NET 10 | Client opzionale, comunicazione solo via HTTP + SignalR |

---

## Struttura cartelle

```
comRouter/
├── Docs/
│   ├── README.md                      ← questa documentazione
│   └── WORKFLOW.md                    ← diagrammi mermaid flussi dati
├── setup/
│   ├── deploy.ps1                     ← script deploy completo
│   ├── installer.iss                  ← InnoSetup per Windows x64
│   ├── comRouter_install.sh           ← script installazione Linux ARM64 (sorgente)
│   └── version.json                   ← versione corrente (aggiornata automaticamente)
└── src/
    ├── Start-Dev.ps1                  ← avvio ambiente di sviluppo
    ├── Backend/                       ← soluzione .NET (CommRouter.slnx)
    │   ├── CommRouter.Interfaces/     ← net9.0 — DTOs, interfacce, nessuna dipendenza esterna
    │   ├── CommRouter.Core/           ← net9.0 — logica, plugin built-in, settings
    │   ├── CommRouter.WebServer/      ← net9.0 — ASP.NET Core, SignalR, Swagger, SPA hosting
    │   │   └── wwwroot/               ← OUTPUT build React (non committare)
    │   └── CommRouter/                ← net10.0-windows — WinForms client
    └── Frontend/                      ← React Vite app (TypeScript)
```

---

## Dipendenze tra progetti .NET

```
CommRouter (WinForms)    →  CommRouter.Interfaces, CommRouter.Core
CommRouter.WebServer     →  CommRouter.Interfaces, CommRouter.Core
CommRouter.Core          →  CommRouter.Interfaces
CommRouter.Interfaces    →  (nessuna)
```

> Il client WinForms referenzia Core/Interfaces a compile-time ma comunica con il WebServer
> **esclusivamente via HTTP REST e SignalR** a runtime (localhost:5025).

---

## Tecnologie e versioni

| Componente | Versione |
|---|---|
| .NET SDK | 10.0.202 |
| CommRouter.Interfaces / Core / WebServer | net9.0 |
| CommRouter (WinForms) | net10.0-windows |
| Solution file | CommRouter.slnx (formato .NET 10) |
| React / Vite | 19 / 6 |
| TypeScript | 5+ |
| Node | v24+ |

Pacchetti React chiave: `@tanstack/react-query`, `react-router-dom`, `react-hook-form`, `zod`, `@microsoft/signalr`, `axios`.

---

## Avvio in sviluppo

```powershell
.\src\Start-Dev.ps1
```

Avvia in tre finestre PowerShell separate:
1. `CommRouter.WebServer` (dotnet run) su porta 5025
2. `npm run dev` (Vite) su porta 5173 con proxy `/api` e `/hubs` → localhost:5025
3. `CommRouter.exe` (WinForms)

Comandi singoli:
```powershell
# Build .NET
Set-Location src\Backend
dotnet build CommRouter.slnx

# Build React (→ wwwroot/)
Set-Location src\Frontend
npm run build

# Dev React
npm run dev
```

---

## Porte e URL

| Servizio | URL |
|---|---|
| WebServer | http://localhost:5025 |
| Swagger UI | http://localhost:5025/swagger |
| React dev server | http://localhost:5173 |

Profilo avvio: `CommRouter.WebServer/Properties/launchSettings.json` (profilo `http`).

---

## Architettura WebServer

**`Program.cs`**: configura DI, CORS, Swagger, SignalR hub su `/hubs/router`, SPA fallback su `wwwroot/`.

**`RouterHostedService`**: all'avvio esegue:
1. `PluginLoader.Scan(AppContext.BaseDirectory)` — scopre plugin da tutte le DLL nella cartella
2. Carica config da `%APPDATA%\COMRouter\COMRouter.json` (o migra da XML se presente)
3. Avvia tutti i listener se `AutoStart = true`

### Controllers REST

| Controller | Route base | Note |
|---|---|---|
| `RouterController` | `GET /api/router/status` | Stato running + contatori |
| | `POST /api/router/start` / `stop` | Avvia/ferma tutti i listener |
| `ListenersController` | `/api/listeners` | CRUD completo |
| `ReceiversController` | `/api/receivers` | CRUD completo |
| `MatchesController` | `/api/matches` | CRUD completo |
| `TypesController` | `GET /api/types` | Plugin disponibili (da scan) |

### SignalR Hub — `/hubs/router`

| Evento | Direzione | Payload |
|---|---|---|
| `StateChanged` | Server → Client | — (il client ricarica tutto) |
| `LogEntry` | Server → Client | `LogEntryDto` (streaming real-time) |

---

## Plugin System

`PluginLoader.Scan(directory)` ispeziona tutte le `.dll` nella cartella, istanzia i tipi che implementano `IListener` o `IReceiver` e non sono astratti.

### Plugin built-in (CommRouter.Core.dll)

| Classe | Interfaccia | Parametri chiave |
|---|---|---|
| `RS232Listener` / `RS232Receiver` | IListener / IReceiver | PortName, BaudRate, DataBits, StopBits, Parity, Handshake |
| `TcpIpListener` / `TcpIpReceiver` | IListener / IReceiver | IpAddress, Port |
| `UdpListener` / `UdpReceiver` | IListener / IReceiver | IpAddress, Port |
| `ProcessReceiver` | IReceiver | FileName, Arguments |

**Plugin esterni**: copiare la DLL nella stessa cartella del WebServer — viene caricata automaticamente all'avvio.

---

## Configurazione

- **File primario**: `%APPDATA%\COMRouter\COMRouter.json`
- **Migrazione automatica**: se esiste `%APPDATA%\COMRouter\COMRouter.xml` viene letto e convertito in JSON (XML non viene mai modificato)
- **`AppSettings.AutoStart`**: se `true`, avvia tutti i listener all'apertura

---

## Logica di routing (Match)

```
Listener.DataReceived
  → Match.OnDataReceived()
      → IsMatchingData()     // OR tra ListenerCommands; vuoto = passa tutto
      → IProtocol.Send(ReceiverCommands, Receiver)
          → per ogni ReceiverCommand: Receiver.Send(bytes)
```

- **`ListenerCommands`** = filtro ingresso (OR). Vuoto = passa tutto.
- **`ReceiverCommands`** = sequenza comandi da inviare al receiver.
- Per "se A → manda B,C / se D → manda F,G": usare **2 Match distinti** sulla stessa coppia Listener/Receiver.

### AppProtocol — sintassi comandi

Token separati da virgola:

| Token | Significato |
|---|---|
| `56h` / `1Fh` | Byte esadecimale |
| `123` | Byte decimale (0–255) |
| `"AB"` | Stringa ASCII |
| `p` / `P` | Pausa 100 ms |
| `d` / `D` | Delay 100 ms |

---

## Frontend React

```
src/Frontend/src/
├── types/api.ts              ← DTOs TypeScript (specchio di ApiDtos.cs)
├── services/
│   ├── apiService.ts         ← axios wrapper per ogni risorsa
│   └── signalrService.ts     ← getHubConnection, startHub, stopHub
├── hooks/
│   ├── useListeners.ts       ← React Query CRUD
│   ├── useReceivers.ts
│   ├── useMatches.ts
│   ├── useRouterStatus.ts
│   ├── usePluginTypes.ts
│   └── useSignalR.ts         ← hub + logs[] (max 200 voci)
├── components/
│   ├── Modal.tsx
│   ├── CommandList.tsx        ← lista comandi editabile
│   └── LogPanel.tsx           ← log real-time
├── pages/
│   ├── Matrix/MatrixPage.tsx  ← griglia Listener × Receiver
│   ├── Listeners/ListenersPage.tsx
│   └── Receivers/ReceiversPage.tsx
├── router/index.tsx           ← / → Matrix, /listeners, /receivers
├── App.tsx                    ← sidebar + Outlet + LogPanel
└── main.tsx                   ← QueryClientProvider + RouterProvider
```

`vite.config.ts` imposta `outDir` direttamente su `../Backend/CommRouter.WebServer/wwwroot` — nessuna copia manuale.

---

## WinForms Client

Comunica **solo via HTTP REST e SignalR** su `localhost:5025`.

| File | Ruolo |
|---|---|
| `Program.cs` | Entry point: avvia `server\CommRouter.WebServer.exe` se presente, attende `GET /api/router/status` (max 30s), poi apre `frmMain` |
| `frmMain.cs` | Form principale: ToolStrip start/stop, TabControl Matrice/Listeners/Receivers/Log |
| `frmMatch.cs` | Dialog crea/modifica/elimina Match |
| `frmEndpoint.cs` | Dialog crea/modifica Listener o Receiver (pannello dinamico via `ControlPanelFactory`) |
| `Panels/RS232Panel` | UserControl configurazione RS232 |
| `Panels/TcpIpPanel` | UserControl configurazione TCP/IP |
| `Panels/ProcessPanel` | UserControl configurazione Process |
| `Panels/ControlPanelFactory` | Mappa tipo plugin → UserControl |

### Modalità dev vs installata

| | Dev (`Start-Dev.ps1`) | Installata (InnoSetup) |
|---|---|---|
| `server\CommRouter.WebServer.exe` | Assente | Presente in `{app}\server\` |
| Comportamento `Program.cs` | Connette direttamente a localhost:5025 | Lancia il WebServer, attende readiness, poi apre la form |
| WebServer terminato alla chiusura? | No | Sì (`Kill(entireProcessTree: true)`) |

---

## Deploy

### Script

```powershell
cd setup
.\deploy.ps1                   # build completo + FTP upload
.\deploy.ps1 -SkipFrontend     # salta npm build
.\deploy.ps1 -SkipFTP          # salta upload FTP
```

Workflow:
1. Incrementa `version.json` (minor++)
2. `npm run build` (React → wwwroot)
3. `dotnet publish` WebServer → `src/Backend/publish/server/` (win-x64, self-contained)
4. `dotnet publish` WinForms → `src/Backend/publish/` root (win-x64, self-contained)
5. `dotnet publish` WebServer → `src/Backend/publish/linux-arm64/` (linux-arm64, self-contained)
6. Copia `setup/comRouter_install.sh` in `linux-arm64/` normalizzando a LF
7. Crea `setup/Output/ComRouterLinux-arm64-<ver>.tar.gz`
8. ISCC → `setup/Output/ComRouterSetup.exe`
9. FTP upload: Setup.exe + tar.gz + version.json
10. `git commit && git push`

### Struttura installata — Windows

```
%ProgramFiles%\JBTechnology\ComRouter\
├── CommRouter.exe          ← WinForms (entry point utente)
└── server\
    ├── CommRouter.WebServer.exe
    └── wwwroot\            ← React SPA
```

### Struttura installata — Linux ARM64

```
/opt/comrouter/
├── CommRouter.WebServer    ← binario self-contained
└── wwwroot\                ← React SPA
```

Installazione: `sudo bash comRouter_install.sh`

Crea utente `comrouter`, installa systemd unit `comrouter.service` su porta 5025, `enable` + `restart`.

```bash
systemctl status comrouter
journalctl -u comrouter -f
# Web UI: http://<ip>:5025
```

---

## Note per lo sviluppo

### comRouter_install.sh
Il file sorgente è `setup/comRouter_install.sh` (committato con LF).
Lo script `deploy.ps1` lo legge, normalizza CRLF→LF e lo include nel tar.gz.
**Non modificare il file inline nel deploy.ps1** — modificare il sorgente `.sh`.

### wwwroot/
La cartella `src/Backend/CommRouter.WebServer/wwwroot/` è nell'output di Vite e **non va committata**.
Aggiungere a `.gitignore` se non già presente.

### Plugin scan
`PluginLoader.Scan` viene chiamato in `RouterHostedService.StartAsync`, non in `Program.cs`.
Questo garantisce che il DI container sia completamente inizializzato prima della scansione.

### Versioning
`version.json` usa il formato `major.minor.patch.build`.
Il deploy incrementa automaticamente il **minor** ad ogni esecuzione.