using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace panelAIR1000
{
    /// <summary>
    /// Pantalles i tecles de les MCDU del FlyByWire A32NX (MSFS) a través del servidor MCDU del SimBridge
    /// (ws://localhost:8380/interfaces/v1/mcdu; el mateix que fa servir la MCDU remota del SimBridge). L'avió s'hi
    /// connecta sol i el servidor reenvia els missatges entre l'avió i els altres clients (comprovat al mcdu.js de l'avió):
    ///  - "requestUpdate" → l'avió respon "update:{left:{…}, right:{…}}" (i n'envia un a cada canvi de pàgina).
    ///    Cada costat: lines (12 × [esquerra, dreta, centre]: etiqueta, línia, etiqueta…), scratchpad, title, titleLeft,
    ///    page ("{small}1/2{end}") i arrows [↑, ↓, ←, →]. Dins del text: {color}…{end}, {small}/{big}…{end}, {sp} espai;
    ///    "{" sol és la fletxa ←, "}" la →, "_" la casella àmbar.
    ///  - "event:left:DIR" / "event:right:A"… → tecla de la MCDU 1 / 2 (H:A320_Neo_CDU_n_BTN_…).
    /// </summary>
    internal sealed class FbwMcduLink : IDisposable
    {
        const string WsUrl = "ws://localhost:8380/interfaces/v1/mcdu";

        /// <summary>Pantalla d'una MCDU (1 capità, 2 primer oficial) (al fil de la interfície).</summary>
        public event Action<int, McduScreen>? McduChanged;
        /// <summary>true mentre hi ha connexió amb el SimBridge i ja ha arribat alguna pantalla de l'avió.</summary>
        public bool Connected { get; private set; }

        readonly McduScreen?[] _last = new McduScreen?[2];
        readonly SemaphoreSlim _sendLock = new(1, 1);
        SynchronizationContext? _ui;
        CancellationTokenSource? _cts;
        ClientWebSocket? _ws;

        public void Start()
        {
            if (_cts != null) return;
            _ui = SynchronizationContext.Current;
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            Task.Run(() => RunAsync(ct));
        }

        public void Stop()
        {
            try { _cts?.Cancel(); } catch { }
            _cts = null;
            Connected = false;
            _last[0] = _last[1] = null;
        }

        public void Dispose() => Stop();

        // (després d'Stop ja no es publica res: si no, una pantalla pendent tornaria a sortir un cop netejada)
        void Post(Action a) { if (_ui != null) _ui.Post(_ => { if (_cts != null) a(); }, null); else a(); }

        async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                bool opened = false;
                try { await SessionAsync(ct, () => opened = true); }
                catch { /* el SimBridge no hi és o s'ha tancat: es torna a provar */ }
                Connected = false;
                if (!ct.IsCancellationRequested && !opened) ShowMessage(NoSimBridgeMsg);
                _ws = null;
                try { await Task.Delay(3000, ct); } catch { }
            }
        }

        // ── Missatges a la pantalla de la MCDU quan l'app i l'avió no es troben (text, color, petita) ──
        static readonly (string, char, bool)[] NoSimBridgeMsg =
        {
            ("SIMBRIDGE", 'w', false), ("", 'w', false),
            ("SIMBRIDGE NOT FOUND", 'a', false), ("", 'w', false),
            ("START FBW SIMBRIDGE", 'w', true), ("FROM THE FBW INSTALLER", 'w', true),
            ("(SIMBRIDGE TAB, START)", 'w', true), ("", 'w', false),
            ("PANELAIR CONNECTS TO", 'b', true), ("LOCALHOST PORT 8380", 'b', true),
        };
        static readonly (string, char, bool)[] NoAircraftMsg =
        {
            ("SIMBRIDGE", 'w', false), ("", 'w', false),
            ("AIRCRAFT NOT CONNECTED", 'a', false), ("TO SIMBRIDGE", 'a', false),
            ("", 'w', false),
            ("ON THE FLYPAD:", 'w', true), ("SETTINGS > SIM OPTIONS", 'w', true),
            ("SIMBRIDGE: OFF, THEN", 'w', true), ("AUTO ON", 'w', true), ("", 'w', false),
            ("WAIT FOR THE MESSAGE", 'b', true), ("MCDU CONNECTED", 'b', true),
        };

        static readonly (string, char, bool)[] NoDataMsg =
        {
            ("SIMBRIDGE", 'w', false), ("", 'w', false),
            ("NO MCDU DATA", 'a', false), ("", 'w', false),
            ("AIRCRAFT DISCONNECTED", 'w', true), ("FROM SIMBRIDGE OR", 'w', true), ("MCDU WITHOUT POWER", 'w', true),
            ("", 'w', false),
            ("ON THE FLYPAD:", 'w', true), ("SETTINGS > SIM OPTIONS", 'w', true),
            ("SIMBRIDGE: AUTO ON", 'w', true), ("", 'w', false),
            ("WAIT FOR THE MESSAGE", 'b', true), ("MCDU CONNECTED", 'b', true),
        };
        bool _gotBlank;   // l'última pantalla de l'avió era buida

        /// <summary>Missatge centrat a les dues MCDU (només mentre no arriba cap pantalla de l'avió).</summary>
        void ShowMessage((string text, char col, bool small)[] lines)
        {
            if (Connected) return;
            var s = new McduScreen();
            for (int row = 0; row < lines.Length && row < McduScreen.Rows; row++)
            {
                var (text, col, small) = lines[row];
                int start = Math.Max(0, (McduScreen.Cols - text.Length) / 2);
                for (int k = 0; k < text.Length && start + k < McduScreen.Cols; k++)
                {
                    if (text[k] == ' ') continue;
                    s.Ch[row, start + k] = text[k]; s.Col[row, start + k] = col; s.Small[row, start + k] = small;
                }
            }
            for (int u = 0; u < 2; u++)
            {
                if (s.Equals(_last[u])) continue;
                _last[u] = s;
                int unit = u + 1;
                Post(() => McduChanged?.Invoke(unit, s));
            }
        }

        async Task SessionAsync(CancellationToken ct, Action onOpen)
        {
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(WsUrl), ct);
            onOpen();
            _ws = ws;
            await SendAsync(ws, "requestUpdate", ct);
            var buf = new byte[64 * 1024];
            var msg = new System.IO.MemoryStream();
            // mentre no arriba cap pantalla (l'avió encara no s'ha connectat al SimBridge), es torna a demanar cada 5 s
            // sense tancar la connexió
            using var askCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!askCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(5000, askCts.Token);
                        if (!Connected && ws.State == WebSocketState.Open)
                        {
                            // el SimBridge hi és, però l'avió no hi està connectat (o ha enviat pantalles buides)
                            ShowMessage(_gotBlank ? NoDataMsg : NoAircraftMsg);
                            await SendAsync(ws, "requestUpdate", askCts.Token);
                        }
                    }
                }
                catch { }
            });
            try { await ReceiveLoop(ws, buf, msg, ct); }
            finally { askCts.Cancel(); }
        }

        async Task ReceiveLoop(ClientWebSocket ws, byte[] buf, System.IO.MemoryStream msg, CancellationToken ct)
        {
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct);
                if (r.MessageType == WebSocketMessageType.Close) break;
                msg.Write(buf, 0, r.Count);
                if (!r.EndOfMessage) continue;
                string text = Encoding.UTF8.GetString(msg.GetBuffer(), 0, (int)msg.Length);
                msg.SetLength(0);
                if (text.StartsWith("update:")) Publish(text.Substring(7));
                else if (text == "mcduConnected") await SendAsync(ws, "requestUpdate", ct);
            }
        }

        async Task SendAsync(ClientWebSocket ws, string text, CancellationToken ct)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            await _sendLock.WaitAsync(ct);
            try { await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct); }
            finally { _sendLock.Release(); }
        }

        void Publish(string json)
        {
            McduScreen?[] screens = new McduScreen?[2];
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("left", out var l)) screens[0] = Parse(l);
                if (root.TryGetProperty("right", out var rr)) screens[1] = Parse(rr);
            }
            catch { return; }
            // pantalles buides als dos costats: l'avió s'ha desconnectat del SimBridge (en marxar n'envia una de buida)
            // o les MCDU no tenen corrent. No compta com a connectat: surt el missatge i es torna a demanar cada 5 s.
            bool blank = true;
            foreach (var sc in screens)
                if (sc != null)
                    for (int r = 0; r < McduScreen.Rows && blank; r++) if (sc.RowText(r).Trim().Length > 0) blank = false;
            if (blank)
            {
                Connected = false; _gotBlank = true;
                ShowMessage(NoDataMsg);
                return;
            }
            Connected = true; _gotBlank = false;
            for (int u = 0; u < 2; u++)
            {
                var s = screens[u];
                if (s == null || s.Equals(_last[u])) continue;
                _last[u] = s;
                int unit = u + 1;
                Post(() => McduChanged?.Invoke(unit, s));
            }
        }

        // ── Text de l'FBW → graella 14 × 24 ──
        struct Cell { public char Ch, Col; public bool Small; }

        /// <summary>
        /// Cadena amb marques ("{cyan}TEXT{small}x{end}{end}") → caràcters amb color i mida. Les marques s'apilen i {end}
        /// tanca l'última. "{" o "}" que no són una marca són les fletxes ← →; "_" és la casella àmbar.
        /// </summary>
        static List<Cell> Cells(string? t, bool small)
        {
            var res = new List<Cell>();
            if (string.IsNullOrEmpty(t)) return res;
            char color = 'w';
            var stack = new Stack<(char, bool)>();
            for (int i = 0; i < t.Length; i++)
            {
                char ch = t[i];
                if (ch == '{')
                {
                    int e = t.IndexOf('}', i + 1);
                    string tag = e > i ? t.Substring(i + 1, e - i - 1) : "";
                    if (tag.Length > 0 && IsTag(tag))
                    {
                        i = e;
                        if (tag == "sp") { res.Add(new Cell { Ch = ' ', Col = color, Small = small }); continue; }
                        if (tag == "end") { if (stack.Count > 0) (color, small) = stack.Pop(); continue; }
                        stack.Push((color, small));
                        if (tag == "small") small = true;
                        else if (tag == "big") small = false;
                        else color = ColorOf(tag);
                        continue;
                    }
                    ch = '←';
                }
                else if (ch == '}') ch = '→';
                else if (ch == ' ') ch = ' ';
                else if (ch == '_') ch = '#';     // casella per omplir (el tipus de lletra de l'FBW dibuixa "_" així)
                else if (ch == '|') ch = '¦';     // separador ("____|____"): no és el triangle del ToLiss
                res.Add(new Cell { Ch = ch, Col = color, Small = small });
            }
            return res;
        }

        static bool IsTag(string tag)
        {
            foreach (char c in tag) if (c < 'a' || c > 'z') return false;
            return tag is "sp" or "end" or "small" or "big" or "white" or "cyan" or "green" or "amber" or "red"
                or "magenta" or "yellow" or "inop";
        }

        static char ColorOf(string tag) => tag switch
        {
            "cyan" => 'b', "green" => 'g', "amber" => 'a', "red" => 'r', "magenta" => 'm', "yellow" => 'y',
            _ => 'w',   // white, inop (gris)
        };

        static void Put(McduScreen s, int row, int start, List<Cell> cells)
        {
            for (int k = 0; k < cells.Count; k++)
            {
                int c = start + k;
                if (c < 0 || c >= McduScreen.Cols || cells[k].Ch == ' ') continue;
                s.Ch[row, c] = cells[k].Ch; s.Col[row, c] = cells[k].Col; s.Small[row, c] = cells[k].Small;
            }
        }

        /// <summary>Una fila amb les tres parts de l'FBW: esquerra (columna 0), dreta (acaba a la 23) i centre (centrada).</summary>
        static void Row(McduScreen s, int row, JsonElement parts, bool small)
        {
            if (parts.ValueKind != JsonValueKind.Array) return;
            string? Part(int i) => parts.GetArrayLength() > i && parts[i].ValueKind == JsonValueKind.String ? parts[i].GetString() : null;
            var center = Cells(Part(2), small);
            Put(s, row, (McduScreen.Cols - center.Count) / 2, center);
            Put(s, row, 0, Cells(Part(0), small));
            var right = Cells(Part(1), small);
            Put(s, row, McduScreen.Cols - right.Count, right);
        }

        internal static McduScreen Parse(JsonElement side)
        {
            var s = new McduScreen();
            string? Str(string name) => side.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            // fletxes [↑, ↓, ←, →]
            bool up = false, down = false, left = false, right = false;
            if (side.TryGetProperty("arrows", out var ar) && ar.ValueKind == JsonValueKind.Array && ar.GetArrayLength() >= 4)
            {
                up = ar[0].ValueKind == JsonValueKind.True; down = ar[1].ValueKind == JsonValueKind.True;
                left = ar[2].ValueKind == JsonValueKind.True; right = ar[3].ValueKind == JsonValueKind.True;
            }
            // títol: centrat (gran); titleLeft a l'esquerra; número de pàgina i fletxes ←→ a la dreta
            var title = Cells(Str("title"), false);
            Put(s, 0, (McduScreen.Cols - title.Count) / 2, title);
            Put(s, 0, 0, Cells(Str("titleLeft"), false));
            int rightEnd = McduScreen.Cols;
            if (left || right)
            {
                rightEnd -= 2;
                Put(s, 0, McduScreen.Cols - 2, new List<Cell>
                {
                    new() { Ch = left ? '←' : ' ', Col = 'w' }, new() { Ch = right ? '→' : ' ', Col = 'w' },
                });
            }
            var page = Cells(Str("page"), true);
            Put(s, 0, rightEnd - page.Count, page);
            // 12 files: etiqueta (petita), línia (gran)…
            if (side.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array)
                for (int i = 0; i < 12 && i < lines.GetArrayLength(); i++) Row(s, i + 1, lines[i], i % 2 == 0);
            Put(s, 13, 0, Cells(Str("scratchpad"), false));
            s.VertSlew = up && down ? 1 : up ? 2 : down ? 3 : 0;
            return s;
        }

        // ── Tecles ──
        /// <summary>Tecla de l'app (part de l'id després de "PED_MCDUn_") → nom de la tecla de l'FBW (Keypad del mcdu.js).</summary>
        static readonly Dictionary<string, string> KeyMap = BuildKeyMap();
        static Dictionary<string, string> BuildKeyMap()
        {
            var m = new Dictionary<string, string>
            {
                ["DIR"] = "DIR", ["PROG"] = "PROG", ["PERF"] = "PERF", ["INIT"] = "INIT", ["DATA"] = "DATA",
                ["FPLN"] = "FPLN", ["RADNAV"] = "RAD", ["FUELPRED"] = "FUEL", ["SECFPLN"] = "SEC",
                ["ATCCOMM"] = "ATC", ["MCDUMENU"] = "MENU", ["AIRPORT"] = "AIRPORT",
                ["PREV"] = "PREVPAGE", ["NEXT"] = "NEXTPAGE", ["UP"] = "UP", ["DOWN"] = "DOWN",
                ["BRT"] = "BRT", ["DIM"] = "DIM",
                ["KEY_DOT"] = "DOT", ["KEY_PLUSMINUS"] = "PLUSMINUS", ["KEY_DIV"] = "DIV", ["KEY_SP"] = "SP",
                ["KEY_OVFY"] = "OVFY", ["KEY_CLR"] = "CLR",
            };
            for (int i = 1; i <= 6; i++) { m["LSK" + i + "L"] = "L" + i; m["LSK" + i + "R"] = "R" + i; }
            for (char c = 'A'; c <= 'Z'; c++) m["KEY_" + c] = c.ToString();
            for (char c = '0'; c <= '9'; c++) m["KEY_" + c] = c.ToString();
            return m;
        }

        /// <summary>Tecla d'una MCDU del pedestal ("PED_MCDU1_KEY_A"…) cap a l'FBW. false si no és una tecla de MCDU.</summary>
        public bool TrySendKey(string id)
        {
            if (!id.StartsWith("PED_MCDU") || id.Length < 10 || id[9] != '_') return false;
            char unit = id[8];
            if (unit != '1' && unit != '2') return false;
            if (!KeyMap.TryGetValue(id[10..], out var key)) return true;   // tecla sense equivalent (p.ex. les buides)
            var ws = _ws;
            var ct = _cts?.Token ?? CancellationToken.None;
            if (ws == null || ws.State != WebSocketState.Open) return true;
            string msg = "event:" + (unit == '1' ? "left" : "right") + ":" + key;
            _ = Task.Run(async () => { try { await SendAsync(ws, msg, ct); } catch { } });
            return true;
        }
    }
}
