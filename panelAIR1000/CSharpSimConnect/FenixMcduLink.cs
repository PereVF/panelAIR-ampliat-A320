using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace panelAIR1000
{
    /// <summary>
    /// Pantalles i tecles de les MCDU del Fenix A320 (MSFS) a través del servidor GraphQL que el Fenix porta integrat
    /// (procés Fenix.GqlGateway, port 8083; el mateix que fa servir l'app MCDU de l'EFB del Fenix). Sense pop-out ni
    /// res instal·lat: un WebSocket (protocol graphql-transport-ws) per a les pantalles i POST HTTP per a les tecles.
    ///  - Pantalla: dataref "aircraft.mcduN.display", un XML amb &lt;title&gt;, 12 &lt;line&gt; i &lt;scratchpad&gt;.
    ///    Dins del text, una lletra minúscula canvia el format dels caràcters següents: s petita, l gran, a àmbar,
    ///    w blanc, c cian, g verd, m magenta, r vermell, y groc.
    ///  - Tecla: escriure 1 a "system.switches.S_CDUN_KEY_&lt;nom&gt;" (mutation WriteIntDataRef).
    /// </summary>
    internal sealed class FenixMcduLink : IDisposable
    {
        const string HttpUrl = "http://localhost:8083/graphql";
        const string WsUrl = "ws://localhost:8083/graphql";
        const string SubQuery = "subscription DataRefsChange($names: [String!]!) { dataRefs(names: $names) { name value } }";
        const string GetQuery = "query GetDataRef($name: String!) { dataRef { dataRef(name: $name) { value } } }";
        const string WriteIntMutation = "mutation WriteIntDataRef($name: String!, $value: Int!) { dataRef { writeInt(name: $name, value: $value) } }";
        static readonly string[] DisplayRefs = { "aircraft.mcdu1.display", "aircraft.mcdu2.display" };

        /// <summary>Pantalla d'una MCDU (1 capità, 2 primer oficial) (al fil de la interfície).</summary>
        public event Action<int, McduScreen>? McduChanged;
        /// <summary>true mentre hi ha connexió amb el servidor del Fenix i ja ha arribat alguna pantalla.</summary>
        public bool Connected { get; private set; }

        readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
        readonly McduScreen?[] _last = new McduScreen?[2];
        SynchronizationContext? _ui;
        CancellationTokenSource? _cts;

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

        public void Dispose() { Stop(); _http.Dispose(); }

        // (després d'Stop ja no es publica res: si no, una pantalla pendent tornaria a sortir un cop netejada)
        void Post(Action a) { if (_ui != null) _ui.Post(_ => { if (_cts != null) a(); }, null); else a(); }

        async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await SessionAsync(ct); }
                catch { /* el Fenix no hi és o s'ha tancat: es torna a provar */ }
                Connected = false;
                if (!ct.IsCancellationRequested) ShowNoConnection();
                try { await Task.Delay(3000, ct); } catch { }
            }
        }

        async Task SessionAsync(CancellationToken ct)
        {
            using var ws = new ClientWebSocket();
            ws.Options.AddSubProtocol("graphql-transport-ws");
            await ws.ConnectAsync(new Uri(WsUrl), ct);
            await SendAsync(ws, new { type = "connection_init", payload = new { } }, ct);
            await SendAsync(ws, new
            {
                id = "1", type = "subscribe",
                payload = new { query = SubQuery, variables = new { names = DisplayRefs } }
            }, ct);
            // valor actual de les pantalles (la subscripció només envia els canvis)
            for (int u = 0; u < 2; u++)
                if (await GetAsync(DisplayRefs[u], ct) is string xml) Publish(u, xml);

            var buf = new byte[64 * 1024];
            var msg = new System.IO.MemoryStream();
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct);
                if (r.MessageType == WebSocketMessageType.Close) break;
                msg.Write(buf, 0, r.Count);
                if (!r.EndOfMessage) continue;
                var data = msg.ToArray();
                msg.SetLength(0);
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;
                string? type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
                if (type == "ping") { await SendAsync(ws, new { type = "pong" }, ct); continue; }
                if (type == "error" || type == "complete") break;
                if (type != "next") continue;
                if (!root.TryGetProperty("payload", out var p) || !p.TryGetProperty("data", out var d)
                    || !d.TryGetProperty("dataRefs", out var dr)) continue;
                string? name = dr.TryGetProperty("name", out var n) ? n.GetString() : null;
                string? value = dr.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                int unit = Array.IndexOf(DisplayRefs, name);
                if (unit >= 0 && value != null) Publish(unit, value);
            }
        }

        /// <summary>Missatge a les dues MCDU mentre no hi ha connexió amb el servidor del Fenix (p.ex. mentre es carrega).</summary>
        void ShowNoConnection()
        {
            (string text, char col, bool small)[] lines =
            {
                ("FENIX MCDU", 'w', false), ("", 'w', false),
                ("NO CONNECTION", 'a', false), ("", 'w', false),
                ("FENIX NOT RUNNING OR", 'w', true), ("STILL LOADING", 'w', true), ("", 'w', false),
                ("RETRYING EVERY 3 S", 'b', true), ("(LOCALHOST PORT 8083)", 'b', true),
            };
            var s = new McduScreen();
            for (int row = 0; row < lines.Length; row++)
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

        static async Task SendAsync(ClientWebSocket ws, object payload, CancellationToken ct)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }

        async Task<string?> GetAsync(string name, CancellationToken ct)
        {
            var body = JsonSerializer.Serialize(new { query = GetQuery, variables = new { name } });
            using var resp = await _http.PostAsync(HttpUrl, new StringContent(body, Encoding.UTF8, "application/json"), ct);
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsByteArrayAsync(ct));
            return doc.RootElement.TryGetProperty("data", out var d) && d.TryGetProperty("dataRef", out var a)
                   && a.TryGetProperty("dataRef", out var b) && b.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() : null;
        }

        void Publish(int unit, string xml)
        {
            McduScreen s;
            try { s = Parse(xml); } catch { return; }
            Connected = true;
            if (s.Equals(_last[unit])) return;
            _last[unit] = s;
            Post(() => McduChanged?.Invoke(unit + 1, s));
        }

        /// <summary>XML del Fenix → graella 14 × 24 (fila 0 títol, 1..12 les línies, 13 el scratchpad).</summary>
        internal static McduScreen Parse(string xml)
        {
            var s = new McduScreen();
            var root = XElement.Parse(xml);
            void Row(int row, string? text)
            {
                if (text == null) return;
                char color = 'w'; bool small = false; int col = 0;
                var arrows = new List<int>();
                foreach (char ch in text)
                {
                    if (ch >= 'a' && ch <= 'z')
                    {
                        switch (ch)
                        {
                            case 's': small = true; break;
                            case 'l': small = false; break;
                            case 'a': case 'w': case 'g': case 'm': case 'r': case 'y': color = ch; break;
                            case 'c': color = 'b'; break;   // cian (a la graella, "b")
                        }
                        continue;
                    }
                    if (col >= McduScreen.Cols) break;
                    // símbols del tipus de lletra del Fenix (comprovats): "£" fletxa ← (p.ex. "←COPY ACTIVE" a SEC INDEX),
                    // "¥" fletxa ↓ (p.ex. "↓VATSIM" a AOC ATIS REQUEST), "¢" fletxa → i "¤" fletxa ↑ (les fletxes de
                    // canvi de pàgina "←→" del títol i "↑↓" de baix a la dreta, a F-PLN)
                    // "&" és el triangle Δ (p.ex. "PG084Δ" a F-PLN; manté el color que envia el Fenix)
                    char c2 = ch switch { '£' => '←', '¥' => '↓', '¢' => '→', '¤' => '↑', '&' => 'Δ', _ => ch };
                    if (c2 != ' ') { s.Ch[row, col] = c2; s.Col[row, col] = color; s.Small[row, col] = small; }
                    if (c2 != ch && c2 != 'Δ') arrows.Add(col);
                    col++;
                }
                // les fletxes prenen el color del text que les segueix (com a la pop-out del Fenix: "↓VATSIM" tot cian)
                foreach (int a in arrows)
                    if (a + 1 < McduScreen.Cols && s.Ch[row, a + 1] != ' ') s.Col[row, a] = s.Col[row, a + 1];
            }
            Row(0, root.Element("title")?.Value);
            int i = 1;
            foreach (var line in root.Elements("line")) { if (i > 12) break; Row(i++, line.Value); }
            Row(13, root.Element("scratchpad")?.Value);
            return s;
        }

        // ── Tecles ──
        /// <summary>Tecla de l'app (part de l'id després de "PED_MCDUn_") → nom de la tecla del Fenix.</summary>
        static readonly Dictionary<string, string> KeyMap = BuildKeyMap();
        static Dictionary<string, string> BuildKeyMap()
        {
            var m = new Dictionary<string, string>
            {
                ["DIR"] = "DIR", ["PROG"] = "PROG", ["PERF"] = "PERF", ["INIT"] = "INIT", ["DATA"] = "DATA",
                ["FPLN"] = "FPLN", ["RADNAV"] = "RAD_NAV", ["FUELPRED"] = "FUEL_PRED", ["SECFPLN"] = "SEC_FPLN",
                ["ATCCOMM"] = "ATC_COM", ["MCDUMENU"] = "MENU", ["AIRPORT"] = "AIRPORT",
                ["PREV"] = "ARROW_LEFT", ["NEXT"] = "ARROW_RIGHT", ["UP"] = "ARROW_UP", ["DOWN"] = "ARROW_DOWN",
                ["BRT"] = "BRT", ["DIM"] = "DIM",
                ["KEY_DOT"] = "DOT", ["KEY_PLUSMINUS"] = "MINUS", ["KEY_DIV"] = "SLASH", ["KEY_SP"] = "SPACE",
                ["KEY_OVFY"] = "OVFLY", ["KEY_CLR"] = "CLEAR",
            };
            for (int i = 1; i <= 6; i++) { m["LSK" + i + "L"] = "LSK" + i + "L"; m["LSK" + i + "R"] = "LSK" + i + "R"; }
            for (char c = 'A'; c <= 'Z'; c++) m["KEY_" + c] = c.ToString();
            for (char c = '0'; c <= '9'; c++) m["KEY_" + c] = c.ToString();
            return m;
        }

        /// <summary>Tecla d'una MCDU del pedestal ("PED_MCDU1_KEY_A"…) cap al Fenix. false si no és una tecla de MCDU.</summary>
        public bool TrySendKey(string id)
        {
            if (!id.StartsWith("PED_MCDU") || id.Length < 10 || id[9] != '_') return false;
            char unit = id[8];
            if (unit != '1' && unit != '2') return false;
            if (!KeyMap.TryGetValue(id[10..], out var key)) return true;   // tecla de MCDU sense equivalent (p.ex. les buides)
            var body = JsonSerializer.Serialize(new { query = WriteIntMutation, variables = new { name = "system.switches.S_CDU" + unit + "_KEY_" + key, value = 1 } });
            _ = Task.Run(async () =>
            {
                try { using var r = await _http.PostAsync(HttpUrl, new StringContent(body, Encoding.UTF8, "application/json")); } catch { }
            });
            return true;
        }
    }
}
