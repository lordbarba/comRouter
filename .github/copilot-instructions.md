# ComRouter — Copilot Context

Questo file viene caricato automaticamente da GitHub Copilot.
Contiene tutto il contesto necessario per continuare lo sviluppo da qualsiasi macchina.

---

## Descrizione del progetto

**ComRouter** è un router di comunicazione seriale/TCP/UDP/Process.
Permette di definire regole (Match) del tipo: "quando il Listener X riceve il comando A, manda B e C al Receiver Y".

Riscrittura completa dell'applicazione legacy `Source/` (WinForms .NET 4.8 monolitico) con architettura moderna:
- Backend ASP.NET Core 9 con API REST + SignalR
- Frontend React (Vite 6 + TypeScript)
- Client WinForms .NET 10
- Plugin system dinamico (assembly scan)
- Config JSON (migrazione automatica da XML legacy)

---

## Struttura cartelle

```
comRouter/
  Docs/                        ← README.md (architettura), WORKFLOW.md (diagrammi)
  setup/
    deploy.ps1                 ← script deploy completo (Windows + Linux ARM64)
    installer.iss              ← InnoSetup per Windows x64
    comRouter_install.sh       ← script installazione Linux ARM64 (sorgente, LF)
    version.json               ← versione corrente (incrementata da deploy.ps1)
  src/
    Backend/                   ← soluzione .NET (CommRouter.slnx)
      CommRouter.Interfaces/   ← net9.0 — DTOs, interfacce, nessuna dipendenza esterna
      CommRouter.Core/         ← net9.0 — logica, plugin built-in, settings, EF-free
      CommRouter.WebServer/    ← net9.0 — ASP.NET Core, SignalR, Swagger, wwwroot SPA
        wwwroot/               ← OUTPUT build React (non committare)
      CommRouter/              ← net10.0-windows — WinForms client
    Frontend/                  ← React Vite app (TypeScript)
    Start-Dev.ps1              ← avvio dev (WebServer + Vite + WinForms)
```

---

## Dipendenze di progetto

```
CommRouter (WinForms)    → CommRouter.Interfaces, CommRouter.Core
CommRouter.WebServer     → CommRouter.Interfaces, CommRouter.Core
CommRouter.Core          → CommRouter.Interfaces
CommRouter.Interfaces    → (nessuna)
```

---

## Tecnologie e versioni

- .NET SDK 10.0.202
- CommRouter.Interfaces / CommRouter.Core / CommRouter.WebServer: `net9.0`
- CommRouter (WinForms): `net10.0-windows`
- Solution file: `CommRouter.slnx` (formato .NET 10)
- React: Vite 6, TypeScript, `@tanstack/react-query`, `react-router-dom`, `react-hook-form`, `zod`, `@microsoft/signalr`, `axios`
- Node: v24+

---

## Comandi utili

```powershell
# Avvio completo ambiente di sviluppo
.\src\Start-Dev.ps1

# Build solo .NET
Set-Location src\Backend
dotnet build CommRouter.slnx

# Build solo React (output → CommRouter.WebServer/wwwroot/)
Set-Location src\Frontend
npm run build

# Dev React (proxy /api e /hubs → localhost:5025)
npm run dev

# Deploy completo (build + installer Windows + tar.gz Linux + FTP + git push)
Set-Location setup
.\deploy.ps1
.\deploy.ps1 -SkipFrontend   # salta npm build
.\deploy.ps1 -SkipFTP        # salta upload FTP
```

---

## Porte

| Servizio | URL |
|----------|-----|
| WebServer (http) | http://localhost:5025 |
| React dev server | http://localhost:5173 |

Il profilo `http` è in `CommRouter.WebServer/Properties/launchSettings.json`.

---

## Architettura WebServer

**`Program.cs`**: DI, CORS, Swagger, SignalR hub su `/hubs/router`, SPA fallback su `wwwroot/`.

**`RouterHostedService`**: all'avvio chiama `PluginLoader.Scan(AppContext.BaseDirectory)` per scoprire i plugin, poi carica config JSON (o migra da XML), poi avvia se `AutoStart = true`.

**Controllers**: `RouterController`, `ListenersController`, `ReceiversController`, `MatchesController`, `TypesController`.

**SignalR events** (broadcast a tutti i client):
- `StateChanged` — su ogni modifica strutturale (add/remove/start/stop)
- `LogEntry(LogEntryDto)` — streaming log in tempo reale

---

## Plugin system

`PluginLoader.Scan(directory)` cerca tutti i `.dll` nella cartella, istanzia i tipi che implementano `IListener` o `IReceiver`.

**Plugin built-in** (in `CommRouter.Core.dll`):
- `RS232Listener`, `RS232Receiver` — porta COM seriale
- `TcpIpListener`, `TcpIpReceiver` — TCP
- `UdpListener`, `UdpReceiver` — UDP
- `ProcessReceiver` — lancia un processo

**Plugin esterni**: basta copiare la DLL nella stessa cartella del WebServer.

Basi astratte: `RS232Base` (PortName, BaudRate, DataBits, StopBits, Parity, Handshake), `TcpIpBase` (IpAddress, Port).

---

## Configurazione

- File primario: `%APPDATA%\COMRouter\COMRouter.json`
- Migrazione automatica da: `%APPDATA%\COMRouter\COMRouter.xml` (sola lettura, viene salvato in JSON)
- `AppSettings.AutoStart` — avvia tutti i listener all'apertura

---

## Logica di routing (Match)

```
Listener.DataReceived
  → Match.OnDataReceived()
      → IsMatchingData()   // OR tra ListenerCommands; se vuoto passa tutto
      → IProtocol.Send(ReceiverCommands, Receiver)
          → per ogni ReceiverCommand: Receiver.Send(bytes)
```

- `ListenerCommands` = filtro in ingresso (OR). Vuoto = passa tutto.
- `ReceiverCommands` = comandi da inviare in sequenza al receiver.
- Per "se ricevo A manda B,C / se ricevo D manda F,G" → **2 Match distinti** sulla stessa coppia Listener/Receiver.

**AppProtocol** (`ParseCrossing`): interpreta token separati da virgola:
- `56h` / `1Fh` = byte esadecimale
- `123` = byte decimale
- `"AB"` = stringa ASCII
- `p`/`P` = pausa 100ms
- `d`/`D` = delay 100ms

---

## Frontend React

```
src/
  types/api.ts           ← DTOs TypeScript (specchio di ApiDtos.cs)
  services/
    apiService.ts        ← axios, un oggetto per ogni risorsa
    signalrService.ts    ← getHubConnection, startHub, stopHub, onStateChanged, onLogEntry
  hooks/
    useListeners.ts      ← React Query CRUD
    useReceivers.ts
    useMatches.ts
    useRouterStatus.ts
    usePluginTypes.ts
    useSignalR.ts        ← hub connection + logs[] (max 200)
  components/
    Modal.tsx            ← dialog riusabile
    CommandList.tsx      ← lista comandi editabile
    LogPanel.tsx         ← log real-time
  pages/
    Matrix/MatrixPage.tsx    ← griglia Listener x Receiver, doppio click = crea/modifica match
    Listeners/ListenersPage.tsx
    Receivers/ReceiversPage.tsx
  router/index.tsx       ← createBrowserRouter: / Matrix, /listeners, /receivers
  App.tsx                ← sidebar + Outlet + LogPanel, invalida queries su StateChanged
  main.tsx               ← QueryClientProvider + RouterProvider
```

---

## WinForms Client

Comunica **solo via HTTP REST e SignalR** su `localhost:5025`. Non referenzia `CommRouter.WebServer`.

- `Program.cs` — entry point: cerca `server\CommRouter.WebServer.exe` accanto all'exe; se presente lo avvia e attende `GET /api/router/status` (max 30s) prima di aprire `frmMain`. In dev il WebServer è già avviato separatamente.
- `frmMain` — form principale: ToolStrip (start/stop), TabControl (Matrice/Listeners/Receivers/Log), StatusStrip
- `frmMatch` — dialog crea/modifica/elimina Match
- `frmEndpoint` — dialog crea/modifica Listener o Receiver (carica pannello config dinamico via `ControlPanelFactory`)
- `Panels/RS232Panel`, `TcpIpPanel`, `ProcessPanel` — UserControl per configurazione plugin
- `Panels/ControlPanelFactory` — mappa tipo plugin → pannello UI

---

## Deploy

### Windows x64 — InnoSetup
`setup/Output/ComRouterSetup.exe` installa in `%ProgramFiles%\JBTechnology\ComRouter\`:
```
CommRouter.exe          ← WinForms (entry point utente)
server\
  CommRouter.WebServer.exe
  wwwroot\              ← React SPA
```

### Linux ARM64 — tar.gz
`setup/Output/ComRouterLinux-arm64-<ver>.tar.gz` contiene il WebServer self-contained + `comRouter_install.sh`.

```bash
tar -xzf ComRouterLinux-arm64-*.tar.gz
sudo bash comRouter_install.sh
# → installa in /opt/comrouter/, crea utente comrouter, systemd unit su porta 5025
```

### Workflow deploy.ps1
1. Incrementa minor in `version.json`
2. `npm run build` (React → wwwroot)
3. `dotnet publish` WebServer → `publish/server/` (win-x64)
4. `dotnet publish` WinForms → `publish/` root (win-x64)
5. `dotnet publish` WebServer → `publish/linux-arm64/` (linux-arm64)
6. Copia `setup/comRouter_install.sh` → `publish/linux-arm64/` (normalizza CRLF→LF)
7. `tar -czf` → `setup/Output/ComRouterLinux-arm64-<ver>.tar.gz`
8. ISCC → `setup/Output/ComRouterSetup.exe`
9. FTP upload: Setup.exe + tar.gz + version.json
10. `git commit && git push`

### Note importanti
- `comRouter_install.sh` è il file sorgente bash committato in `setup/`. **Non modificare** il contenuto inline nel deploy.ps1.
- `wwwroot/` non va committato (è output di build).
- `PluginLoader.Scan` viene chiamato in `RouterHostedService.StartAsync`, non in `Program.cs`.

---

## Stato attuale

- [x] CommRouter.Interfaces — completo
- [x] CommRouter.Core — completo (plugin, settings, migrazione XML)
- [x] CommRouter.WebServer — completo (API REST, SignalR, Swagger, SPA serving)
- [x] Frontend React — build ok, output in wwwroot/
- [x] CommRouter WinForms — completo (frmMain, frmMatch, frmEndpoint, panels)
- [x] Program.cs WinForms — avvia WebServer se in modalità installata, attende readiness
- [x] Start-Dev.ps1 — build .NET + avvio 3 processi separati
- [x] deploy.ps1 — Windows installer + Linux ARM64 tar.gz + FTP + git push
- [x] installer.iss — InnoSetup per Windows x64 (CommRouter.exe + server\)
- [x] comRouter_install.sh — installazione Linux ARM64 con systemd unit

## TODO / Possibili miglioramenti

- [ ] Testare end-to-end con hardware reale (es. scheda relè USB SNT319 su porta COM)
- [ ] Aggiungere autenticazione API (Bearer token / API key)
- [ ] Aggiungere logging su file (Serilog)
- [ ] Unit test su AppProtocol e Match
- [ ] Splash screen WinForms durante attesa avvio WebServer
