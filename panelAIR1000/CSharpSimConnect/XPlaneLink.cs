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
            // botons rodons del FCU / glareshield
            [("MIP_FCU_SPD_MACH", "PUSH")] = "toliss_airbus/ias_mach_button_push",
            [("MIP_FCU_HDGTRK_VSFPA", "PUSH")] = "toliss_airbus/hdgtrk_button_push",
            [("MIP_FCU_METRIC_ALT", "PUSH")] = "toliss_airbus/metric_alt_button_push",
            [("MIP_CHRONO", "PUSH")] = "AirbusFBW/CaptChronoButton",
            // obrir / tancar la finestra del PFD del capità (si ja s'ha fet pop-out un cop en aquesta sessió, surt com a finestra de Windows)
            [("XP_POPUP_PFD1", "PUSH")] = "AirbusFBW/PopUpPFD1",
            [("XP_POPUP_ND1", "PUSH")] = "AirbusFBW/PopUpND1",
            [("XP_POPUP_EWD", "PUSH")] = "AirbusFBW/PopUpEWD",
            [("XP_POPUP_SD", "PUSH")] = "AirbusFBW/PopUpSD",
            // tecles del FCU
            [("MIP_FCU_AP1", "PUSH")] = "toliss_airbus/ap1_push",
            [("MIP_FCU_AP2", "PUSH")] = "toliss_airbus/ap2_push",
            [("MIP_FCU_ATHR", "PUSH")] = "AirbusFBW/ATHRbutton",
            [("MIP_FCU_LOC", "PUSH")] = "AirbusFBW/LOCbutton",
            [("MIP_FCU_APPR", "PUSH")] = "AirbusFBW/APPRbutton",
            [("MIP_FCU_EXPED", "PUSH")] = "AirbusFBW/EXPEDbutton",
            // tecles de l'EFIS del capità
            [("MIP_EFIS_FD", "PUSH")] = "toliss_airbus/fd1_push",
            [("MIP_EFIS_LS", "PUSH")] = "toliss_airbus/dispcommands/CaptLSButtonPush",
            [("MIP_EFIS_CSTR", "PUSH")] = "toliss_airbus/dispcommands/CaptCstrPushButton",
            [("MIP_EFIS_WPT", "PUSH")] = "toliss_airbus/dispcommands/CaptWptPushButton",
            [("MIP_EFIS_VORD", "PUSH")] = "toliss_airbus/dispcommands/CaptVorDPushButton",
            [("MIP_EFIS_NDB", "PUSH")] = "toliss_airbus/dispcommands/CaptNdbPushButton",
            [("MIP_EFIS_ARPT", "PUSH")] = "toliss_airbus/dispcommands/CaptArptPushButton",
            // glareshield, TERR ON ND, frens
            [("MIP_MASTER_WARN", "PUSH")] = "sim/annunciator/clear_master_warning",
            [("MIP_MASTER_CAUT", "PUSH")] = "sim/annunciator/clear_master_caution",
            [("MIP_TERR_ON_ND_1", "PUSH")] = "toliss_airbus/dispcommands/TerrOnND1Toggle",
            [("MIP_TERR_ON_ND_2", "PUSH")] = "toliss_airbus/dispcommands/TerrOnND2Toggle",
            [("MIP_AUTOBRK_LO", "PUSH")] = "AirbusFBW/AbrkLo",
            [("MIP_AUTOBRK_MED", "PUSH")] = "AirbusFBW/AbrkMed",
            [("MIP_AUTOBRK_MAX", "PUSH")] = "AirbusFBW/AbrkMax",
            [("MIP_BRK_FAN", "PUSH")] = "toliss_airbus/gear/brake_fan",
            // palanca del tren (ordres estàndard d'X-Plane)
            [("MIP_GEAR_LEVER", "UP")] = "sim/flight_controls/landing_gear_up",
            [("MIP_GEAR_LEVER", "DOWN")] = "sim/flight_controls/landing_gear_down",
            // rellotge del panell principal
            [("MIP_CLK_CHR", "PUSH")] = "toliss_airbus/chrono/ChronoStartStopPush",
            [("MIP_CLK_RST", "PUSH")] = "toliss_airbus/chrono/ChronoResetPush",
            [("MIP_CLK_DATE", "PUSH")] = "toliss_airbus/chrono/datePush",
        };
        static readonly Dictionary<(string id, string cmd), (string dataref, double value)> WriteMap = new()
        {
            [("MIP_BARO", "INHG")] = ("AirbusFBW/BaroUnitCapt", 0),   // 0 = inHg
            [("MIP_BARO", "HPA")] = ("AirbusFBW/BaroUnitCapt", 1),    // 1 = hPa
            [("MIP_FCU_ALT", "STEP100")] = ("AirbusFBW/ALT100_1000", 0),    // 0 = 100 (comprovat al ToLiss)
            [("MIP_FCU_ALT", "STEP1000")] = ("AirbusFBW/ALT100_1000", 1),   // 1 = 1000
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
            "AirbusFBW/ALT100_1000",                                        // selector ALT 100 (0) / 1000 (1)
            GearHandleDr,                                                   // palanca del tren (1 = avall)
        };
        const string GearHandleDr = "sim/cockpit2/controls/gear_handle_down";
        /// <summary>Palanca del tren: true = avall (al fil de la interfície).</summary>
        public event Action<bool>? GearChanged;
        bool? _lastGear;

        /// <summary>Llums de tecles i polsadors: element del panell → dataref (encès si el valor arriba al llindar).</summary>
        static readonly (string id, string dataref, double min)[] LightMap =
        {
            ("MIP_FCU_AP1", "AirbusFBW/AP1Engage", 0.5),
            ("MIP_FCU_AP2", "AirbusFBW/AP2Engage", 0.5),
            ("MIP_FCU_ATHR", "AirbusFBW/ATHRmode", 0.5),
            ("MIP_FCU_LOC", "AirbusFBW/LOCilluminated", 0.5),
            ("MIP_FCU_APPR", "AirbusFBW/APPRilluminated", 0.5),
            ("MIP_FCU_EXPED", "AirbusFBW/APVerticalMode", 112),   // EXPED encès quan el mode vertical és 112 o més
            ("MIP_EFIS_FD", "AirbusFBW/FD1Engage", 0.5),
            ("MIP_EFIS_LS", "AirbusFBW/ILSonCapt", 0.5),
            ("MIP_EFIS_CSTR", "AirbusFBW/NDShowCSTRCapt", 0.5),
            ("MIP_EFIS_WPT", "AirbusFBW/NDShowWPTCapt", 0.5),
            ("MIP_EFIS_VORD", "AirbusFBW/NDShowVORDCapt", 0.5),
            ("MIP_EFIS_NDB", "AirbusFBW/NDShowNDBCapt", 0.5),
            ("MIP_EFIS_ARPT", "AirbusFBW/NDShowARPTCapt", 0.5),
            ("MIP_MASTER_WARN", "AirbusFBW/MasterWarn", 0.5),
            ("MIP_MASTER_CAUT", "AirbusFBW/MasterCaut", 0.5),
            ("MIP_TERR_ON_ND_1", "AirbusFBW/TerrainSelectedND1", 0.5),
            ("MIP_TERR_ON_ND_2", "AirbusFBW/TerrainSelectedND2", 0.5),
            // (autofre i BRK FAN: a LegendMap; els *ButtonAnim només es mouen mentre es prem el botó)
        };
        /// <summary>
        /// Selectors amb posicions numerades: element del panell → dataref. toSim: valor del dataref per a cada posició
        /// de l'app (null = mateixa numeració a tots dos costats).
        /// </summary>
        static readonly (string id, string dataref, int[]? toSim)[] PosMap =
        {
            ("MIP_ND_MODE", "AirbusFBW/NDmodeCapt", null),                         // 0 LS, 1 VOR, 2 NAV, 3 ARC, 4 PLAN
            ("MIP_ND_RANGE", "AirbusFBW/NDrangeCapt", null),                       // 0 = 10 NM … 5 = 320 NM
            ("MIP_EFIS_ADFVOR1", "sim/cockpit2/EFIS/EFIS_1_selection_pilot", null), // 0 ADF, 1 OFF, 2 VOR
            ("MIP_EFIS_ADFVOR2", "sim/cockpit2/EFIS/EFIS_2_selection_pilot", null),
            // rellotge (mesurat al ToLiss): GPS 0 GPS, 1 INT, 2 SET; RUN 0 RUN, 1 STP, 2 RST (molla, torna a STP)
            ("MIP_CLK_GPS", "ckpt/clock/gpsKnob/anim", null),
            ("MIP_CLK_RUN", "AirbusFBW/ClockETSwitch", null),
            // A/SKID & N/W STRG: a l'app 0 = ON (amunt), 1 = OFF; al ToLiss 1 = ON
            ("MIP_ASKID_NWSTRG", "AirbusFBW/NWSnAntiSkid", new[] { 1, 0 }),
        };
        /// <summary>Datarefs que s'escriuen a més del del selector (l'animació del pom RUN no segueix l'interruptor).</summary>
        static readonly Dictionary<string, string> PosMirror = new() { ["MIP_CLK_RUN"] = "ckpt/clock/runKnob/anim" };

        /// <summary>
        /// Polsadors de dues llegendes i indicadors del tren: (element, dataref, llegenda de dalt?, quan s'encén).
        /// Comprovat al ToLiss: tren baixat i blocat = 2; AutoBrkLo/Med/Max = 1 en armar; BrakeFan = 1 encès.
        /// (No comprovat: UNLK = bit 1 del tren, DECEL = 2 a l'autofre.)
        /// </summary>
        static readonly (string id, string dataref, bool upper, Func<double, bool> on)[] LegendMap =
        {
            ("MIP_GEAR_IND_0", "AirbusFBW/LeftGearInd", true, v => ((int)Math.Round(v) & 1) != 0),
            ("MIP_GEAR_IND_0", "AirbusFBW/LeftGearInd", false, v => ((int)Math.Round(v) & 2) != 0),
            ("MIP_GEAR_IND_1", "AirbusFBW/NoseGearInd", true, v => ((int)Math.Round(v) & 1) != 0),
            ("MIP_GEAR_IND_1", "AirbusFBW/NoseGearInd", false, v => ((int)Math.Round(v) & 2) != 0),
            ("MIP_GEAR_IND_2", "AirbusFBW/RightGearInd", true, v => ((int)Math.Round(v) & 1) != 0),
            ("MIP_GEAR_IND_2", "AirbusFBW/RightGearInd", false, v => ((int)Math.Round(v) & 2) != 0),
            ("MIP_AUTOBRK_LO", "AirbusFBW/AutoBrkLo", false, v => v >= 0.5),
            ("MIP_AUTOBRK_LO", "AirbusFBW/AutoBrkLo", true, v => v >= 1.5),
            ("MIP_AUTOBRK_MED", "AirbusFBW/AutoBrkMed", false, v => v >= 0.5),
            ("MIP_AUTOBRK_MED", "AirbusFBW/AutoBrkMed", true, v => v >= 1.5),
            ("MIP_AUTOBRK_MAX", "AirbusFBW/AutoBrkMax", false, v => v >= 0.5),
            ("MIP_AUTOBRK_MAX", "AirbusFBW/AutoBrkMax", true, v => v >= 1.5),
            ("MIP_BRK_FAN", "AirbusFBW/BrakeFan", false, v => v >= 0.5),
            ("MIP_BRK_FAN", BrakeTempDr, true, v => v >= 300),     // HOT: algun fre per sobre de 300 °C
        };
        /// <summary>Temperatura dels frens (una per roda): es guarda la més alta.</summary>
        const string BrakeTempDr = "AirbusFBW/BrakeTemperatureArray";
        /// <summary>Llegenda d'un polsador o indicador: (element, llegenda de dalt?, encesa) (al fil de la interfície).</summary>
        public event Action<string, bool, bool>? LegendChanged;
        readonly Dictionary<(string, bool), bool> _lastLegends = new();

        /// <summary>Rellotge del panell principal: cronòmetre (s), ET, si es veu l'ET, i hora UTC.</summary>
        static readonly string[] ClockReads =
        {
            "AirbusFBW/ClockChronoValue", "AirbusFBW/ClockETHours", "AirbusFBW/ClockETMinutes", "AirbusFBW/ClockShowsET",
            "sim/cockpit2/clock_timer/zulu_time_hours", "sim/cockpit2/clock_timer/zulu_time_minutes", "sim/cockpit2/clock_timer/zulu_time_seconds",
            "sim/cockpit2/clock_timer/zulu_day", "sim/cockpit2/clock_timer/zulu_month",
            ChronoBtnDr,
        };
        /// <summary>Animació dels botons del rellotge [CHR, RST, DATE, …]: mentre DATE és premut, la pantalla UTC mostra la data.</summary>
        const string ChronoBtnDr = "AirbusFBW/ChronoButtonAnimations";
        /// <summary>Ordres que s'han de mantenir premudes un temps fix (s); la resta, un toc (0).</summary>
        static readonly Dictionary<string, double> CmdHold = new();
        const string DateHeldKey = "#DATE_HELD";
        /// <summary>Pantalles del rellotge (cronòmetre "MM:SS", UTC "HH:MM:SS", ET "HH:MM"; "" = apagada) (al fil de la interfície).</summary>
        public event Action<string, string, string>? ClockChanged;
        (string, string, string)? _lastClock;
        /// <summary>Palanques de gas del ToLiss, una per motor (mesurat: -1 REV FULL, 0 ralentí, 0,7 CL, ≈0,875 FLX MCT, 1 TO GA).</summary>
        const string ThrDr = "AirbusFBW/throttle_input";
        static readonly double[] ThrSim = { -1, 0, 0.7, 0.875, 1 };
        static readonly double[] ThrApp = { -1, 0, 0.58, 0.8, 1 };
        static double MapLin(double v, double[] from, double[] to)
        {
            if (v <= from[0]) return to[0];
            for (int i = 0; i < from.Length - 1; i++)
                if (v <= from[i + 1]) return to[i] + (v - from[i]) / (from[i + 1] - from[i]) * (to[i + 1] - to[i]);
            return to[^1];
        }
        /// <summary>Palanques de gas (posició de l'app: -1 REV FULL … 0 ralentí … 1 TO GA, esquerra i dreta) (al fil de la interfície).</summary>
        public event Action<float, float>? ThrottleChanged;
        (float, float)? _lastThr;

        static IEnumerable<string> AllReads()
        {
            yield return ThrDr;
            foreach (var n in ReadDatarefs) yield return n;
            foreach (var l in LightMap) yield return l.dataref;
            foreach (var p in PosMap) yield return p.dataref;
            foreach (var l in LegendMap) yield return l.dataref;
            foreach (var n in ClockReads) yield return n;
        }

        /// <summary>true = hi ha un ToLiss carregat a X-Plane i l'enllaç està connectat.</summary>
        public bool A320Loaded { get; private set; }
        /// <summary>Canvi de ToLiss carregat / no carregat (al fil de la interfície).</summary>
        public event Action<bool>? A320Changed;
        /// <summary>QNH del capità: amb corrent, mode (0 STD, 1 QNH), valor (hPa o inHg), unitat inHg (al fil de la interfície).</summary>
        public event Action<bool, int, double, bool>? BaroChanged;
        /// <summary>Pantalla del FCU (al fil de la interfície).</summary>
        public event Action<panelAIR1000.FcuState>? FcuChanged;
        /// <summary>Selector 100 / 1000 de l'ALT: true = 1000 (al fil de la interfície).</summary>
        public event Action<bool>? AltStepChanged;
        /// <summary>Llum d'una tecla o polsador (id de l'element, encès) (al fil de la interfície).</summary>
        public event Action<string, bool>? LightChanged;
        /// <summary>Posició d'un selector (id de l'element, índex) (al fil de la interfície).</summary>
        public event Action<string, int>? PositionChanged;

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
        bool? _lastAltStep;
        readonly Dictionary<string, bool> _lastLights = new();
        readonly Dictionary<string, int> _lastPos = new();

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
            _cmdIds.Clear(); _drIds.Clear(); _drNames.Clear(); _values.Clear(); _lastBaro = null; _lastFcu = null; _lastAltStep = null; _lastLights.Clear(); _lastPos.Clear(); _lastThr = null; _lastLegends.Clear(); _lastClock = null; _lastGear = null;
            foreach (var name in AllReads())
                if (!_drIds.ContainsKey(name) && await LookupId("datarefs", name, ct) is long id) { _drIds[name] = id; _drNames[id] = name; }
            foreach (var w in WriteMap.Values)
                if (!_drIds.ContainsKey(w.dataref) && await LookupId("datarefs", w.dataref, ct) is long id) _drIds[w.dataref] = id;
            foreach (var mdr in PosMirror.Values)
                if (!_drIds.ContainsKey(mdr) && await LookupId("datarefs", mdr, ct) is long id) _drIds[mdr] = id;
            foreach (var c in CommandMap.Values)
                if (!_cmdIds.ContainsKey(c) && await LookupId("commands", c, ct) is long id) _cmdIds[c] = id;

            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(WsUrl), ct);
            _ws = ws;
            try
            {
                var subs = new List<object>();
                foreach (var id in _drNames.Keys) subs.Add(new { id });
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
                if (name == ThrDr)
                {
                    // un valor per motor: es publiquen els dos primers
                    if (v.ValueKind == JsonValueKind.Array && v.GetArrayLength() >= 2 && v[0].ValueKind == JsonValueKind.Number && v[1].ValueKind == JsonValueKind.Number)
                    {
                        var thr = ((float)MapLin(v[0].GetDouble(), ThrSim, ThrApp), (float)MapLin(v[1].GetDouble(), ThrSim, ThrApp));
                        if (_lastThr != thr) { _lastThr = thr; Post(() => ThrottleChanged?.Invoke(thr.Item1, thr.Item2)); }
                    }
                    continue;
                }
                if (name == ChronoBtnDr && v.ValueKind == JsonValueKind.Array)
                {
                    // tercer element: botó DATE (mesurat al ToLiss: 1 mentre es mostra la data)
                    if (v.GetArrayLength() > 2 && v[2].ValueKind == JsonValueKind.Number) lock (_values) _values[DateHeldKey] = v[2].GetDouble();
                    continue;
                }
                if (name == BrakeTempDr && v.ValueKind == JsonValueKind.Array)
                {
                    // temperatura dels frens: la més alta de totes les rodes
                    double max = double.MinValue;
                    foreach (var e in v.EnumerateArray()) if (e.ValueKind == JsonValueKind.Number) max = Math.Max(max, e.GetDouble());
                    if (max > double.MinValue) lock (_values) _values[name] = max;
                    continue;
                }
                if (v.ValueKind == JsonValueKind.Array) { if (v.GetArrayLength() == 0) continue; v = v[0]; }
                if (v.ValueKind == JsonValueKind.Number) lock (_values) _values[name] = v.GetDouble();
            }
            lock (_values) { PublishBaro(); PublishFcu(); PublishLights(); PublishPositions(); PublishLegends(); PublishClock(); PublishGear(); }
        }

        void PublishGear()
        {
            if (!_values.TryGetValue(GearHandleDr, out var v)) return;
            bool down = v >= 0.5;
            if (_lastGear == down) return;
            _lastGear = down;
            Post(() => GearChanged?.Invoke(down));
        }

        void PublishLegends()
        {
            foreach (var (id, dr, upper, onFn) in LegendMap)
            {
                if (!_values.TryGetValue(dr, out var v)) continue;
                bool on = onFn(v);
                if (_lastLegends.TryGetValue((id, upper), out var was) && was == on) continue;
                _lastLegends[(id, upper)] = on;
                Post(() => LegendChanged?.Invoke(id, upper, on));
            }
        }

        void PublishClock()
        {
            if (!_values.ContainsKey("sim/cockpit2/clock_timer/zulu_time_hours")) return;
            int I(string n) => (int)Math.Round(V(n));
            double chr = V("AirbusFBW/ClockChronoValue");
            int cs = (int)Math.Floor(chr);
            // cronòmetre MM:SS (fins a 99:59). Com al ToLiss: parat + RST (valor exactament 0) → pantalla apagada fins
            // que es torna a prémer CHR; en marxa + RST torna a 0 però continua comptant, i es veu 00:00, 00:01…
            string chrTxt = chr > 0 ? $"{Math.Min(99, cs / 60):00}:{cs % 60:00}" : "";
            // UTC: l'hora; mentre DATE és premut, la data MM:DD:AA (X-Plane no té l'any: el del rellotge del PC)
            string utc = V(DateHeldKey) > 0.5
                ? $"{I("sim/cockpit2/clock_timer/zulu_month"):00}:{I("sim/cockpit2/clock_timer/zulu_day"):00}:{DateTime.UtcNow.Year % 100:00}"
                : $"{I("sim/cockpit2/clock_timer/zulu_time_hours"):00}:{I("sim/cockpit2/clock_timer/zulu_time_minutes"):00}:{I("sim/cockpit2/clock_timer/zulu_time_seconds"):00}";
            string et = B("AirbusFBW/ClockShowsET") ? $"{I("AirbusFBW/ClockETHours"):00}:{I("AirbusFBW/ClockETMinutes"):00}" : "";
            var c = (chrTxt, utc, et);
            if (_lastClock == c) return;
            _lastClock = c;
            Post(() => ClockChanged?.Invoke(c.chrTxt, c.utc, c.et));
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
                // en mode TRK-FPA el ToLiss guarda l'FPA a vertical_velocity multiplicat per 1000 (200 = 0.2°);
                // el dataref estàndard sim/cockpit2/autopilot/fpa queda sempre a 0
                V("sim/cockpit/autopilot/vertical_velocity"), V("sim/cockpit/autopilot/vertical_velocity") / 1000.0, B("AirbusFBW/VSdashed"));
            if (s != _lastFcu) { _lastFcu = s; Post(() => FcuChanged?.Invoke(s)); }
            if (_values.ContainsKey("AirbusFBW/ALT100_1000"))
            {
                bool k = B("AirbusFBW/ALT100_1000");
                if (k != _lastAltStep) { _lastAltStep = k; Post(() => AltStepChanged?.Invoke(k)); }
            }
        }

        void PublishLights()
        {
            foreach (var (id, dr, min) in LightMap)
            {
                if (!_values.TryGetValue(dr, out var v)) continue;
                bool on = v >= min;
                if (_lastLights.TryGetValue(id, out var was) && was == on) continue;
                _lastLights[id] = on;
                Post(() => LightChanged?.Invoke(id, on));
            }
        }

        void PublishPositions()
        {
            foreach (var (id, dr, toSim) in PosMap)
            {
                if (!_values.TryGetValue(dr, out var v)) continue;
                int idx = toSim == null ? (int)Math.Round(v) : Array.IndexOf(toSim, (int)Math.Round(v));
                if (idx < 0) continue;
                if (_lastPos.TryGetValue(id, out var was) && was == idx) continue;
                _lastPos[id] = idx;
                Post(() => PositionChanged?.Invoke(id, idx));
            }
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
            if (id == "MIP_THROTTLE")
            {
                // palanques arrossegades a l'app ("BOTH:v", "LEFT:v", "RIGHT:v"): s'escriu throttle_input de cada motor
                // (el ToLiss només hi torna a posar la palanca física quan aquesta es mou)
                int colon = cmd.IndexOf(':');
                if (colon < 0 || !_drIds.TryGetValue(ThrDr, out var tid) || !double.TryParse(cmd.AsSpan(colon + 1),
                        System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double p)) return;
                double sim = Math.Round(MapLin(p, ThrApp, ThrSim), 4);
                string which = cmd[..colon];
                var drs = new List<object>();
                if (which != "RIGHT") drs.Add(new { id = tid, value = sim, index = 0 });
                if (which != "LEFT") drs.Add(new { id = tid, value = sim, index = 1 });
                _ = SendAsync(new { req_id = NextReq(), type = "dataref_set_values", @params = new { datarefs = drs } });
                return;
            }
            // selectors: "POS:n" → escriure n al dataref del selector
            if (cmd.StartsWith("POS:") && int.TryParse(cmd.AsSpan(4), out var pos))
            {
                foreach (var (pid, dr, toSim) in PosMap)
                    if (pid == id && _drIds.TryGetValue(dr, out var pdid))
                    {
                        if (toSim != null && (pos < 0 || pos >= toSim.Length)) continue;
                        double val = toSim == null ? pos : toSim[pos];
                        lock (_values) _lastPos[id] = pos;   // ja és on toca: no cal tornar-lo a moure quan arribi l'eco
                        var drs = new List<object> { new { id = pdid, value = val } };
                        // (p.ex. el pom RUN del rellotge: l'animació s'ha d'escriure a part)
                        if (PosMirror.TryGetValue(id, out var mdr) && _drIds.TryGetValue(mdr, out var mid)) drs.Add(new { id = mid, value = val });
                        _ = SendAsync(new { req_id = NextReq(), type = "dataref_set_values", @params = new { datarefs = drs } });
                    }
                return;
            }
            // botons que es mantenen (DATE del rellotge): l'ordre queda activa mentre el dit és a sobre
            if ((cmd == "DOWN" || cmd == "UP") && CommandMap.TryGetValue((id, "PUSH"), out var hc) && _cmdIds.TryGetValue(hc, out var hid))
            {
                _ = SendAsync(new { req_id = NextReq(), type = "command_set_is_active", @params = new { commands = new[] { new { id = hid, is_active = cmd == "DOWN" } } } });
                return;
            }
            object? req = null;
            if (CommandMap.TryGetValue((id, cmd), out var c) && _cmdIds.TryGetValue(c, out var cid))
                req = new { req_id = NextReq(), type = "command_set_is_active", @params = new { commands = new[] { new { id = cid, is_active = true, duration = CmdHold.GetValueOrDefault(c) } } } };
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

        /// <summary>Com SendCommand, però espera (fins a <paramref name="ms"/>) que l'ordre hagi sortit (p.ex. en tancar l'app).</summary>
        public void SendCommandAndWait(string id, string cmd, int ms = 1000)
        {
            if (!A320Loaded || _ws == null) return;
            if (!CommandMap.TryGetValue((id, cmd), out var c) || !_cmdIds.TryGetValue(c, out var cid)) return;
            try { SendAsync(new { req_id = NextReq(), type = "command_set_is_active", @params = new { commands = new[] { new { id = cid, is_active = true, duration = 0.0 } } } }).Wait(ms); }
            catch { }
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

    /// <summary>
    /// Finestres d'X-Plane (pop-out) a Windows: troba una finestra d'X-Plane pel títol i la col·loca sense marc
    /// exactament en un rectangle de pantalla, just darrere d'una altra finestra (la de l'app).
    /// </summary>
    internal static class XPlaneWindows
    {
        delegate bool EnumProc(IntPtr h, IntPtr l);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr l);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern int GetWindowThreadProcessId(IntPtr h, out int pid);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr h, int i);
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr h, int i, IntPtr v);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr h, uint cmd);
        struct RECT { public int L, T, R, B; }
        struct POINT { public int X, Y; }
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr h, ref POINT p);
        static readonly HashSet<IntPtr> s_stripped = new();
        static readonly Dictionary<IntPtr, int> s_reframed = new();
        const int GWL_STYLE = -16;
        const long WS_POPUP = 0x80000000L, WS_FRAME = 0x00CF0000L;   // WS_OVERLAPPEDWINDOW: títol, vores, botons
        const uint SWP_NOACTIVATE = 0x0010, SWP_FRAMECHANGED = 0x0020, SWP_SHOWWINDOW = 0x0040;
        const uint GW_HWNDPREV = 3;

        // Llista de processos: buscar-la (GetProcessesByName) és lent (desenes de ms) i abans es feia per a cada títol
        // cada segon, cosa que aturava l'app uns 300 ms; ara es reaprofita durant 3 s.
        static readonly Dictionary<string, (HashSet<int> pids, long at)> s_pidCache = new();
        static HashSet<int> Pids(string[] processNames)
        {
            string key = string.Join("|", processNames);
            long now = Environment.TickCount64;
            if (s_pidCache.TryGetValue(key, out var c) && now - c.at < 3000) return c.pids;
            var pids = new HashSet<int>();
            foreach (var n in processNames)
                foreach (var p in System.Diagnostics.Process.GetProcessesByName(n)) { pids.Add(p.Id); p.Dispose(); }
            s_pidCache[key] = (pids, now);
            return pids;
        }

        /// <summary>Per a cada títol, la primera finestra visible d'aquests processos que el té (una sola passada).</summary>
        public static Dictionary<string, IntPtr> FindEach(IEnumerable<string> titles, params string[] processNames)
        {
            var res = new Dictionary<string, IntPtr>();
            foreach (var t in titles) res[t] = IntPtr.Zero;
            var pids = Pids(processNames);
            if (pids.Count == 0) return res;
            EnumWindows((h, l) =>
            {
                GetWindowThreadProcessId(h, out int pid);
                if (!pids.Contains(pid) || !IsWindowVisible(h)) return true;
                var sb = new StringBuilder(256);
                GetWindowText(h, sb, 256);
                if (res.TryGetValue(sb.ToString(), out var cur) && cur == IntPtr.Zero) res[sb.ToString()] = h;
                return true;
            }, IntPtr.Zero);
            return res;
        }

        /// <summary>Finestra visible d'X-Plane amb aquest títol (o IntPtr.Zero).</summary>
        public static IntPtr Find(string title) => Find(title, "X-Plane");

        /// <summary>Finestra visible d'algun d'aquests processos (p.ex. "FlightSimulator") amb aquest títol (o IntPtr.Zero).</summary>
        public static IntPtr Find(string title, params string[] processNames)
        {
            var pids = Pids(processNames);
            if (pids.Count == 0) return IntPtr.Zero;
            IntPtr found = IntPtr.Zero;
            EnumWindows((h, l) =>
            {
                GetWindowThreadProcessId(h, out int pid);
                if (!pids.Contains(pid) || !IsWindowVisible(h)) return true;
                var sb = new StringBuilder(256);
                GetWindowText(h, sb, 256);
                if (sb.ToString() == title) { found = h; return false; }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        /// <summary>Totes les finestres visibles d'aquests processos amb aquest títol, de la de dalt a la de sota.</summary>
        public static List<IntPtr> FindAll(string title, params string[] processNames)
        {
            var pids = Pids(processNames);
            var list = new List<IntPtr>();
            if (pids.Count == 0) return list;
            EnumWindows((h, l) =>
            {
                GetWindowThreadProcessId(h, out int pid);
                if (!pids.Contains(pid) || !IsWindowVisible(h)) return true;
                var sb = new StringBuilder(256);
                GetWindowText(h, sb, 256);
                if (sb.ToString() == title) list.Add(h);
                return true;
            }, IntPtr.Zero);
            return list;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
        /// <summary>Imatge del contingut d'una finestra (també de les pop-out de l'MSFS); null si no es pot.</summary>
        public static System.Drawing.Bitmap? Capture(IntPtr h)
        {
            if (!GetWindowRect(h, out var r)) return null;
            int w = r.R - r.L, hh = r.B - r.T;
            if (w < 50 || hh < 50) return null;
            var b = new System.Drawing.Bitmap(w, hh, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(b))
            {
                var dc = g.GetHdc();
                try { PrintWindow(h, dc, 2); } finally { g.ReleaseHdc(dc); }
            }
            return b;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
        /// <summary>Demana a la finestra que es tanqui (WM_CLOSE), com si es premés la seva creu.</summary>
        public static void Close(IntPtr h) => PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero);

        /// <summary>
        /// Treu el marc a la finestra i la posa exactament a <paramref name="screen"/>, just darrere de <paramref name="behind"/>.
        /// Només toca la finestra si no hi és ja (es pot cridar sovint).
        /// </summary>
        public static void Place(IntPtr h, System.Drawing.Rectangle screen, IntPtr behind)
        {
            long style = GetWindowLongPtr(h, GWL_STYLE).ToInt64();
            bool framed = (style & WS_FRAME) != 0;
            // Hi ha finestres a les quals algú (el simulador o una altra eina) torna a posar el marc just després de
            // treure-l'hi: treure'l cada segon fa pampallugues amb el títol. Si passa dues vegades, es deixa el marc
            // i es col·loca la finestra de manera que la seva zona interior quedi al forat (el marc queda amagat
            // darrere del panell de l'app).
            if (framed && s_stripped.Contains(h)) s_reframed[h] = s_reframed.GetValueOrDefault(h) + 1;
            if (framed && s_reframed.GetValueOrDefault(h) >= 2)
            {
                if (!GetWindowRect(h, out var wr) || !GetClientRect(h, out var cr)) return;
                var org = new POINT();
                ClientToScreen(h, ref org);
                int dl = org.X - wr.L, dt = org.Y - wr.T, dr = wr.R - (org.X + cr.R), db = wr.B - (org.Y + cr.B);
                var want = System.Drawing.Rectangle.FromLTRB(screen.Left - dl, screen.Top - dt, screen.Right + dr, screen.Bottom + db);
                bool ok = wr.L == want.Left && wr.T == want.Top && wr.R == want.Right && wr.B == want.Bottom;
                if (!ok || GetWindow(h, GW_HWNDPREV) != behind)
                    SetWindowPos(h, behind, want.X, want.Y, want.Width, want.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
                return;
            }
            if (framed) { SetWindowLongPtr(h, GWL_STYLE, new IntPtr((style & ~WS_FRAME) | WS_POPUP)); s_stripped.Add(h); }
            GetWindowRect(h, out var r);
            bool placed = r.L == screen.Left && r.T == screen.Top && r.R == screen.Right && r.B == screen.Bottom;
            bool zOk = GetWindow(h, GW_HWNDPREV) == behind;   // la finestra de just a sobre és la de l'app
            if (framed || !placed || !zOk)
                SetWindowPos(h, behind, screen.X, screen.Y, screen.Width, screen.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW | (framed ? SWP_FRAMECHANGED : 0));
        }
    }
}