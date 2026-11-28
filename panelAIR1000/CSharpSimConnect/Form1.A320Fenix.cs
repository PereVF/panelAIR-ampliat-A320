using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;

namespace SimConnect3
{
    /// <summary>
    /// A320 del Fenix a l'MSFS: glareshield (FCU, EFIS del capità, QNH, MASTER WARN/CAUT, CHRONO, TERR ON ND), en els
    /// dos sentits. Codis presos dels presets de HubHop (MobiFlight) del Fenix A320.
    ///  - Ordres: codi RPN sobre les L-vars del Fenix, enviat pel canal genèric de l'app (SendGenericRpn).
    ///    Els botons es premen i es deixen anar (dos codis separats uns 150 ms), com fa el Fenix.
    ///  - Estat: una sola lectura de L-vars que només arriba quan canvia.
    /// Les pantalles pop-out del Fenix es diuen totes "WASMINSTRUMENT": s'identifiquen per l'ordre en què es treuen.
    /// </summary>
    public partial class frmMain
    {
        // Fenix A320 (títol de l'avió amb "Fenix" o "FNX")
        bool IsA320Fenix() =>
            _currentAircraftTitle.IndexOf("Fenix", StringComparison.OrdinalIgnoreCase) >= 0 ||
            _currentAircraftTitle.IndexOf("FNX", StringComparison.OrdinalIgnoreCase) >= 0;
        /// <summary>Qualsevol A320 de l'MSFS que l'app sap fer anar (FlyByWire o Fenix).</summary>
        bool IsA320Msfs() => IsA320FBW() || IsA320Fenix();

        const uint FenixReadDef = 7300, FenixReadReq = 7300;

        // ── Ordres: element + acció → codi en prémer (i en deixar anar, si cal) ──
        static string Inc(string lv) => $"(L:{lv}) ++ (>L:{lv})";
        static string Dec(string lv) => $"(L:{lv}) -- (>L:{lv})";
        static string Set(string lv, int v) => $"{v} (>L:{lv})";
        static string Even(string lv) => $"(L:{lv}) s0 2 % 0 != if{{ l0 ++ (>L:{lv}) }}";   // deixar anar: comptador parell

        static readonly (string id, string cmd, string press, string? release)[] FenixCmds =
        {
            // rotatius del FCU: girar (E_), prémer (S_ --) i estirar (S_ ++)
            ("MIP_FCU_SPD", "INC", Inc("E_FCU_SPEED"), null), ("MIP_FCU_SPD", "DEC", Dec("E_FCU_SPEED"), null),
            ("MIP_FCU_SPD", "PUSH", Dec("S_FCU_SPEED"), null), ("MIP_FCU_SPD", "PULL", Inc("S_FCU_SPEED"), null),
            ("MIP_FCU_HDG", "INC", Inc("E_FCU_HEADING"), null), ("MIP_FCU_HDG", "DEC", Dec("E_FCU_HEADING"), null),
            ("MIP_FCU_HDG", "PUSH", Dec("S_FCU_HEADING"), null), ("MIP_FCU_HDG", "PULL", Inc("S_FCU_HEADING"), null),
            ("MIP_FCU_ALT", "INC", Inc("E_FCU_ALTITUDE"), null), ("MIP_FCU_ALT", "DEC", Dec("E_FCU_ALTITUDE"), null),
            ("MIP_FCU_ALT", "PUSH", Dec("S_FCU_ALTITUDE"), null), ("MIP_FCU_ALT", "PULL", Inc("S_FCU_ALTITUDE"), null),
            ("MIP_FCU_ALT", "STEP100", Set("S_FCU_ALTITUDE_SCALE", 0), null), ("MIP_FCU_ALT", "STEP1000", Set("S_FCU_ALTITUDE_SCALE", 1), null),
            ("MIP_FCU_VS", "INC", Inc("E_FCU_VS"), null), ("MIP_FCU_VS", "DEC", Dec("E_FCU_VS"), null),
            ("MIP_FCU_VS", "PUSH", Dec("S_FCU_VERTICAL_SPEED"), null), ("MIP_FCU_VS", "PULL", Inc("S_FCU_VERTICAL_SPEED"), null),
            // botons del FCU
            ("MIP_FCU_SPD_MACH", "PUSH", Inc("S_FCU_SPD_MACH"), Inc("S_FCU_SPD_MACH")),
            ("MIP_FCU_HDGTRK_VSFPA", "PUSH", Inc("S_FCU_HDGVS_TRKFPA"), Inc("S_FCU_HDGVS_TRKFPA")),
            ("MIP_FCU_METRIC_ALT", "PUSH", Inc("S_FCU_METRIC_ALT"), Even("S_FCU_METRIC_ALT")),
            ("MIP_FCU_AP1", "PUSH", Inc("S_FCU_AP1"), Inc("S_FCU_AP1")),
            ("MIP_FCU_AP2", "PUSH", Inc("S_FCU_AP2"), Inc("S_FCU_AP2")),
            ("MIP_FCU_EXPED", "PUSH", Inc("S_FCU_EXPED"), Inc("S_FCU_EXPED")),
            ("MIP_FCU_ATHR", "PUSH", Inc("S_FCU_ATHR"), Even("S_FCU_ATHR")),
            ("MIP_FCU_LOC", "PUSH", Inc("S_FCU_LOC"), Even("S_FCU_LOC")),
            ("MIP_FCU_APPR", "PUSH", Inc("S_FCU_APPR"), Even("S_FCU_APPR")),
            // EFIS del capità
            ("MIP_EFIS_FD", "PUSH", Set("S_FCU_EFIS1_FD", 1), Set("S_FCU_EFIS1_FD", 0)),
            ("MIP_EFIS_LS", "PUSH", Set("S_FCU_EFIS1_LS", 1), Set("S_FCU_EFIS1_LS", 0)),
            ("MIP_EFIS_CSTR", "PUSH", Set("S_FCU_EFIS1_CSTR", 1), Set("S_FCU_EFIS1_CSTR", 0)),
            ("MIP_EFIS_WPT", "PUSH", Set("S_FCU_EFIS1_WPT", 1), Set("S_FCU_EFIS1_WPT", 0)),
            ("MIP_EFIS_VORD", "PUSH", Set("S_FCU_EFIS1_VORD", 1), Set("S_FCU_EFIS1_VORD", 0)),
            ("MIP_EFIS_NDB", "PUSH", Set("S_FCU_EFIS1_NDB", 1), Set("S_FCU_EFIS1_NDB", 0)),
            ("MIP_EFIS_ARPT", "PUSH", Set("S_FCU_EFIS1_ARPT", 1), Set("S_FCU_EFIS1_ARPT", 0)),
            // QNH del capità
            ("MIP_BARO", "INC", Inc("E_FCU_EFIS1_BARO"), null), ("MIP_BARO", "DEC", Dec("E_FCU_EFIS1_BARO"), null),
            ("MIP_BARO", "PUSH", Dec("S_FCU_EFIS1_BARO_STD"), null), ("MIP_BARO", "PULL", Inc("S_FCU_EFIS1_BARO_STD"), null),
            ("MIP_BARO", "INHG", Set("S_FCU_EFIS1_BARO_MODE", 0), null), ("MIP_BARO", "HPA", Set("S_FCU_EFIS1_BARO_MODE", 1), null),
            // glareshield i panell principal
            ("MIP_MASTER_WARN", "PUSH", Set("S_MIP_MASTER_WARNING_CAPT", 1), Set("S_MIP_MASTER_WARNING_CAPT", 0)),
            ("MIP_MASTER_CAUT", "PUSH", Set("S_MIP_MASTER_CAUTION_CAPT", 1), Set("S_MIP_MASTER_CAUTION_CAPT", 0)),
            ("MIP_CHRONO", "PUSH", Set("S_MIP_CHRONO_CAPT", 1), Set("S_MIP_CHRONO_CAPT", 0)),
            // TERR ON ND: comptador (comprovat al Fenix; amb 1/0 no feia res)
            ("MIP_TERR_ON_ND_1", "PUSH", Inc("S_MIP_GPWS_TERRAIN_ON_ND_CAPT"), Even("S_MIP_GPWS_TERRAIN_ON_ND_CAPT")),
            ("MIP_TERR_ON_ND_2", "PUSH", Inc("S_MIP_GPWS_TERRAIN_ON_ND_FO"), Even("S_MIP_GPWS_TERRAIN_ON_ND_FO")),
            // rellotge, autofre i BRK FAN (comprovat al Fenix: CHR, RST, DATE i autofre són comptadors, senar = premut;
            // BRK FAN és un interruptor que es canvia)
            ("MIP_CLK_CHR", "PUSH", Inc("S_MIP_CLOCK_CHR"), Even("S_MIP_CLOCK_CHR")),
            ("MIP_CLK_RST", "PUSH", Inc("S_MIP_CLOCK_RST"), Even("S_MIP_CLOCK_RST")),
            ("MIP_CLK_DATE", "DOWN", Inc("S_MIP_CLOCK_SET"), null),     // la data es veu mentre es manté premut
            ("MIP_CLK_DATE", "UP", Even("S_MIP_CLOCK_SET"), null),
            ("MIP_AUTOBRK_LO", "PUSH", Inc("S_MIP_AUTOBRAKE_LO"), Even("S_MIP_AUTOBRAKE_LO")),
            ("MIP_AUTOBRK_MED", "PUSH", Inc("S_MIP_AUTOBRAKE_MED"), Even("S_MIP_AUTOBRAKE_MED")),
            ("MIP_AUTOBRK_MAX", "PUSH", Inc("S_MIP_AUTOBRAKE_MAX"), Even("S_MIP_AUTOBRAKE_MAX")),
            ("MIP_BRK_FAN", "PUSH", "(L:S_MIP_BRAKE_FAN) ! (>L:S_MIP_BRAKE_FAN)", null),
            // ISIS (al Fenix, "ISFD"): botons comptadors (llegit: parells = deixats anar); pom: girar (E_) i prémer (comptador)
            ("MIP_ISIS_BUGS", "PUSH", Inc("S_MIP_ISFD_BUGS"), Even("S_MIP_ISFD_BUGS")),
            ("MIP_ISIS_LS", "PUSH", Inc("S_MIP_ISFD_LS"), Even("S_MIP_ISFD_LS")),
            ("MIP_ISIS_PLUS", "PUSH", Inc("S_MIP_ISFD_PLUS"), Even("S_MIP_ISFD_PLUS")),
            ("MIP_ISIS_MINUS", "PUSH", Inc("S_MIP_ISFD_MINUS"), Even("S_MIP_ISFD_MINUS")),
            ("MIP_ISIS_RST", "PUSH", Inc("S_MIP_ISFD_RST"), Even("S_MIP_ISFD_RST")),
            ("MIP_ISIS_BARO", "INC", Inc("E_MIP_ISFD_BARO"), null),
            ("MIP_ISIS_BARO", "DEC", Dec("E_MIP_ISFD_BARO"), null),
            ("MIP_ISIS_BARO", "PUSH", Inc("S_MIP_ISFD_BARO_BUTTON"), Even("S_MIP_ISFD_BARO_BUTTON")),
            // palanca del tren (HubHop: 1 = avall, 0 = amunt; llegit al Fenix: 1 amb el tren baixat)
            ("MIP_GEAR_LEVER", "DOWN", Set("S_MIP_GEAR", 1), null),
            ("MIP_GEAR_LEVER", "UP", Set("S_MIP_GEAR", 0), null),
        };

        /// <summary>
        /// Selectors: element → L-var. toSim: valor de la L-var per a cada posició de l'app (null = mateixa numeració).
        /// </summary>
        static readonly (string id, string lvar, int[]? toSim)[] FenixPos =
        {
            ("MIP_ND_MODE", "S_FCU_EFIS1_ND_MODE", null),        // 0 LS, 1 VOR, 2 NAV, 3 ARC, 4 PLAN
            ("MIP_ND_RANGE", "S_FCU_EFIS1_ND_ZOOM", null),       // 0 = 10 … 5 = 320 NM
            ("MIP_EFIS_ADFVOR1", "S_FCU_EFIS1_NAV1", null),      // 0 ADF, 1 OFF, 2 VOR
            ("MIP_EFIS_ADFVOR2", "S_FCU_EFIS1_NAV2", null),
            ("MIP_CLK_GPS", "S_MIP_CLOCK_UTC", null),            // 0 GPS, 1 INT, 2 SET
            ("MIP_CLK_RUN", "S_MIP_CLOCK_ET", null),             // 0 RUN, 1 STP, 2 RST (molla, torna a STP)
            ("MIP_ASKID_NWSTRG", "S_FC_MIP_ANTI_SKID", new[] { 1, 0 }),   // app 0 = ON (amunt); Fenix 1 = ON
        };

        /// <summary>Llegendes de dalt / de baix: indicadors del tren (UNLK / triangle), autofre (DECEL / ON), BRK FAN (HOT / ON).</summary>
        static readonly (string id, string lvar, bool upper)[] FenixLegends =
        {
            ("MIP_GEAR_IND_0", "I_MIP_GEAR_1_U", true), ("MIP_GEAR_IND_0", "I_MIP_GEAR_1_L", false),
            ("MIP_GEAR_IND_1", "I_MIP_GEAR_2_U", true), ("MIP_GEAR_IND_1", "I_MIP_GEAR_2_L", false),
            ("MIP_GEAR_IND_2", "I_MIP_GEAR_3_U", true), ("MIP_GEAR_IND_2", "I_MIP_GEAR_3_L", false),
            ("MIP_AUTOBRK_LO", "I_MIP_AUTOBRAKE_LO_U", true), ("MIP_AUTOBRK_LO", "I_MIP_AUTOBRAKE_LO_L", false),
            ("MIP_AUTOBRK_MED", "I_MIP_AUTOBRAKE_MED_U", true), ("MIP_AUTOBRK_MED", "I_MIP_AUTOBRAKE_MED_L", false),
            ("MIP_AUTOBRK_MAX", "I_MIP_AUTOBRAKE_MAX_U", true), ("MIP_AUTOBRK_MAX", "I_MIP_AUTOBRAKE_MAX_L", false),
            ("MIP_BRK_FAN", "I_MIP_BRAKE_FAN_U", true), ("MIP_BRK_FAN", "I_MIP_BRAKE_FAN_L", false),
        };

        /// <summary>Rellotge: les tres pantalles (en el format de la pantalla) i el mode de la UTC.</summary>
        static readonly string[] FenixClock = { "N_MIP_CLOCK_CHRONO", "N_MIP_CLOCK_ELAPSED", "N_MIP_CLOCK_UTC", "N_MIP_CLOCK_MODE" };
        /// <summary>Indicador triple (agulles, ja en milers de PSI: acumulador, fre esquerre, fre dret) i palanca del tren (1 = avall).</summary>
        static readonly string[] FenixGear = { "N_HYD_PRESSURE_BRAKE_ACCU", "N_HYD_PRESSURE_BRAKE_LEFT", "N_HYD_PRESSURE_BRAKE_RIGHT", "S_MIP_GEAR" };

        /// <summary>Llums: element → L-var.</summary>
        static readonly (string id, string lvar)[] FenixLights =
        {
            ("MIP_FCU_AP1", "I_FCU_AP1"), ("MIP_FCU_AP2", "I_FCU_AP2"), ("MIP_FCU_ATHR", "I_FCU_ATHR"),
            ("MIP_FCU_LOC", "I_FCU_LOC"), ("MIP_FCU_APPR", "I_FCU_APPR"), ("MIP_FCU_EXPED", "I_FCU_EXPED"),
            ("MIP_EFIS_FD", "I_FCU_EFIS1_FD"), ("MIP_EFIS_LS", "I_FCU_EFIS1_LS"), ("MIP_EFIS_CSTR", "I_FCU_EFIS1_CSTR"),
            ("MIP_EFIS_WPT", "I_FCU_EFIS1_WPT"), ("MIP_EFIS_VORD", "I_FCU_EFIS1_VORD"), ("MIP_EFIS_NDB", "I_FCU_EFIS1_NDB"),
            ("MIP_EFIS_ARPT", "I_FCU_EFIS1_ARPT"),
            ("MIP_MASTER_WARN", "I_MIP_MASTER_WARNING_CAPT"), ("MIP_MASTER_CAUT", "I_MIP_MASTER_CAUTION_CAPT"),
            ("MIP_TERR_ON_ND_1", "I_MIP_GPWS_TERRAIN_ON_ND_CAPT_L"), ("MIP_TERR_ON_ND_2", "I_MIP_GPWS_TERRAIN_ON_ND_FO_L"),
        };

        /// <summary>Pantalla del FCU i QNH (en aquest ordre a la lectura).</summary>
        static readonly string[] FenixDisplay =
        {
            "N_FCU_SPEED", "B_FCU_SPEED_MACH", "B_FCU_SPEED_DASHED", "I_FCU_SPEED_MANAGED",          // 0-3
            "N_FCU_HEADING", "B_FCU_HEADING_DASHED", "I_FCU_HEADING_MANAGED", "B_FCU_TRACK_FPA_MODE", // 4-7
            "N_FCU_ALTITUDE", "I_FCU_ALTITUDE_MANAGED", "N_FCU_VS", "B_FCU_VERTICALSPEED_DASHED",     // 8-11
            "B_FCU_POWER", "S_FCU_ALTITUDE_SCALE",                                                    // 12-13
            "N_FCU_EFIS1_BARO_HPA", "N_FCU_EFIS1_BARO_INCH", "S_FCU_EFIS1_BARO_MODE", "I_FCU_EFIS1_QNH", // 14-17
            "A_FC_THROTTLE_LEFT_INPUT", "A_FC_THROTTLE_RIGHT_INPUT",                                    // 18-19 palanques de gas
        };

        // pantalla+QNH+palanques, llums, selectors, llegendes, rellotge i data
        const int FenixReadCount = 20 + 17 + 7 + 14 + 4 + 4 + 1 + 2;
        /// <summary>
        /// FLAPS del Fenix (PROVISIONAL, per confirmar amb Pere): es llegeixen dues L-vars candidates i cada canvi
        /// s'apunta al registre (finestra de lectura en directe); la que es mogui és la bona. S'aplica S_FC_FLAPS (0 … 4).
        /// </summary>
        static readonly string[] FenixFlapsCand = { "S_FC_FLAPS", "A_FC_FLAPS" };
        private readonly double?[] _fxFlapsRaw = new double?[2];
        private int? _fxFlaps;
        /// <summary>
        /// SPEED BRAKE del Fenix (A_FC_SPEEDBRAKE, mesurat per Pere al simulador): 0 = armat (GND SPLRS), 1 = RET,
        /// 2 = 1/2, 3 = FULL, continu entre RET i FULL. App: posició 0 (RET) … 1 (FULL) → Fenix 1 + 2·posició.
        /// </summary>
        const string FenixSpeedBrake = "A_FC_SPEEDBRAKE";
        private double? _fxSpdBrkRaw;
        private (float pos, bool armed)? _fxSpdBrk;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct FenixGlareStruct
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FenixReadCount)]
            public double[] v;
        }

        // últim estat llegit del Fenix
        private panelAIR1000.FcuState? _fxFcu;
        private bool? _fxAltStep;
        private (bool p, int m, double v, bool hg)? _fxBaro;
        private readonly Dictionary<string, bool> _fxLights = new();
        private readonly Dictionary<string, int> _fxPos = new();
        private bool _fxData;
        private (float l, float r)? _fxThr;
        private readonly Dictionary<(string id, bool upper), bool> _fxLegends = new();
        private (string chr, string utc, string et)? _fxClock;
        private int _fxUtcBlink = -1;
        private (float accu, float left, float right)? _fxBrakes;   // indicador triple (milers de PSI)
        private bool? _fxGearDown;                                  // palanca del tren

        /// <summary>
        /// Palanca del Fenix (A_FC_THROTTLE_*_INPUT, mesurat al simulador: 1 REV FULL, 2 ralentí, 3 CL, 4 FLX MCT, 5 TO GA)
        /// → posició del quadrant de l'app (-1 REV FULL, 0 ralentí, 0,58 CL, 0,8 FLX MCT, 1 TO GA), lineal entre detents.
        /// </summary>
        private static float FenixLeverPos(double v)
        {
            double[] sim = { 1, 2, 3, 4, 5 };
            double[] app = { -1, 0, 0.58, 0.8, 1 };
            if (v <= sim[0]) return -1f;
            for (int i = 0; i < sim.Length - 1; i++)
                if (v <= sim[i + 1]) return (float)(app[i] + (v - sim[i]) / (sim[i + 1] - sim[i]) * (app[i + 1] - app[i]));
            return 1f;
        }

        /// <summary>Inversa de FenixLeverPos: posició de l'app (-1 … 1) → valor de la palanca del Fenix (1 … 5).</summary>
        private static double FenixLeverSim(float p)
        {
            double[] sim = { 1, 2, 3, 4, 5 };
            double[] app = { -1, 0, 0.58, 0.8, 1 };
            if (p <= app[0]) return 1;
            for (int i = 0; i < app.Length - 1; i++)
                if (p <= app[i + 1]) return sim[i] + (p - app[i]) / (app[i + 1] - app[i]) * (sim[i + 1] - sim[i]);
            return 5;
        }

        private void RegisterFenixGlareshield()
        {
            if (simconnect == null) return;
            var def = (DEFINITIONS)FenixReadDef;
            int n = 0;
            foreach (var name in FenixDisplay) { simconnect.AddToDataDefinition(def, "L:" + name, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            foreach (var l in FenixLights) { simconnect.AddToDataDefinition(def, "L:" + l.lvar, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            foreach (var p in FenixPos) { simconnect.AddToDataDefinition(def, "L:" + p.lvar, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            foreach (var l in FenixLegends) { simconnect.AddToDataDefinition(def, "L:" + l.lvar, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            foreach (var c in FenixClock) { simconnect.AddToDataDefinition(def, "L:" + c, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            // (agulles: només quan canvien 0,02 o més, és a dir, 20 PSI)
            foreach (var c in FenixGear) { simconnect.AddToDataDefinition(def, "L:" + c, "Number", SIMCONNECT_DATATYPE.FLOAT64, c.StartsWith("N_") ? 0.02f : 0f, SimConnect.SIMCONNECT_UNUSED); n++; }
            simconnect.AddToDataDefinition(def, "L:" + FenixSpeedBrake, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0.01f, SimConnect.SIMCONNECT_UNUSED); n++;
            foreach (var fc in FenixFlapsCand) { simconnect.AddToDataDefinition(def, "L:" + fc, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0.01f, SimConnect.SIMCONNECT_UNUSED); n++; }
            if (n != FenixReadCount) throw new InvalidOperationException("Fenix: el nombre de L-vars llegides no quadra amb FenixReadCount");
            simconnect.RegisterDataDefineStruct<FenixGlareStruct>(def);
            simconnect.RequestDataOnSimObject((REQUESTS)FenixReadReq, def, SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.VISUAL_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
        }

        /// <summary>Ordre del panell cap al Fenix. Retorna true si l'ha gestionada.</summary>
        private bool TrySendFenix(string id, string cmd)
        {
            if (simconnect == null || !IsA320Fenix()) return false;
            foreach (var c in FenixCmds)
                if (c.id == id && c.cmd == cmd)
                {
                    SendGenericRpn(c.press);
                    if (c.release != null)
                    {
                        string rel = c.release;
                        Task.Delay(150).ContinueWith(_ => { try { BeginInvoke(new Action(() => SendGenericRpn(rel))); } catch { } });
                    }
                    return true;
                }
            if (id == "MIP_THROTTLE")
            {
                // palanques arrossegades a l'app: "BOTH:v", "LEFT:v" o "RIGHT:v" (v = posició de l'app, -1 … 1)
                int c = cmd.IndexOf(':');
                if (c < 0 || !float.TryParse(cmd.AsSpan(c + 1), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float v)) return true;
                string sim = FenixLeverSim(v).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
                string which = cmd[..c];
                var rpn = new System.Text.StringBuilder();
                if (which != "RIGHT") rpn.Append($"{sim} (>L:A_FC_THROTTLE_LEFT_INPUT) ");
                if (which != "LEFT") rpn.Append($"{sim} (>L:A_FC_THROTTLE_RIGHT_INPUT)");
                SendGenericRpn(rpn.ToString().Trim());
                return true;
            }
            // FLAPS: posició 0 … 4 (provisional: S_FC_FLAPS)
            if (id == "PED_FLAPS")
            {
                if (cmd.StartsWith("POS:") && int.TryParse(cmd.AsSpan(4), out int fp))
                    SendGenericRpn($"{Math.Clamp(fp, 0, 4)} (>L:{FenixFlapsCand[0]})");
                return true;
            }
            // SPEED BRAKE: posició contínua (RET 1 … FULL 3), armar (0) i desarmar (1, RET)
            if (id == "PED_SPEEDBRAKE")
            {
                var ic = System.Globalization.CultureInfo.InvariantCulture;
                if (cmd.StartsWith("POS:") && float.TryParse(cmd.AsSpan(4), System.Globalization.NumberStyles.Float, ic, out float sp))
                    SendGenericRpn($"{(1 + 2 * Math.Clamp(sp, 0f, 1f)).ToString("0.###", ic)} (>L:{FenixSpeedBrake})");
                else if (cmd == "ARM") SendGenericRpn($"0 (>L:{FenixSpeedBrake})");
                else if (cmd == "DISARM") SendGenericRpn($"1 (>L:{FenixSpeedBrake})");
                return true;
            }
            if (cmd.StartsWith("POS:") && int.TryParse(cmd.AsSpan(4), out int pos))
                foreach (var p in FenixPos)
                    if (p.id == id)
                    {
                        if (p.toSim != null && (pos < 0 || pos >= p.toSim.Length)) return true;
                        _fxPos[id] = pos;
                        SendGenericRpn(Set(p.lvar, p.toSim == null ? pos : p.toSim[pos]));
                        return true;
                    }
            return false;
        }

        /// <summary>Lectura del Fenix (només arriba quan canvia algun valor).</summary>
        private void OnFenixData(double[] v)
        {
            // (es guarda sempre: la primera lectura pot arribar abans que l'app sàpiga que l'avió és el Fenix, i
            //  després només arriben canvis; ApplyFenixState ja comprova que sigui el Fenix abans d'aplicar res)
            if (_xp != null && _xp.A320Loaded) return;
            _fxData = true;
            bool powered = v[12] > 0.5, mach = v[1] > 0.5, trk = v[7] > 0.5;
            double spd = mach && v[0] > 2 ? v[0] / 100.0 : v[0];      // (per si el Mach arriba com a 78 en lloc de 0.78)
            double vs = v[10];
            double fpa = Math.Abs(vs) >= 10 ? vs / 10.0 : vs;         // (per si l'FPA arriba com a 8 en lloc de 0.8)
            _fxFcu = new panelAIR1000.FcuState(powered,
                spd, mach, v[2] > 0.5, v[3] > 0.5,
                v[4], v[5] > 0.5, v[6] > 0.5, trk,
                v[8], v[9] > 0.5,
                vs, fpa, v[11] > 0.5,
                FbwStyle: true);
            _fxAltStep = v[13] > 0.5;
            bool hpa = v[16] > 0.5;
            double inch = v[15] > 100 ? v[15] / 100.0 : v[15];       // (per si arriba com a 2992)
            _fxBaro = (powered, v[17] > 0.5 ? 1 : 0, hpa ? v[14] : inch, !hpa);
            _fxThr = (FenixLeverPos(v[18]), FenixLeverPos(v[19]));
            int k = FenixDisplay.Length;
            foreach (var (id, _) in FenixLights) _fxLights[id] = v[k++] > 0.5;
            foreach (var (id, _, toSim) in FenixPos)
            {
                int raw = (int)Math.Round(v[k++]);
                int idx = toSim == null ? raw : Array.IndexOf(toSim, raw);
                if (idx >= 0) _fxPos[id] = idx;
            }
            foreach (var (id, _, upper) in FenixLegends) _fxLegends[(id, upper)] = v[k++] > 0.5;
            // rellotge (mesurat al Fenix): els valors són el que surt a la pantalla, en decimal: cronòmetre MMSS, ET HHMM,
            // UTC HHMMSS (o la data MMDDAA); -1 = pantalla apagada
            double chr = v[k++], et = v[k++], utc = v[k++];
            int mode = (int)Math.Round(v[k++]);
            static string Pairs(double val, int n) { var s = ((long)Math.Round(Math.Max(0, val))).ToString(new string('0', n * 2)); s = s[^(n * 2)..]; return n == 2 ? $"{s[..2]}:{s[2..]}" : $"{s[..2]}:{s[2..4]}:{s[4..]}"; }
            string chrTxt = chr < 0 ? "" : Pairs(chr, 2);
            string etTxt = et < 0 ? "" : Pairs(et, 2);
            string utcTxt = utc < 0 ? "" : Pairs(utc, 3);
            // mode: -1 hora, -2 data (DATE premut), 0..4 posada en hora (SET), cada DATE passa al següent i fa pampallugues:
            // 0 minuts, 1 hores (HH:MM, sense segons), 2 any, 3 mes, 4 dia (data MM:DD:AA); després torna a 0
            int blink = mode switch { 0 => 1, 1 => 0, 2 => 2, 3 => 0, 4 => 1, _ => -1 };
            if ((mode == 0 || mode == 1) && utcTxt.Length == 8) utcTxt = utcTxt[..6] + "  ";
            _fxClock = (chrTxt, utcTxt, etTxt);
            _fxUtcBlink = blink;
            _fxBrakes = ((float)v[k++], (float)v[k++], (float)v[k++]);
            _fxGearDown = v[k++] > 0.5;
            // SPEED BRAKE: 0 armat, 1 RET … 3 FULL (s'apunta també al registre, per a la finestra de lectura en directe)
            double sbr = v[k++];
            _fxSpdBrk = sbr < 0.5 ? (0f, true) : ((float)Math.Clamp((sbr - 1) / 2, 0, 1), false);
            if (_fxSpdBrkRaw is not double old || Math.Abs(old - sbr) > 0.001)
            {
                _fxSpdBrkRaw = sbr;
                try { System.IO.File.AppendAllText("panelAIR_App_crash.txt", $"\r\n[FENIX SPEEDBRAKE {DateTime.Now:HH:mm:ss}] {FenixSpeedBrake} = {sbr.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); } catch { }
            }
            // FLAPS (provisional): les dues candidates s'apunten al registre quan canvien
            for (int fi = 0; fi < FenixFlapsCand.Length; fi++)
            {
                double fv = v[k++];
                if (_fxFlapsRaw[fi] is not double fo || Math.Abs(fo - fv) > 0.001)
                {
                    _fxFlapsRaw[fi] = fv;
                    try { System.IO.File.AppendAllText("panelAIR_App_crash.txt", $"\r\n[FENIX FLAPS {DateTime.Now:HH:mm:ss}] {FenixFlapsCand[fi]} = {fv.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); } catch { }
                }
                if (fi == 0) _fxFlaps = (int)Math.Round(Math.Clamp(fv, 0, 4));
            }
            ApplyFenixState();
        }

        private void ApplyFenixState()
        {
            var ov = _a320Overhead;
            if (ov == null || !_fxData || !IsA320Fenix() || (_xp != null && _xp.A320Loaded)) return;
            ov.SimDriven = true;
            if (_msReg != null) ov.SetRegistration(_msReg);   // matrícula (ATC ID de l'MSFS, igual que al FlyByWire)
            if (_fxFcu != null) ov.SetFcu(_fxFcu);
            if (_fxAltStep is bool st) ov.SetKnobRing("MIP_FCU_ALT", st);
            if (_fxBaro is var (bp, bm, bv, bh)) ov.SetBaro(bp, bm, bv, bh);
            if (_fxThr is var (tl, tr)) ov.SetThrottle(tl, tr);
            foreach (var kv in _fxLights) ov.SetLight(kv.Key, kv.Value);
            foreach (var kv in _fxPos) ov.SetPosition(kv.Key, kv.Value);
            foreach (var kv in _fxLegends)
                ov.SetLegend(kv.Key.id, kv.Key.upper ? (kv.Value ? 1f : 0f) : null, kv.Key.upper ? null : (kv.Value ? 1f : 0f));
            if (_fxClock is var (fc, fu, fe)) ov.SetClock(fc, fu, fe, _fxUtcBlink);
            if (_fxBrakes is var (ba, bl, br)) ov.SetBrakeGauge(ba, bl, br);
            if (_fxGearDown is bool gd) ov.SetGearLever(gd);
            if (_fxSpdBrk is var (sbp, sba)) ov.SetSpeedBrake(sbp, sba);
            if (_fxFlaps is int fxf) ov.SetFlaps(fxf);
        }

        // ── Pantalles pop-out del Fenix: totes es diuen "WASMINSTRUMENT" → s'assignen per ordre de sortida ──
        private readonly Dictionary<IntPtr, int> _fxSlots = new();   // finestra → vidre (0 PFD, 1 ND, 2 ECAM sup., 3 ECAM inf.)

        private static readonly string[] FenixScreenNames = { "PFD", "ND", "upper ECAM", "lower ECAM" };

        /// <summary>
        /// Les 4 finestres del Fenix es diuen igual: només es poden identificar per l'ordre en què l'usuari les treu.
        /// Per això l'app guia pas a pas quina toca ("Step 1 of 4: PFD") i cada finestra nova va al forat demanat.
        /// Si n'apareixen diverses de cop (p.ex. després de reiniciar el vol) no se sap quina és quina: l'app les tanca
        /// i torna a demanar-les una a una.
        /// </summary>
        /// <summary>
        /// Les 4 finestres del Fenix es diuen igual ("WASMINSTRUMENT"): l'app mira el contingut de cadascuna i reconeix
        /// quina pantalla és (veure ClassifyFenix), de manera que l'usuari les pot treure en qualsevol ordre.
        /// Una finestra encara negra o dubtosa es torna a mirar cada 2 s.
        /// </summary>
        /// <summary>
        /// Finestres pop-out del Fenix: "WASMINSTRUMENT" i, a l'MSFS 2024, també "FENIXWASMINSTRUMENT" (comprovat: el ND
        /// hi va sortir amb aquest títol i les altres tres amb el primer).
        /// </summary>
        private static List<IntPtr> FenixWindows()
        {
            var list = XPlaneWindows.FindAll("WASMINSTRUMENT", MsfsProcesses);
            foreach (var h in XPlaneWindows.FindAll("FENIXWASMINSTRUMENT", MsfsProcesses)) if (!list.Contains(h)) list.Add(h);
            return list;
        }

        private void PlaceFenixScreens()
        {
            var wins = FenixWindows();
            // Pantalles ja assentades (1 minut ben col·locades): no es fa res més que mirar si la llista de finestres
            // canvia (un pop-out nou o una finestra tancada); si canvia, es torna a activar tot.
            if (_fxSettled && _fxSettledTitle == _currentAircraftTitle)
            {
                if (wins.Count == _fxSlots.Count && wins.TrueForAll(_fxSlots.ContainsKey)) return;
                _fxSettled = false; _fxStableSince = DateTime.UtcNow;
            }
            foreach (var h in new List<IntPtr>(_fxSlots.Keys)) if (!wins.Contains(h)) _fxSlots.Remove(h);
            foreach (var h in new List<IntPtr>(_fxSeen.Keys)) if (!wins.Contains(h)) _fxSeen.Remove(h);
            _fxTentative.RemoveWhere(h => !wins.Contains(h));
            var now = DateTime.UtcNow;
            // ordre d'aparició: les noves de la de sota a la de dalt de l'ordre de Windows (la més antiga, primer)
            for (int i = wins.Count - 1; i >= 0; i--) if (!_fxSeen.ContainsKey(wins[i])) _fxSeen[wins[i]] = (now, _fxSeq++);
            _fxLastKind.Keys.Where(h => !wins.Contains(h)).ToList().ForEach(h => { _fxLastKind.Remove(h); _fxMismatch.Remove(h); });
            bool pending = wins.Exists(h => !_fxSlots.ContainsKey(h) || _fxTentative.Contains(h));
            // un cop les 4 pantalles fan 1 minut seguit ben col·locades, ja no es revisen (es torna a fer amb un altre avió)
            if (_fxSettledTitle != _currentAircraftTitle) { _fxSettled = false; _fxStableSince = now; _fxSettledTitle = _currentAircraftTitle; }
            bool stable = !pending && _fxSlots.Values.Count(s => s < 4) == 4 && _fxMismatch.Count == 0;
            if (!stable) _fxStableSince = now;
            else if ((now - _fxStableSince).TotalSeconds >= 60) _fxSettled = true;
            bool paused = now < _fxPauseUntil;   // s'està prement / animant la palanca del tren
            bool verify = !_fxSettled && !paused && (now - _fxLastVerify).TotalSeconds >= 6;   // revisió de les ja confirmades (cada 6 s)
            if ((pending && !paused && (now - _fxLastClassify).TotalSeconds >= 2) || verify)
            {
                _fxLastClassify = now;
                if (verify) _fxLastVerify = now;
                // 1) reconeixement per la imatge. "ND" és el cas per defecte (pantalla amb contingut sense res reconegut):
                //    p.ex. un ECAM inferior encara carregant-se. Per això una finestra classificada com a ND queda
                //    provisional, i les confirmades es tornen a revisar: si deixen de semblar el seu forat 2 vegades seguides
                //    (3 si el que semblen ara és un ND), surten del forat i es tornen a col·locar.
                foreach (var h in wins)
                {
                    bool tentative = _fxTentative.Contains(h);
                    bool placed = _fxSlots.TryGetValue(h, out int cur);
                    if (placed && !tentative && !verify) continue;
                    int kind;
                    using (var bmp = XPlaneWindows.Capture(h)) kind = bmp == null ? -1 : ClassifyFenix(bmp);
                    if (kind < 0) continue;
                    // ND segur (GS / TAS a dalt a l'esquerra): és el ND sense cap dubte, encara que abans s'hagués pres per MCDU
                    bool ndSure = kind == FxNdSure;
                    if (ndSure) kind = 1;
                    // una MCDU ja reconeguda no surt mai cap al forat del ND: "ND" només és el cas per defecte (p.ex. una
                    // pàgina plena de text on les files queden enganxades) i una MCDU no s'ha de veure mai al MFD. Només en
                    // surt si ara té una signatura clara d'una altra pantalla (PFD, ISIS, ECAM o el GS / TAS del ND).
                    if (!ndSure && kind == 1 && placed && (cur == FxMcduSlot || cur == FxMcdu2Slot)) kind = cur;
                    _fxLastKind[h] = kind;
                    // MCDU: dos forats (esquerra / dreta). Si ja és en un dels dos, hi continua; si no, va al primer lliure
                    if (kind == FxMcduSlot)
                    {
                        if (placed && (cur == FxMcduSlot || cur == FxMcdu2Slot)) kind = cur;
                        else
                        {
                            bool leftTaken = false;
                            foreach (var kv in _fxSlots) if (kv.Value == FxMcduSlot && kv.Key != h) leftTaken = true;
                            if (leftTaken) kind = FxMcdu2Slot;
                        }
                    }
                    if (placed && cur == kind) { if (kind != 1 || ndSure) _fxTentative.Remove(h); _fxMismatch.Remove(h); continue; }
                    if (placed && !tentative)
                    {
                        int n = _fxMismatch.GetValueOrDefault(h) + 1;
                        _fxMismatch[h] = n;
                        if (!ndSure && n < (kind == 1 ? 3 : 2)) continue;
                        _fxSlots.Remove(h); _fxMismatch.Remove(h);   // ja no sembla el seu forat: es torna a col·locar
                    }
                    IntPtr holder = IntPtr.Zero;
                    foreach (var kv in _fxSlots) if (kv.Value == kind && kv.Key != h) holder = kv.Key;
                    if (holder != IntPtr.Zero)
                    {
                        if (!_fxTentative.Contains(holder)) continue;                                   // ja hi ha una pantalla reconeguda
                        if (kind == 1 && _fxLastKind.GetValueOrDefault(holder, -1) == 1) continue;      // dos "ND": no es prenen el lloc
                        _fxSlots.Remove(holder); _fxTentative.Remove(holder);                           // la provisional cedeix el forat
                    }
                    _fxSlots[h] = kind;
                    if (kind == 1 && !ndSure) _fxTentative.Add(h); else _fxTentative.Remove(h);
                }
                // 2) si al cap de 6 s alguna no s'ha pogut reconèixer (p.ex. un PC on la captura surt negra), es col·loca
                //    per ordre d'aparició al primer forat lliure, seguint l'ordre que demana l'avís (PFD, ND, ECAM sup., ECAM inf.)
                var late = new List<IntPtr>();
                foreach (var h in wins) if (!_fxSlots.ContainsKey(h) && (now - _fxSeen[h].t).TotalSeconds >= 6) late.Add(h);
                late.Sort((a, b) => _fxSeen[a].seq.CompareTo(_fxSeen[b].seq));
                foreach (var h in late)
                {
                    int slot = 0;
                    while (slot < 4 && _fxSlots.ContainsValue(slot)) slot++;
                    if (slot >= 4) break;
                    _fxSlots[h] = slot; _fxTentative.Add(h);
                }
            }
            foreach (var kv in _fxSlots)
                try
                {
                    if (kv.Value == FxMcduSlot || kv.Value == FxMcdu2Slot) continue;   // les MCDU van al pedestal (PlaceFenixMcdu)
                    if (SimWindowRect(kv.Value, MsfsExtraHeight) is not System.Drawing.Rectangle wr) continue;
                    // ISIS: la finestra un 12,9% més gran que el forat, centrada (el seu marc negre queda amagat darrere l'app)
                    if (kv.Value == 4) wr.Inflate((int)Math.Round(wr.Width * 0.0645), (int)Math.Round(wr.Height * 0.0645));
                    XPlaneWindows.Place(kv.Key, wr, this.Handle);
                }
                catch { }
            // (l'avís només compta les 4 pantalles principals; l'ISIS és opcional)
            UpdatePopoutMessage(4 - _fxSlots.Values.Count(s => s < 4), 4, _msfsSince, ref _fxPopoutMsgShown, FenixPopoutText);
        }
        /// <summary>"Forats" de les MCDU del pedestal (5 esquerra, 6 dreta): fora dels 5 vidres del panell principal.</summary>
        private const int FxMcduSlot = 5, FxMcdu2Slot = 6;

        // (les pop-out de les MCDU no es col·loquen mai: la pantalla de les MCDU la dibuixa l'app amb el servidor GraphQL del Fenix;
        //  si l'usuari en fa una, es reconeix com a MCDU només perquè no vagi a parar al forat d'una altra pantalla)

        private DateTime _fxLastClassify = DateTime.MinValue, _fxLastVerify = DateTime.MinValue;
        private DateTime _fxPauseUntil = DateTime.MinValue, _fxStableSince = DateTime.MinValue;
        private bool _fxSettled;               // pantalles ben col·locades 1 minut seguit: ja no es revisen
        private string? _fxSettledTitle;       // avió amb què s'ha fet (un altre avió torna a activar la revisió)
        private readonly Dictionary<IntPtr, int> _fxLastKind = new();    // última classificació de cada finestra
        private readonly Dictionary<IntPtr, int> _fxMismatch = new();    // revisions seguides en què no sembla el seu forat
        private bool _fxPopoutMsgShown;
        private readonly Dictionary<IntPtr, (DateTime t, long seq)> _fxSeen = new();
        /// <summary>Finestres col·locades per ordre (sense reconèixer): es continuen mirant i poden cedir el forat.</summary>
        private readonly HashSet<IntPtr> _fxTentative = new();
        private long _fxSeq;
        private const string FenixPopoutText = "In Microsoft Flight Simulator, hold down the right **ALT** key on your keyboard and click the A320 displays one at a time, in this order: PFD, ND, upper ECAM, lower ECAM and the small STBY gauge. panelAIR will place them in the panel.";

        /// <summary>
        /// Quina pantalla del Fenix és (0 PFD, 1 ND, 2 ECAM superior, 3 ECAM inferior, 4 ISIS, 5 MCDU; -1 si no se sap, p.ex. negra):
        ///  - PFD: hi ha prou cel blau i terra marró (l'horitzó artificial);
        ///  - ECAM inferior (SD): línia horitzontal clara de gairebé tota l'amplada al 78-95% de l'alçada (franja TAT/SAT/GW);
        ///  - ECAM superior (EWD): línia horitzontal clara llarga al 55-78% (separació entre motors i missatges);
        ///  - ND: la resta (pantalla amb contingut però sense cap d'aquestes línies llargues).
        /// </summary>
        /// <summary>Resultat de ClassifyFenix: el ND segur (porta el GS / TAS a dalt a l'esquerra). Es col·loca com el ND (1).</summary>
        private const int FxNdSure = 9;

        /// <summary>
        /// El ND és l'única pantalla amb "GS 0 TAS ---" a dalt a l'esquerra (Pere): text blanc (GS), valor verd, text blanc
        /// (TAS), valor verd. Es busca una fila del racó de dalt a l'esquerra que, d'esquerra a dreta, passi per blanc → verd
        /// → blanc → verd. (Les files de la barra de títol de Windows, clares, no hi compten.)
        /// </summary>
        private static bool HasNdGsTas(byte[] px, int stride, int w, int h)
        {
            int xe = (int)(w * 0.45), ye = (int)(h * 0.22);
            int rows = 0;
            for (int y = 0; y < ye; y++)
            {
                int light = 0;
                for (int x = 0; x < xe; x++)
                {
                    int i = y * stride + x * 4;
                    if (Math.Min(px[i], Math.Min(px[i + 1], px[i + 2])) > 140) light++;
                }
                if (light > xe * 0.35) continue;   // barra de títol (fons clar)
                int stage = 0;                      // 0 → busca blanc, 1 → verd, 2 → blanc, 3 → verd, 4 = trobat
                for (int x = 0; x < xe && stage < 4; x++)
                {
                    int i = y * stride + x * 4;
                    int bl = px[i], gr = px[i + 1], rd = px[i + 2];
                    // (la lletra és fina i petita: a la captura el "blanc" surt gris, mesurat ~70-130 per canal)
                    bool white = Math.Min(rd, Math.Min(gr, bl)) > 55 && Math.Max(rd, Math.Max(gr, bl)) - Math.Min(rd, Math.Min(gr, bl)) < 45;
                    bool green = gr > 90 && gr > rd * 1.3f && gr > bl * 1.1f;
                    if ((stage % 2 == 0 && white) || (stage % 2 == 1 && green)) stage++;
                }
                if (stage >= 4 && ++rows >= 1) return true;
            }
            return false;
        }

        private static int ClassifyFenix(System.Drawing.Bitmap b)
        {
            int w = b.Width, h = b.Height;
            var data = b.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var px = new byte[data.Stride * h];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, px, 0, px.Length);
            int stride = data.Stride;
            b.UnlockBits(data);
            int x0 = (int)(w * 0.06), x1 = (int)(w * 0.94), y0 = (int)(h * 0.04), y1 = (int)(h * 0.96);
            long sky = 0, ground = 0, tot = 0, lit = 0;
            double ewd = 0, sd = 0;
            // files de text (MCDU): files de la imatge amb algun píxel encès (qualsevol color) i franges fosques entre elles
            var rowLit = new bool[y1 - y0];
            for (int y = y0; y < y1; y++)
            {
                int run = 0, maxRun = 0, litPx = 0;
                for (int x = x0; x < x1; x++)
                {
                    int i = y * stride + x * 4;
                    int bl = px[i], gr = px[i + 1], rd = px[i + 2];
                    if (Math.Max(rd, Math.Max(gr, bl)) > 150) litPx++;
                    if ((y & 1) == 0 && (x & 1) == 0)
                    {
                        tot++;
                        // cel: blau amb poc verd (el cian dels símbols del ND no hi compta);
                        // terra: marró fosc (l'ambre dels textos no hi compta)
                        // (per proporcions, no per intensitat: amb la pantalla poc brillant (p.ex. l'ISIS enfosquit: cel
                        //  20,52,76 i terra 35,24,11, mesurat) els colors són molt foscos però mantenen el to)
                        if (bl >= 40 && bl > rd * 2 && bl > gr * 1.2f) sky++;
                        else if (rd >= 25 && rd < 190 && rd > bl * 2 && gr > bl && gr < rd * 0.85f) ground++;
                        if (rd + gr + bl > 150 || (rd + gr + bl > 40 && (bl > rd * 2 || rd > bl * 2))) lit++;   // (també el cel / terra foscos)
                    }
                    if ((rd + gr + bl) / 3 > 110) { if (++run > maxRun) maxRun = run; } else run = 0;
                }
                rowLit[y - y0] = litPx >= 2;
                double f = (double)y / h, frac = (double)maxRun / (x1 - x0);
                if (f > 0.55 && f < 0.78 && frac > ewd) ewd = frac;
                if (f >= 0.78 && f < 0.95 && frac > sd) sd = frac;
            }
            if (tot == 0 || (double)lit / tot < 0.005) return -1;   // encara negra (sense corrent o carregant)
            // ISIS (pantalla d'emergència): també té cel i terra, però ocupen molt més que al PFD (el PFD, menys del 18%;
            // l'ISIS, ~30% amb el marc i la barra de títol de la finestra, més sense): no s'ha de confondre amb el PFD;
            // va al seu forat (4)
            if ((double)(sky + ground) / tot > 0.25 && (double)sky / tot > 0.04 && (double)ground / tot > 0.04) return 4;
            // PFD: l'horitzó artificial té cel i terra alhora (mesurat: uns 8,8% de cada; el ND, 0%)
            if ((double)(sky + ground) / tot > 0.03 && (double)sky / tot > 0.005 && (double)ground / tot > 0.005) return 0;
            if (sd >= 0.85) return 3;
            if (ewd >= 0.5) return 2;
            // ND segur: GS / TAS a dalt a l'esquerra (un cop descartats PFD, ISIS i ECAM, que també poden tenir blanc i verd)
            if (HasNdGsTas(px, stride, w, h)) return FxNdSure;
            // MCDU: només text, en files separades per franges fosques; cada bloc de files enceses fa com a molt l'alçada
            // d'una línia de text (mesurat a la pàgina MCDU MENU: 3 blocs, el més alt el 8,7% de l'alçada). El ND té arcs
            // que travessen moltes files seguides (blocs molt alts). Franja fosca: almenys l'1% de l'alçada.
            int gap = Math.Max(2, h / 100), dark = 0, blocks = 0, cur = 0, maxBlock = 0;
            bool inBlock = false;
            foreach (bool rl in rowLit)
            {
                if (rl)
                {
                    if (!inBlock) { blocks++; inBlock = true; cur = 0; }
                    cur += dark + 1; dark = 0;
                    if (cur > maxBlock) maxBlock = cur;
                }
                else if (inBlock && ++dark >= gap) { inBlock = false; dark = 0; }
            }
            // (fins al 20%: en pàgines plenes, una etiqueta petita i la seva línia de dades poden quedar enganxades en un
            //  sol bloc de ~2 files; els arcs del ND en fan molts més)
            if (blocks >= 2 && maxBlock <= h * 0.20) return FxMcduSlot;
            return 1;
        }

        private int NextFenixSlot()
        {
            int s = 0;
            while (s < 4 && _fxSlots.ContainsValue(s)) s++;
            return s;
        }

        private System.Windows.Forms.Form? _fxGuide;
        private System.Windows.Forms.Label? _fxGuideStep, _fxGuideText;
        private int _fxGuideShownStep = -1;
        private bool _fxGuideDismissed;   // l'usuari l'ha tancat: no es torna a obrir fins al pas següent

        private void ShowFenixGuide(int step, bool retry)
        {
            if (step >= 4) return;
            if (_fxGuide != null && !_fxGuide.IsDisposed && _fxGuideShownStep == step && !retry) return;
            if (_fxGuideShownStep != step || retry) _fxGuideDismissed = false;
            if (_fxGuideDismissed) return;
            _fxGuideShownStep = step;
            string stepText = $"Step {step + 1} of 4: pop out the {FenixScreenNames[step]}";
            string body = (retry ? "Several displays appeared at once, so panelAIR couldn't tell which is which and closed them. " : "") +
                          $"In Microsoft Flight Simulator, hold down the right ALT key on your keyboard and click the {FenixScreenNames[step]}. " +
                          "Pop out the displays one at a time, in this order: PFD, ND, upper ECAM, lower ECAM.";
            if (_fxGuide == null || _fxGuide.IsDisposed)
            {
                var form = new System.Windows.Forms.Form
                {
                    AutoScaleMode = System.Windows.Forms.AutoScaleMode.None,
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                    MaximizeBox = false, MinimizeBox = false, ControlBox = false,
                    Text = "panelAIR", ClientSize = new System.Drawing.Size(1500, 360),
                    BackColor = System.Drawing.Color.White,
                    StartPosition = System.Windows.Forms.FormStartPosition.Manual
                };
                form.Location = this.PointToScreen(new System.Drawing.Point((this.ClientSize.Width - form.Width) / 2, 400));
                _fxGuideStep = new System.Windows.Forms.Label
                {
                    Font = new System.Drawing.Font("Segoe UI", 51f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel),
                    ForeColor = System.Drawing.Color.FromArgb(22, 32, 42), TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    Location = new System.Drawing.Point(40, 30), Size = new System.Drawing.Size(1420, 70)
                };
                _fxGuideText = new System.Windows.Forms.Label
                {
                    Font = new System.Drawing.Font("Segoe UI", 30f, System.Drawing.GraphicsUnit.Pixel),
                    ForeColor = System.Drawing.Color.FromArgb(102, 114, 127), TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    Location = new System.Drawing.Point(40, 110), Size = new System.Drawing.Size(1420, 150)
                };
                var ok = new System.Windows.Forms.Button
                {
                    Text = "Got it", Size = new System.Drawing.Size(320, 64),
                    Font = new System.Drawing.Font("Segoe UI", 30f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel),
                    BackColor = System.Drawing.Color.FromArgb(22, 32, 42), ForeColor = System.Drawing.Color.White,
                    FlatStyle = System.Windows.Forms.FlatStyle.Flat
                };
                ok.FlatAppearance.BorderSize = 0;
                ok.Location = new System.Drawing.Point((1500 - 320) / 2, 360 - 64 - 24);
                ok.Click += (s, e) => { _fxGuideDismissed = true; form.Close(); };
                form.Controls.Add(_fxGuideStep); form.Controls.Add(_fxGuideText); form.Controls.Add(ok);
                _fxGuide = form;
                form.Show(this);
            }
            _fxGuideStep!.Text = stepText;
            _fxGuideText!.Text = body;
        }

        private void CloseFenixGuide()
        {
            if (_fxGuide != null && !_fxGuide.IsDisposed) _fxGuide.Close();
            _fxGuideShownStep = -1; _fxGuideDismissed = false;
        }

        private void CloseFenixScreens()
        {
            // les pop-out de les MCDU no són de l'app: es deixen obertes (i també les que encara no s'han reconegut, que
            // poden ser MCDU)
            foreach (var h in FenixWindows())
            {
                if (!_fxSlots.TryGetValue(h, out int slot) || slot == FxMcduSlot || slot == FxMcdu2Slot) continue;
                XPlaneWindows.Close(h);
            }
        }
    }
}
