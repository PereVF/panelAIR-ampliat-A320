using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimConnect3
{
    /// <summary>
    /// Enllaç amb X-Plane 12 (ToLiss A320) a través de l'API web integrada d'X-Plane (v3, port 8086, sense plugins
    /// ni DLL externes: només HttpClient, ClientWebSocket i System.Text.Json del .NET).
    ///
    /// Funcionament:
    ///  - Cada 3 s mira si X-Plane respon i si hi ha un ToLiss carregat (existeix el dataref AirbusFBW/BaroStdCapt).
    ///  - Si hi és: resol els ID dels datarefs i commands per nom, obre el WebSocket i se subscriu als datarefs.
    ///    X-Plane envia els valors només quan canvien (uns 10 cops per segon com a màxim).
    ///  - Cada 5 s torna a comprovar que el ToLiss continua carregat; si no, tanca i torna a esperar.
    ///  - Les ordres del panell (MIP_BARO INC, PUSH...) es tradueixen a commands o escriptures de datarefs.
    /// Tots els avisos cap a l'app es fan al fil de la interfície (SynchronizationContext capturat a Start).
    ///
    /// Per ara només el QNH del capità; la resta de controls s'afegeixen omplint les taules de sota.
    /// </summary>
    internal sealed class XPlaneLink : IDisposable
    {
        const string RestBase = "http://localhost:8086/api/v3/";
        const string WsUrl = "ws://localhost:8086/api/v3";
        const string ToLissProbe = "AirbusFBW/BaroStdCapt";   // si existeix, hi ha un ToLiss carregat

        // ── Taules de correspondència (element del panell + acció → X-Plane) ──
        static readonly Dictionary<(string id, string cmd), string> CommandMap = new()
        {
            [("MIP_BARO", "INC")] = "sim/instruments/barometer_up",
            [("MIP_BARO", "DEC")] = "sim/instruments/barometer_down",
            [("MIP_BARO", "PUSH")] = "toliss_airbus/capt_baro_push",
            [("MIP_BARO", "PULL")] = "toliss_airbus/capt_baro_pull",
            // rotatius del FCU: girar amb els commands estàndard, prémer / estirar amb els del ToLiss
            [("MIP_FCU_SPD", "INC")] = "sim/autopilot/airspeed_up",
            [("MIP_FCU_SPD", "DEC")] = "sim/autopilot/airspeed_down",
            [("MIP_FCU_SPD", "PUSH")] = "AirbusFBW/PushSPDSel",
            [("MIP_FCU_SPD", "PULL")] = "AirbusFBW/PullSPDSel",
            [("MIP_FCU_HDG", "INC")] = "sim/autopilot/heading_up",
            [("MIP_FCU_HDG", "DEC")] = "sim/autopilot/heading_down",
            [("MIP_FCU_HDG", "PUSH")] = "AirbusFBW/PushHDGSel",
            [("MIP_FCU_HDG", "PULL")] = "AirbusFBW/PullHDGSel",
            [("MIP_FCU_ALT", "INC")] = "sim/autopilot/altitude_up",
            [("MIP_FCU_ALT", "DEC")] = "sim/autopilot/altitude_down",
            [("MIP_FCU_ALT", "PUSH")] = "AirbusFBW/PushAltitude",
            [("MIP_FCU_ALT", "PULL")] = "AirbusFBW/PullAltitude",
            [("MIP_FCU_VS", "INC")] = "sim/autopilot/vertical_speed_up",
            [("MIP_FCU_VS", "DEC")] = "sim/autopilot/vertical_speed_down",
            [("MIP_FCU_VS", "PUSH")] = "AirbusFBW/PushVSSel",
            [("MIP_FCU_VS", "PULL")] = "AirbusFBW/PullVSSel",
        };
        static readonly Dictionary<(string id, string cmd), (string dataref, double value)> WriteMap = new()
        {
            [("MIP_BARO", "INHG")] = ("AirbusFBW/BaroUnitCapt", 0),   // 0 = inHg
            [("MIP_BARO", "HPA")] = ("AirbusFBW/BaroUnitCapt", 1),    // 1 = hPa
        };
        // datarefs que es llegeixen (estat que es mostra al panell)
        static readonly string[] ReadDatarefs =
        {
            "AirbusFBW/BaroStdCapt",                                        // 1 = STD
            "AirbusFBW/BaroUnitCapt",                                       // 0 = inHg, 1 = hPa
            "sim/cockpit2/gauges/actuators/barometer_setting_in_hg_pilot",  // valor del QNH (inHg)
            // pantalla del FCU
            "sim/cockpit2/autopilot/airspeed_dial_kts_mach", "sim/cockpit/autopilot/airspeed_is_mach",
            "AirbusFBW/SPDdashed", "AirbusFBW/SPDmanaged",
            "sim/cockpit/autopilot/heading_mag", "AirbusFBW/HDGdashed", "AirbusFBW/HDGmanaged", "AirbusFBW/HDGTRKmode",
            "sim/cockpit/autopilot/altitude", "AirbusFBW/ALTmanaged",
            "sim/cockpit/autopilot/vertical_velocity", "sim/cockpit2/autopilot/fpa", "AirbusFBW/VSdashed",
        };

        /// <summary>true = hi ha un ToLiss carregat a X-Plane i l'enllaç està connectat.</summary>
        public bool A320Loaded { get; private set; }
        /// <summary>Canvi de ToLiss carregat / no carregat (al fil de la interfície).</summary>
        public event Action<bool>? A320Changed;
        /// <summary>QNH del capità: amb corrent, mode (0 STD, 1 QNH), valor (hPa o inHg), unitat inHg (al fil de la interfície).</summary>
        public event Action<bool, int, double, bool>? BaroChanged;
        /// <summary>Pantalla del FCU (al fil de la interfície).</summary>
        public event Action<panelAIR1000.FcuState>? FcuChanged;

        readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };   // X-Plane respon al ritme dels seus frames (pot trigar)
        readonly SemaphoreSlim _sendLock = new(1, 1);
        readonly Dictionary<string, long> _cmdIds = new();
        readonly Dictionary<string, long> _drIds = new();
        readonly Dictionary<long, string> _drNames = new();
        readonly Dictionary<string, double> _values = new();
        SynchronizationContext? _ui;
        CancellationTokenSource? _cts;
        ClientWebSocket? _ws;
        int _reqId;
        (bool, int, double, bool)? _lastBaro;
        panelAIR1000.FcuState? _lastFcu;

        public void Start()
        {
            if (_cts != null) return;
            _ui = SynchronizationContext.Current;
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            Task.Run(() => RunAsync(ct));
        }

        public void Dispose()
        {
            try { _cts?.Cancel(); } catch { }
            try { _ws?.Abort(); } catch { }
            _http.Dispose();
        }

        void Post(Action a) { if (_ui != null) _ui.Post(_ => a(), null); else a(); }

        async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (await LookupId("datarefs", ToLissProbe, ct) is long)
                        await SessionAsync(ct);
                }
                catch { /* X-Plane no respon o s'ha tancat: es torna a provar */ }
                SetLoaded(false);
                try { await Task.Delay(3000, ct); } catch { }
            }
        }

        /// <summary>Una sessió amb el ToLiss carregat: resol IDs, se subscriu i escolta fins que es tanca o canvia l'avió.</summary>
        async Task SessionAsync(CancellationToken ct)
        {
            _cmdIds.Clear(); _drIds.Clear(); _drNames.Clear(); _values.Clear(); _lastBaro = null; _lastFcu = null;
            foreach (var name in ReadDatarefs)
                if (await LookupId("datarefs", name, ct) is long id) { _drIds[name] = id; _drNames[id] = name; }
            foreach (var w in WriteMap.Values)
                if (!_drIds.ContainsKey(w.dataref) && await LookupId("datarefs", w.dataref, ct) is long id) _drIds[w.dataref] = id;
            foreach (var c in CommandMap.Values)
                if (!_cmdIds.ContainsKey(c) && await LookupId("commands", c, ct) is long id) _cmdIds[c] = id;

            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(WsUrl), ct);
            _ws = ws;
            try
            {
                var subs = new List<object>();
                foreach (var name in ReadDatarefs) if (_drIds.TryGetValue(name, out var id)) subs.Add(new { id });
                if (subs.Count > 0) await SendAsync(new { req_id = NextReq(), type = "dataref_subscribe_values", @params = new { datarefs = subs } });
                SetLoaded(true);

                using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var watch = WatchAircraftAsync(sessionCts.Token);
                var buf = new byte[64 * 1024];
                var msg = new System.IO.MemoryStream();
                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested && !watch.IsCompleted)
                {
                    var recv = ws.ReceiveAsync(new ArraySegment<byte>(buf), sessionCts.Token);
                    if (await Task.WhenAny(recv, watch) == watch) break;   // el ToLiss ja no hi és
                    var r = await recv;
                    if (r.MessageType == WebSocketMessageType.Close) break;
                    msg.Write(buf, 0, r.Count);
                    if (!r.EndOfMessage) continue;
                    HandleMessage(msg.ToArray());
                    msg.SetLength(0);
                }
                sessionCts.Cancel();
            }
            finally
            {
                _ws = null;
                try { if (ws.State == WebSocketState.Open) await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); } catch { }
            }
        }

        /// <summary>Acaba quan el ToLiss deixa d'estar carregat (comprovació cada 5 s).</summary>
        async Task WatchAircraftAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(5000, ct); } catch { return; }
                try { if (await LookupId("datarefs", ToLissProbe, ct) is not long) return; }
                catch (OperationCanceledException) { return; }
                catch { return; }
            }
        }

        void HandleMessage(byte[] data)
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var t) || t.GetString() != "dataref_update_values") return;
            if (!root.TryGetProperty("data", out var d) || d.ValueKind != JsonValueKind.Object) return;
            foreach (var p in d.EnumerateObject())
            {
                if (!long.TryParse(p.Name.Trim(), out var id) || !_drNames.TryGetValue(id, out var name)) continue;
                var v = p.Value;
                if (v.ValueKind == JsonValueKind.Array) { if (v.GetArrayLength() == 0) continue; v = v[0]; }
                if (v.ValueKind == JsonValueKind.Number) lock (_values) _values[name] = v.GetDouble();
            }
            lock (_values) { PublishBaro(); PublishFcu(); }
        }

        void PublishBaro()
        {
            if (!_values.TryGetValue("sim/cockpit2/gauges/actuators/barometer_setting_in_hg_pilot", out var inHg)) return;
            bool std = _values.TryGetValue("AirbusFBW/BaroStdCapt", out var s) && s > 0.5;
            bool hpa = !_values.TryGetValue("AirbusFBW/BaroUnitCapt", out var u) || u > 0.5;
            double value = hpa ? inHg * 33.8639 : inHg;
            // (encara no tenim el dataref de corrent de l'FCU del ToLiss: es considera que en té mentre hi ha connexió)
            var baro = (true, std ? 0 : 1, value, !hpa);
            if (_lastBaro == baro) return;
            _lastBaro = baro;
            Post(() => BaroChanged?.Invoke(baro.Item1, baro.Item2, baro.Item3, baro.Item4));
        }

        double V(string name) => _values.TryGetValue(name, out var v) ? v : 0;
        bool B(string name) => V(name) > 0.5;

        void PublishFcu()
        {
            if (!_values.ContainsKey("sim/cockpit/autopilot/altitude")) return;
            // (sense dataref de corrent de l'FCU: encesa mentre hi ha connexió)
            var s = new panelAIR1000.FcuState(true,
                V("sim/cockpit2/autopilot/airspeed_dial_kts_mach"), B("sim/cockpit/autopilot/airspeed_is_mach"),
                B("AirbusFBW/SPDdashed"), B("AirbusFBW/SPDmanaged"),
                V("sim/cockpit/autopilot/heading_mag"), B("AirbusFBW/HDGdashed"), B("AirbusFBW/HDGmanaged"), B("AirbusFBW/HDGTRKmode"),
                V("sim/cockpit/autopilot/altitude"), B("AirbusFBW/ALTmanaged"),
                V("sim/cockpit/autopilot/vertical_velocity"), V("sim/cockpit2/autopilot/fpa"), B("AirbusFBW/VSdashed"));
            if (s == _lastFcu) return;
            _lastFcu = s;
            Post(() => FcuChanged?.Invoke(s));
        }

        void SetLoaded(bool on)
        {
            if (A320Loaded == on) return;
            A320Loaded = on;
            Post(() => A320Changed?.Invoke(on));
        }

        /// <summary>Ordre del panell cap a X-Plane (es crida des del fil de la interfície; s'envia en segon pla).</summary>
        public void SendCommand(string id, string cmd)
        {
            if (!A320Loaded || _ws == null) return;
            if (id == "MIP_BARO" && (cmd == "INC" || cmd == "DEC") && TryBaroHpaStep(cmd == "INC" ? 1 : -1)) return;
            object? req = null;
            if (CommandMap.TryGetValue((id, cmd), out var c) && _cmdIds.TryGetValue(c, out var cid))
                req = new { req_id = NextReq(), type = "command_set_is_active", @params = new { commands = new[] { new { id = cid, is_active = true, duration = 0.0 } } } };
            else if (WriteMap.TryGetValue((id, cmd), out var w) && _drIds.TryGetValue(w.dataref, out var did))
                req = new { req_id = NextReq(), type = "dataref_set_values", @params = new { datarefs = new[] { new { id = did, value = w.value } } } };
            if (req != null) _ = SendAsync(req);
        }

        const string BaroDr = "sim/cockpit2/gauges/actuators/barometer_setting_in_hg_pilot";
        const double HpaPerInHg = 33.8639;
        double? _pendingHpa; long _pendingTicks;

        /// <summary>
        /// En hPa, el command estàndard d'X-Plane puja 0,01 inHg (0,34 hPa) per pas; el pom de l'A320 puja 1 hPa.
        /// Per això en hPa s'escriu directament el valor següent en hPa sencers. (En inHg es fa servir el command.)
        /// Si es gira de pressa, es parteix de l'últim valor enviat (X-Plane envia els canvis a uns 10 Hz).
        /// </summary>
        bool TryBaroHpaStep(int dir)
        {
            double inHg;
            lock (_values)
            {
                if (!_values.TryGetValue("AirbusFBW/BaroUnitCapt", out var u) || u < 0.5) return false;
                if (!_values.TryGetValue(BaroDr, out inHg)) return false;
            }
            if (!_drIds.TryGetValue(BaroDr, out var did)) return false;
            long now = Environment.TickCount64;
            double baseHpa = _pendingHpa.HasValue && now - _pendingTicks < 1000 ? _pendingHpa.Value : Math.Round(inHg * HpaPerInHg);
            double target = Math.Clamp(baseHpa + dir, 745, 1100);
            _pendingHpa = target; _pendingTicks = now;
            _ = SendAsync(new { req_id = NextReq(), type = "dataref_set_values", @params = new { datarefs = new[] { new { id = did, value = target / HpaPerInHg } } } });
            return true;
        }

        int NextReq() => Interlocked.Increment(ref _reqId) & 0x7FFFFFFF;

        async Task SendAsync(object payload)
        {
            var ws = _ws;
            if (ws == null || ws.State != WebSocketState.Open) return;
            var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
            await _sendLock.WaitAsync();
            try { await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
            catch { }
            finally { _sendLock.Release(); }
        }

        /// <summary>ID d'un dataref o command pel nom (REST); null si no existeix.</summary>
        async Task<long?> LookupId(string kind, string name, CancellationToken ct)
        {
            using var resp = await _http.GetAsync($"{RestBase}{kind}?filter%5Bname%5D={Uri.EscapeDataString(name)}&fields=id,name", ct);
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsByteArrayAsync(ct));
            if (doc.RootElement.TryGetProperty("data", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var e in arr.EnumerateArray())
                    if (e.TryGetProperty("id", out var idEl) && idEl.TryGetInt64(out var id)) return id;
            return null;
        }
    }
}
