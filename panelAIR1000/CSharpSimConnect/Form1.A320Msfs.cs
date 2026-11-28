using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.FlightSimulator.SimConnect;

namespace SimConnect3
{
    /// <summary>
    /// A320 de FlyByWire a l'MSFS: glareshield (FCU, EFIS del capità i panell de warnings), en els dos sentits.
    /// Noms de l'API oficial del FlyByWire (docs.flybywiresim.com, A32NX flight deck API).
    ///  - Ordres: esdeveniments propis "A32NX.*" (via SimConnect), escriptura de LVARs i un H: event (CHRONO).
    ///  - Estat: una sola lectura de LVARs que només arriba quan canvia (pantalla del FCU, llums, selectors).
    /// El QNH del capità continua al seu codi propi (Form1.cs).
    /// </summary>
    public partial class frmMain
    {
        // identificadors propis (fora del rang dels enums de l'app): SimConnect accepta qualsevol valor d'enum
        enum A320MsfsEvent : uint { Base = 7000 }
        const A320MsfsEvent FbwThr1 = (A320MsfsEvent)7090, FbwThr2 = (A320MsfsEvent)7091;
        const uint MsfsA320ReadDef = 7100, MsfsA320ReadReq = 7100, MsfsA320WriteDefBase = 7200;

        /// <summary>Element del panell + acció → esdeveniment del FlyByWire.</summary>
        static readonly (string id, string cmd, string ev)[] MsfsA320Events =
        {
            ("MIP_FCU_SPD", "INC", "A32NX.FCU_SPD_INC"), ("MIP_FCU_SPD", "DEC", "A32NX.FCU_SPD_DEC"),
            ("MIP_FCU_SPD", "PUSH", "A32NX.FCU_SPD_PUSH"), ("MIP_FCU_SPD", "PULL", "A32NX.FCU_SPD_PULL"),
            ("MIP_FCU_HDG", "INC", "A32NX.FCU_HDG_INC"), ("MIP_FCU_HDG", "DEC", "A32NX.FCU_HDG_DEC"),
            ("MIP_FCU_HDG", "PUSH", "A32NX.FCU_HDG_PUSH"), ("MIP_FCU_HDG", "PULL", "A32NX.FCU_HDG_PULL"),
            ("MIP_FCU_ALT", "INC", "A32NX.FCU_ALT_INC"), ("MIP_FCU_ALT", "DEC", "A32NX.FCU_ALT_DEC"),
            ("MIP_FCU_ALT", "PUSH", "A32NX.FCU_ALT_PUSH"), ("MIP_FCU_ALT", "PULL", "A32NX.FCU_ALT_PULL"),
            ("MIP_FCU_VS", "INC", "A32NX.FCU_VS_INC"), ("MIP_FCU_VS", "DEC", "A32NX.FCU_VS_DEC"),
            ("MIP_FCU_VS", "PUSH", "A32NX.FCU_VS_PUSH"), ("MIP_FCU_VS", "PULL", "A32NX.FCU_VS_PULL"),
            ("MIP_FCU_SPD_MACH", "PUSH", "A32NX.FCU_SPD_MACH_TOGGLE_PUSH"),
            ("MIP_FCU_HDGTRK_VSFPA", "PUSH", "A32NX.FCU_TRK_FPA_TOGGLE_PUSH"),
            ("MIP_FCU_METRIC_ALT", "PUSH", "A32NX.FCU_METRIC_ALT_TOGGLE_PUSH"),
            ("MIP_FCU_AP1", "PUSH", "A32NX.FCU_AP_1_PUSH"), ("MIP_FCU_AP2", "PUSH", "A32NX.FCU_AP_2_PUSH"),
            ("MIP_FCU_ATHR", "PUSH", "A32NX.FCU_ATHR_PUSH"), ("MIP_FCU_LOC", "PUSH", "A32NX.FCU_LOC_PUSH"),
            ("MIP_FCU_APPR", "PUSH", "A32NX.FCU_APPR_PUSH"), ("MIP_FCU_EXPED", "PUSH", "A32NX.FCU_EXPED_PUSH"),
            ("MIP_EFIS_FD", "PUSH", "A32NX.FCU_EFIS_L_FD_PUSH"), ("MIP_EFIS_LS", "PUSH", "A32NX.FCU_EFIS_L_LS_PUSH"),
            ("MIP_EFIS_CSTR", "PUSH", "A32NX.FCU_EFIS_L_CSTR_PUSH"), ("MIP_EFIS_WPT", "PUSH", "A32NX.FCU_EFIS_L_WPT_PUSH"),
            ("MIP_EFIS_VORD", "PUSH", "A32NX.FCU_EFIS_L_VORD_PUSH"), ("MIP_EFIS_NDB", "PUSH", "A32NX.FCU_EFIS_L_NDB_PUSH"),
            ("MIP_EFIS_ARPT", "PUSH", "A32NX.FCU_EFIS_L_ARPT_PUSH"),
        };

        /// <summary>Element + acció → escriure un valor a una LVAR (botons sense esdeveniment propi).</summary>
        static readonly (string id, string cmd, string lvar, double value)[] MsfsA320Writes =
        {
            ("MIP_FCU_ALT", "STEP100", "A32NX_FCU_ALT_INCREMENT_1000", 0),
            ("MIP_FCU_ALT", "STEP1000", "A32NX_FCU_ALT_INCREMENT_1000", 1),
            ("MIP_MASTER_WARN", "PUSH", "A32NX_MASTER_WARNING", 0),    // prémer apaga el llum
            ("MIP_MASTER_CAUT", "PUSH", "A32NX_MASTER_CAUTION", 0),
        };

        /// <summary>
        /// Selectors: element → LVAR, amb la conversió posició de l'app → valor del FlyByWire.
        /// Les palanques ADF/VOR de l'app van 0 ADF, 1 OFF, 2 VOR; el FlyByWire 0 OFF, 1 ADF, 2 VOR.
        /// </summary>
        static readonly (string id, string lvar, int[] toSim)[] MsfsA320Pos =
        {
            ("MIP_ND_MODE", "A32NX_FCU_EFIS_L_EFIS_MODE", new[] { 0, 1, 2, 3, 4 }),      // LS, VOR, NAV, ARC, PLAN
            ("MIP_ND_RANGE", "A32NX_FCU_EFIS_L_EFIS_RANGE", new[] { 0, 1, 2, 3, 4, 5 }), // 10 … 320 NM
            ("MIP_EFIS_ADFVOR1", "A32NX_FCU_EFIS_L_NAVAID_1_MODE", new[] { 1, 0, 2 }),
            ("MIP_EFIS_ADFVOR2", "A32NX_FCU_EFIS_L_NAVAID_2_MODE", new[] { 1, 0, 2 }),
        };

        /// <summary>Llums de tecles i polsadors: element → LVAR (0/1).</summary>
        static readonly (string id, string lvar)[] MsfsA320Lights =
        {
            ("MIP_FCU_AP1", "A32NX_FCU_AP_1_LIGHT_ON"), ("MIP_FCU_AP2", "A32NX_FCU_AP_2_LIGHT_ON"),
            ("MIP_FCU_ATHR", "A32NX_FCU_ATHR_LIGHT_ON"), ("MIP_FCU_LOC", "A32NX_FCU_LOC_LIGHT_ON"),
            ("MIP_FCU_APPR", "A32NX_FCU_APPR_LIGHT_ON"), ("MIP_FCU_EXPED", "A32NX_FCU_EXPED_LIGHT_ON"),
            ("MIP_EFIS_FD", "A32NX_FCU_EFIS_L_FD_LIGHT_ON"), ("MIP_EFIS_LS", "A32NX_FCU_EFIS_L_LS_LIGHT_ON"),
            ("MIP_EFIS_CSTR", "A32NX_FCU_EFIS_L_CSTR_LIGHT_ON"), ("MIP_EFIS_WPT", "A32NX_FCU_EFIS_L_WPT_LIGHT_ON"),
            ("MIP_EFIS_VORD", "A32NX_FCU_EFIS_L_VORD_LIGHT_ON"), ("MIP_EFIS_NDB", "A32NX_FCU_EFIS_L_NDB_LIGHT_ON"),
            ("MIP_EFIS_ARPT", "A32NX_FCU_EFIS_L_ARPT_LIGHT_ON"),
            ("MIP_MASTER_WARN", "A32NX_MASTER_WARNING"), ("MIP_MASTER_CAUT", "A32NX_MASTER_CAUTION"),
            // TERR ON ND (capità / primer oficial): la mateixa LVAR és el llum i l'ordre (es canvia)
            ("MIP_TERR_ON_ND_1", "A32NX_EFIS_TERR_L_ACTIVE"), ("MIP_TERR_ON_ND_2", "A32NX_EFIS_TERR_R_ACTIVE"),
        };

        /// <summary>Pantalla del FCU (en aquest ordre a la lectura).</summary>
        static readonly string[] MsfsA320Display =
        {
            "A32NX_FCU_AFS_DISPLAY_SPD_MACH_VALUE", "A32NX_FCU_AFS_DISPLAY_MACH_MODE",
            "A32NX_FCU_AFS_DISPLAY_SPD_MACH_DASHES", "A32NX_FCU_AFS_DISPLAY_SPD_MACH_MANAGED",
            "A32NX_FCU_AFS_DISPLAY_HDG_TRK_VALUE", "A32NX_FCU_AFS_DISPLAY_HDG_TRK_DASHES",
            "A32NX_FCU_AFS_DISPLAY_HDG_TRK_MANAGED", "A32NX_FCU_AFS_DISPLAY_TRK_FPA_MODE",
            "A32NX_FCU_AFS_DISPLAY_ALT_VALUE", "A32NX_FCU_AFS_DISPLAY_LVL_CH_MANAGED",
            "A32NX_FCU_AFS_DISPLAY_VS_FPA_VALUE", "A32NX_FCU_AFS_DISPLAY_VS_FPA_DASHES",
            "A32NX_ELEC_DC_ESS_BUS_IS_POWERED",                                    // corrent (pantalla encesa / apagada)
            "A32NX_FCU_ALT_INCREMENT_1000",                                        // selector ALT 100 (0) / 1000 (1)
            "A32NX_AUTOTHRUST_TLA:1", "A32NX_AUTOTHRUST_TLA:2",                    // angle de les palanques de gas (graus)
            // configuració de l'eix de gas a l'EFB (rangs de cada detent, -1 … 1) per poder enviar posicions
            "A32NX_THROTTLE_MAPPING_USE_REVERSE_ON_AXIS:1",
            "A32NX_THROTTLE_MAPPING_REVERSE_LOW:1", "A32NX_THROTTLE_MAPPING_REVERSE_HIGH:1",
            "A32NX_THROTTLE_MAPPING_REVERSE_IDLE_LOW:1", "A32NX_THROTTLE_MAPPING_REVERSE_IDLE_HIGH:1",
            "A32NX_THROTTLE_MAPPING_IDLE_LOW:1", "A32NX_THROTTLE_MAPPING_IDLE_HIGH:1",
            "A32NX_THROTTLE_MAPPING_CLIMB_LOW:1", "A32NX_THROTTLE_MAPPING_CLIMB_HIGH:1",
            "A32NX_THROTTLE_MAPPING_FLEXMCT_LOW:1", "A32NX_THROTTLE_MAPPING_FLEXMCT_HIGH:1",
            "A32NX_THROTTLE_MAPPING_TOGA_LOW:1", "A32NX_THROTTLE_MAPPING_TOGA_HIGH:1",
            // 29-34 indicadors del tren (LGCIU 1): blocat avall (triangle verd) / desblocat (UNLK)
            "A32NX_LGCIU_1_LEFT_GEAR_DOWNLOCKED", "A32NX_LGCIU_1_LEFT_GEAR_UNLOCKED",
            "A32NX_LGCIU_1_NOSE_GEAR_DOWNLOCKED", "A32NX_LGCIU_1_NOSE_GEAR_UNLOCKED",
            "A32NX_LGCIU_1_RIGHT_GEAR_DOWNLOCKED", "A32NX_LGCIU_1_RIGHT_GEAR_UNLOCKED",
            // 35-38 autofre (0 res, 1 LO, 2 MED, 3 MAX; DECEL), polsador del ventilador (llum ON: el ventilador en si
            // només gira amb corrent AC; comprovat) i frens calents
            "A32NX_AUTOBRAKES_ARMED_MODE", "A32NX_AUTOBRAKES_DECEL_LIGHT", "A32NX_BRAKE_FAN_BTN_PRESSED", "A32NX_BRAKES_HOT",
            // 39-41 rellotge (mesurat al FBW): cronòmetre i ET en segons (-1 = pantalla apagada), selector RUN/STP/RST
            "A32NX_CHRONO_ELAPSED_TIME", "A32NX_CHRONO_ET_ELAPSED_TIME", "A32NX_CHRONO_ET_SWITCH_POS",
            // 42-45 indicador triple (frens alternatius i acumulador, en PSI) i palanca del tren (1 = avall)
            "A32NX_HYD_BRAKE_ALTN_ACC_PRESS", "A32NX_HYD_BRAKE_ALTN_LEFT_PRESS", "A32NX_HYD_BRAKE_ALTN_RIGHT_PRESS",
            // 45 petició de la palanca del tren (el que es mou a la VC); si no existeix no canvia mai i mana GEAR HANDLE POSITION
            "A32NX_GEAR_LEVER_POSITION_REQUEST",
            // 46 posició animada de la palanca 3D (0 amunt … 1 avall): diu si la palanca de la VC s'ha mogut de debò
            "A32NX_GEAR_HANDLE_POSITION",
            // 47-48 SPEED BRAKE (API del FBW): posició de la palanca (0 RET … 1 FULL) i GND SPLRS armat (0/1)
            "A32NX_SPOILERS_HANDLE_POSITION", "A32NX_SPOILERS_ARMED",
        };
        /// <summary>Variables de simulació (amb unitat) que es llegeixen després dels selectors: A/SKID (1 = ON) i hora UTC (s).</summary>
        static readonly (string name, string unit)[] MsfsA320SimVars =
        {
            ("ANTISKID BRAKES ACTIVE", "Bool"), ("ZULU TIME", "Seconds"),   // (amb el prefix "E:" SimConnect torna 0)
            ("ZULU MONTH OF YEAR", "Number"), ("ZULU DAY OF MONTH", "Number"), ("ZULU YEAR", "Number"),   // data del DATE
            ("GEAR HANDLE POSITION", "Bool"),   // maneta del tren (canvia de cop; la LVAR del FBW és l'animació 3D)
        };

        // lectura: pantalla + llums + selectors + variables de simulació, tot en un sol bloc de nombres
        const int MsfsA320ReadCount = 49 + 17 + 4 + 6;

        /// <summary>
        /// DATE del rellotge al FBW: el FBW canvia entre hora i data a cada ordre i no té cap LVAR que ho digui. L'app envia
        /// l'ordre en prémer i en deixar anar (la data només es veu mentre es manté premut) i mostra ella mateixa la data.
        /// </summary>
        private bool _msDateHeld;
        private (int secs, int month, int day, int year) _msZulu;
        private string MsUtcText() => _msDateHeld
            ? $"{_msZulu.month:00}:{_msZulu.day:00}:{_msZulu.year % 100:00}"
            : $"{_msZulu.secs / 3600:00}:{_msZulu.secs / 60 % 60:00}:{_msZulu.secs % 60:00}";

        /// <summary>Configuració de l'eix del FBW: reversa a l'eix i rang (baix, alt) de cada detent (REV, REV IDLE, IDLE, CL, FLX, TOGA).</summary>
        private bool _fbwRevOnAxis = true;
        private readonly (double lo, double hi)[] _fbwRange =
            { (-1, -0.95), (-0.85, -0.75), (-0.55, -0.45), (-0.05, 0.05), (0.45, 0.55), (0.95, 1) };
        /// <summary>TLA (graus) de cada detent del FBW, en el mateix ordre que _fbwRange.</summary>
        static readonly double[] FbwDetTla = { -20, -6, 0, 25, 35, 45 };

        /// <summary>
        /// Posició de l'app (-1 … 1) → valor de l'eix del FBW (-1 … 1) segons la configuració de l'EFB: a un detent,
        /// el centre del seu rang; entre dos detents, interpolat entre la vora alta d'un i la baixa del següent.
        /// </summary>
        private double FbwAxisFor(float appPos)
        {
            double tla = FbwAppToTla(appPos);
            int first = _fbwRevOnAxis ? 0 : 2;        // sense reversa a l'eix, l'eix comença al ralentí
            if (tla <= FbwDetTla[first]) return Mid(first);
            for (int i = first; i < FbwDetTla.Length - 1; i++)
            {
                if (Math.Abs(tla - FbwDetTla[i]) < 0.3) return Mid(i);
                if (Math.Abs(tla - FbwDetTla[i + 1]) < 0.3) return Mid(i + 1);
                if (tla < FbwDetTla[i + 1])
                {
                    double t = (tla - FbwDetTla[i]) / (FbwDetTla[i + 1] - FbwDetTla[i]);
                    return _fbwRange[i].hi + t * (_fbwRange[i + 1].lo - _fbwRange[i].hi);
                }
            }
            return Mid(FbwDetTla.Length - 1);
            double Mid(int i) => (_fbwRange[i].lo + _fbwRange[i].hi) / 2;
        }
        private static double FbwAppToTla(float p)
        {
            if (p <= FbwApp[0]) return FbwTla[0];
            for (int i = 0; i < FbwApp.Length - 1; i++)
                if (p <= FbwApp[i + 1]) return FbwTla[i] + (p - FbwApp[i]) / (FbwApp[i + 1] - FbwApp[i]) * (FbwTla[i + 1] - FbwTla[i]);
            return FbwTla[^1];
        }

        /// <summary>
        /// Angle de la palanca del FlyByWire (TLA, graus: -20 REV FULL, 0 ralentí, 25 CL, 35 FLX MCT, 45 TO GA)
        /// ↔ posició del quadrant de l'app (-1, 0, 0,58, 0,8, 1), lineal entre detents.
        /// </summary>
        static readonly double[] FbwTla = { -20, 0, 25, 35, 45 };
        static readonly double[] FbwApp = { -1, 0, 0.58, 0.8, 1 };
        private static float FbwLeverPos(double tla)
        {
            if (tla <= FbwTla[0]) return -1f;
            for (int i = 0; i < FbwTla.Length - 1; i++)
                if (tla <= FbwTla[i + 1]) return (float)(FbwApp[i] + (tla - FbwTla[i]) / (FbwTla[i + 1] - FbwTla[i]) * (FbwApp[i + 1] - FbwApp[i]));
            return 1f;
        }
        private (float l, float r)? _msThr;
        private readonly Dictionary<(string id, bool upper), bool> _msLegends = new();   // tren, autofre, BRK FAN
        private (string chr, string utc, string et)? _msClock;                           // pantalles del rellotge
        private (float accu, float left, float right)? _msBrakes;                        // indicador triple (milers de PSI)
        private bool? _msGearDown;                                                       // palanca del tren
        private bool? _msGearAnimDown;                                                   // palanca 3D (amb marge del 5%)
        private (float pos, bool armed)? _msSpdBrk;                                      // SPEED BRAKE
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct A320GlareStruct
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MsfsA320ReadCount)]
            public double[] v;
        }

        // últim estat llegit (per aplicar-lo si el panell es crea després)
        private panelAIR1000.FcuState? _msFcu;
        private bool? _msAltStep;
        private readonly Dictionary<string, bool> _msLights = new();
        private readonly Dictionary<string, int> _msPos = new();
        private bool _msA320Data;   // ha arribat alguna lectura del glareshield del FlyByWire

        /// <summary>Registra esdeveniments, escriptures i la lectura del glareshield (es crida en connectar amb l'MSFS).</summary>
        private void RegisterMsfsA320Glareshield()
        {
            if (simconnect == null) return;
            for (int i = 0; i < MsfsA320Events.Length; i++)
                simconnect.MapClientEventToSimEvent((A320MsfsEvent)(7000 + i), MsfsA320Events[i].ev);
            // eix de gas de cada motor (palanques arrossegades a l'app)
            simconnect.MapClientEventToSimEvent(FbwThr1, "THROTTLE1_AXIS_SET_EX1");
            simconnect.MapClientEventToSimEvent(FbwThr2, "THROTTLE2_AXIS_SET_EX1");

            // una definició d'escriptura per a cada LVAR que es pot escriure
            var writeLvars = new List<string>();
            foreach (var w in MsfsA320Writes) if (!writeLvars.Contains(w.lvar)) writeLvars.Add(w.lvar);
            foreach (var p in MsfsA320Pos) if (!writeLvars.Contains(p.lvar)) writeLvars.Add(p.lvar);
            _msWriteDefs.Clear();
            for (int i = 0; i < writeLvars.Count; i++)
            {
                var def = (DEFINITIONS)(MsfsA320WriteDefBase + i);
                simconnect.AddToDataDefinition(def, "L:" + writeLvars[i], "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<A320BaroUnitStruct>(def);
                _msWriteDefs[writeLvars[i]] = def;
            }

            // lectura (només quan canvia)
            var readDef = (DEFINITIONS)MsfsA320ReadDef;
            int n = 0;
            // (els comptadors del rellotge avancen a cada fotograma: només es reenvien quan canvien mig segon o més)
            // (pressions: només quan canvien 20 PSI o més)
            foreach (var name in MsfsA320Display) { simconnect.AddToDataDefinition(readDef, "L:" + name, "Number", SIMCONNECT_DATATYPE.FLOAT64, name.Contains("ELAPSED_TIME") ? 0.5f : name.Contains("_PRESS") ? 20f : 0f, SimConnect.SIMCONNECT_UNUSED); n++; }
            foreach (var l in MsfsA320Lights) { simconnect.AddToDataDefinition(readDef, "L:" + l.lvar, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            foreach (var p in MsfsA320Pos) { simconnect.AddToDataDefinition(readDef, "L:" + p.lvar, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            foreach (var (name, unit) in MsfsA320SimVars) { simconnect.AddToDataDefinition(readDef, name, unit, SIMCONNECT_DATATYPE.FLOAT64, name.Contains("ZULU") ? 0.5f : 0f, SimConnect.SIMCONNECT_UNUSED); n++; }
            if (n != MsfsA320ReadCount) throw new InvalidOperationException("A320 MSFS: el nombre de LVARs llegides no quadra amb MsfsA320ReadCount");
            simconnect.RegisterDataDefineStruct<A320GlareStruct>(readDef);
            simconnect.RequestDataOnSimObject((REQUESTS)MsfsA320ReadReq, readDef, SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.VISUAL_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);

            // matrícula de l'avió (FlyByWire i Fenix): variable estàndard ATC ID, un cop per segon i només si canvia
            var regDef = (DEFINITIONS)MsfsRegDef;
            simconnect.AddToDataDefinition(regDef, "ATC ID", null, SIMCONNECT_DATATYPE.STRING64, 0, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<MsfsRegStruct>(regDef);
            simconnect.RequestDataOnSimObject((REQUESTS)MsfsRegReq, regDef, SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
        }

        const uint MsfsRegDef = 7110, MsfsRegReq = 7110;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct MsfsRegStruct { [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string id; }
        private string? _msReg;   // matrícula llegida de l'MSFS

        /// <summary>Matrícula llegida de l'MSFS: es posa a la placa si l'A320 és de l'MSFS (no si hi ha el ToLiss).</summary>
        private void OnMsfsReg(string reg)
        {
            _msReg = reg.Trim();
            if (IsA320Msfs() && (_xp == null || !_xp.A320Loaded)) _a320Overhead?.SetRegistration(_msReg);
        }
        private readonly Dictionary<string, DEFINITIONS> _msWriteDefs = new();

        /// <summary>Ordre del panell cap al FlyByWire. Retorna true si l'ha gestionada.</summary>
        private bool TrySendMsfsA320(string id, string cmd)
        {
            if (simconnect == null || !IsA320FBW()) return false;
            try
            {
                for (int i = 0; i < MsfsA320Events.Length; i++)
                    if (MsfsA320Events[i].id == id && MsfsA320Events[i].cmd == cmd)
                    {
                        simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (A320MsfsEvent)(7000 + i), 0,
                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        return true;
                    }
                foreach (var w in MsfsA320Writes)
                    if (w.id == id && w.cmd == cmd) { WriteMsfsLvar(w.lvar, w.value); return true; }
                if (cmd.StartsWith("POS:") && int.TryParse(cmd.AsSpan(4), out int pos))
                    foreach (var p in MsfsA320Pos)
                        if (p.id == id && pos >= 0 && pos < p.toSim.Length)
                        {
                            _msPos[id] = pos;
                            WriteMsfsLvar(p.lvar, p.toSim[pos]);
                            return true;
                        }
                if (id == "MIP_CHRONO" && cmd == "PUSH") { SendGenericRpn("(>H:A32NX_EFIS_L_CHRONO_PUSHED)"); return true; }
                // rellotge del panell principal (el selector GPS del FBW no fa res: no s'envia)
                if (id == "MIP_CLK_CHR" && cmd == "PUSH") { SendGenericRpn("(>H:A32NX_CHRONO_TOGGLE)"); return true; }
                if (id == "MIP_CLK_RST" && cmd == "PUSH") { SendGenericRpn("(>H:A32NX_CHRONO_RST)"); return true; }
                if (id == "MIP_CLK_DATE")
                {
                    // el FBW canvia hora ↔ data a cada ordre: una en prémer i una en deixar anar
                    bool down = cmd == "DOWN";
                    if ((down || cmd == "UP") && down != _msDateHeld)
                    {
                        _msDateHeld = down;
                        SendGenericRpn("(>H:A32NX_CHRONO_DATE)");
                        if (_msClock is var (dc, _, de)) { _msClock = (dc, MsUtcText(), de); _a320Overhead?.SetClock(dc, _msClock.Value.utc, de); }
                    }
                    return true;
                }
                // TERR ON ND: es canvia la LVAR (llum i estat alhora)
                if ((id == "MIP_TERR_ON_ND_1" || id == "MIP_TERR_ON_ND_2") && cmd == "PUSH")
                {
                    string tv = id.EndsWith("1") ? "A32NX_EFIS_TERR_L_ACTIVE" : "A32NX_EFIS_TERR_R_ACTIVE";
                    SendGenericRpn($"(L:{tv}, bool) ! (>L:{tv})");
                    return true;
                }
                if (id == "MIP_CLK_RUN" && cmd.StartsWith("POS:") && int.TryParse(cmd.AsSpan(4), out int etp) && etp >= 0 && etp <= 2)
                {
                    _msPos[id] = etp;
                    SendGenericRpn($"{etp} (>L:A32NX_CHRONO_ET_SWITCH_POS) (>H:A32NX_CHRONO_ET_POS_CHANGED)");
                    return true;
                }
                // A/SKID & N/W STRG: app 0 = ON; el FBW fa servir la variable de simulació i només té l'ordre de canviar
                if (id == "MIP_ASKID_NWSTRG" && cmd.StartsWith("POS:") && int.TryParse(cmd.AsSpan(4), out int asp))
                {
                    _msPos[id] = asp;
                    SendGenericRpn($"(A:ANTISKID BRAKES ACTIVE, Bool) {(asp == 0 ? 1 : 0)} != if{{ 0 (>K:ANTISKID_BRAKES_TOGGLE) }}");
                    return true;
                }
                // autofre: prémer i deixar anar el botó (LVAR del FBW); BRK FAN: canvia l'estat del polsador
                string? abLvar = id switch
                {
                    "MIP_AUTOBRK_LO" => "A32NX_OVHD_AUTOBRK_LOW_ON_IS_PRESSED",
                    "MIP_AUTOBRK_MED" => "A32NX_OVHD_AUTOBRK_MED_ON_IS_PRESSED",
                    "MIP_AUTOBRK_MAX" => "A32NX_OVHD_AUTOBRK_MAX_ON_IS_PRESSED",
                    _ => null
                };
                if (abLvar != null && cmd == "PUSH")
                {
                    SendGenericRpn($"1 (>L:{abLvar})");
                    System.Threading.Tasks.Task.Delay(150).ContinueWith(_ => { try { BeginInvoke(new Action(() => SendGenericRpn($"0 (>L:{abLvar})"))); } catch { } });
                    return true;
                }
                // ISIS (codis dels presets de HubHop del FBW): BUGS i LS canvien el seu estat; + i − es premen i es deixen
                // anar; el pom gira en sentit horari / antihorari o es prem (els codis decideixen si mou els bugs o el QNH)
                switch (id)
                {
                    case "MIP_ISIS_BUGS" when cmd == "PUSH":
                        SendGenericRpn("(>H:A32NX_ISIS_BUGS_PRESSED) (L:A32NX_ISIS_BUGS_ACTIVE) ! (>L:A32NX_ISIS_BUGS_ACTIVE)"); return true;
                    case "MIP_ISIS_LS" when cmd == "PUSH":
                        SendGenericRpn("(>H:A32NX_ISIS_LS_PRESSED) (L:A32NX_ISIS_LS_ACTIVE) ! (>L:A32NX_ISIS_LS_ACTIVE)"); return true;
                    case "MIP_ISIS_RST" when cmd == "PUSH":
                        SendGenericRpn("0 (>H:A32NX_ISIS_RST_PRESSED)"); return true;
                    case "MIP_ISIS_PLUS" or "MIP_ISIS_MINUS" when cmd == "PUSH":
                    {
                        string b = id == "MIP_ISIS_PLUS" ? "PLUS" : "MINUS";
                        SendGenericRpn($"(>H:A32NX_ISIS_{b}_PRESSED)");
                        System.Threading.Tasks.Task.Delay(150).ContinueWith(_ => { try { BeginInvoke(new Action(() => SendGenericRpn($"(>H:A32NX_ISIS_{b}_RELEASED)"))); } catch { } });
                        return true;
                    }
                    case "MIP_ISIS_BARO" when cmd == "INC" || cmd == "DEC":
                    {
                        string h = cmd == "INC" ? "A32NX_ISIS_KNOB_CLOCKWISE" : "A32NX_ISIS_KNOB_ANTI_CLOCKWISE";
                        string k = cmd == "INC" ? "KOHLSMAN_INC" : "KOHLSMAN_DEC";
                        string op = cmd == "INC" ? "++" : "--";
                        SendGenericRpn($"(L:A32NX_ISIS_BUGS_ACTIVE, enum) 1 == if{{ 0 (>H:{h}) }} els{{ (L:A32NX_ISIS_BARO_MODE, enum) 1 != if{{ " +
                                       $"(L:A32NX_ISIS_BARO_UNIT_INHG, enum) if{{ 3 (>K:{k}) }} els{{ 3 (A:KOHLSMAN SETTING MB:3, millibars) {op} 16 * (>K:2:KOHLSMAN_SET) }} }} }}");
                        return true;
                    }
                    case "MIP_ISIS_BARO" when cmd == "PUSH":
                        SendGenericRpn("(L:A32NX_ISIS_BUGS_ACTIVE, bool) if{ 0 (>H:A32NX_ISIS_KNOB_PRESSED, number) } els{ (L:A32NX_ISIS_BARO_MODE, enum) 0 == if{ " +
                                       "1 (>L:A32NX_ISIS_BARO_MODE, enum) (A:KOHLSMAN SETTING MB:2, bars) 16000 * (>O:A32NX_ISIS_BARO_SAVED, number) 2 16211 (>K:2:KOHLSMAN_SET) } " +
                                       "els{ 0 (>L:A32NX_ISIS_BARO_MODE, enum) 2 (O:A32NX_ISIS_BARO_SAVED, number) (>K:2:KOHLSMAN_SET) } }");
                        return true;
                }
                // palanca del tren: ordres estàndard que el FBW intercepta (a terra, el bloqueig de la palanca la frena)
                if (id == "MIP_GEAR_LEVER" && (cmd == "UP" || cmd == "DOWN")) { SendGenericRpn(cmd == "UP" ? "(>K:GEAR_UP)" : "(>K:GEAR_DOWN)"); return true; }
                // SPEED BRAKE (API del FBW): posició amb SPOILERS_SET (0 … 16384); armar / desarmar amb el toggle, només si cal
                if (id == "PED_SPEEDBRAKE")
                {
                    if (cmd.StartsWith("POS:") && float.TryParse(cmd.AsSpan(4), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float sp))
                        SendGenericRpn($"{(int)Math.Round(Math.Clamp(sp, 0f, 1f) * 16384)} (>K:SPOILERS_SET)");
                    else if (cmd == "ARM") SendGenericRpn("(L:A32NX_SPOILERS_ARMED) ! if{ (>K:SPOILERS_ARM_TOGGLE) }");
                    else if (cmd == "DISARM") SendGenericRpn("(L:A32NX_SPOILERS_ARMED) if{ (>K:SPOILERS_ARM_TOGGLE) }");
                    return true;
                }
                if (id == "MIP_BRK_FAN" && cmd == "PUSH") { SendGenericRpn("(L:A32NX_BRAKE_FAN_BTN_PRESSED) ! (>L:A32NX_BRAKE_FAN_BTN_PRESSED)"); return true; }
                if (id == "MIP_THROTTLE")
                {
                    // palanques arrossegades a l'app ("BOTH:v", "LEFT:v", "RIGHT:v"): com si es mogués l'eix de gas
                    int c = cmd.IndexOf(':');
                    if (c < 0 || !float.TryParse(cmd.AsSpan(c + 1), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float p)) return true;
                    uint raw = unchecked((uint)(int)Math.Round(Math.Clamp(FbwAxisFor(p), -1, 1) * 16384));
                    string which = cmd[..c];
                    if (which != "RIGHT")
                        simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, FbwThr1, raw, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    if (which != "LEFT")
                        simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, FbwThr2, raw, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    return true;
                }
            }
            catch { }
            return false;
        }

        private void WriteMsfsLvar(string lvar, double value)
        {
            if (simconnect == null || !_msWriteDefs.TryGetValue(lvar, out var def)) return;
            simconnect.SetDataOnSimObject(def, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new A320BaroUnitStruct { isInhg = value });
        }

        /// <summary>Lectura del glareshield del FlyByWire (només arriba quan canvia algun valor).</summary>
        private void OnMsfsA320Data(double[] v)
        {
            if (_xp != null && _xp.A320Loaded) return;   // amb el ToLiss carregat mana X-Plane
            _msA320Data = true;
            bool trk = v[7] > 0.5;
            var s = new panelAIR1000.FcuState(v[12] > 0.5,
                v[0], v[1] > 0.5, v[2] > 0.5, v[3] > 0.5,
                v[4], v[5] > 0.5, v[6] > 0.5, trk,
                v[8], v[9] > 0.5,
                v[10], v[10], v[11] > 0.5,    // V/S i FPA comparteixen la LVAR (segons el mode)
                FbwStyle: true);
            _msFcu = s;
            _msAltStep = v[13] > 0.5;
            _msThr = (FbwLeverPos(v[14]), FbwLeverPos(v[15]));
            // configuració de l'eix (si l'avió encara no l'ha carregada, tot val 0: es manté la per defecte)
            if (v[28] > v[17])
            {
                _fbwRevOnAxis = v[16] > 0.5;
                for (int i = 0; i < _fbwRange.Length; i++) _fbwRange[i] = (v[17 + 2 * i], v[18 + 2 * i]);
            }
            // tren: triangle verd = blocat avall, UNLK = desblocat
            string[] gears = { "MIP_GEAR_IND_0", "MIP_GEAR_IND_1", "MIP_GEAR_IND_2" };   // esquerre, morro, dret
            for (int i = 0; i < 3; i++) { _msLegends[(gears[i], false)] = v[29 + i * 2] > 0.5; _msLegends[(gears[i], true)] = v[30 + i * 2] > 0.5; }
            // autofre: ON = armat en aquell mode; DECEL = llum de desacceleració amb aquell mode
            int abMode = (int)Math.Round(v[35]); bool decel = v[36] > 0.5;
            string[] abIds = { "MIP_AUTOBRK_LO", "MIP_AUTOBRK_MED", "MIP_AUTOBRK_MAX" };
            for (int i = 0; i < 3; i++) { _msLegends[(abIds[i], false)] = abMode == i + 1; _msLegends[(abIds[i], true)] = decel && abMode == i + 1; }
            _msLegends[("MIP_BRK_FAN", false)] = v[37] > 0.5;
            _msLegends[("MIP_BRK_FAN", true)] = v[38] > 0.5;
            // rellotge: cronòmetre i ET en segons (≤ 0 = pantalla apagada, com després de RST); selector RUN/STP/RST
            double chr = v[39], et = v[40];
            int cs = (int)Math.Floor(Math.Max(0, chr)), es = (int)Math.Floor(Math.Max(0, et));
            string chrTxt = chr > 0 ? $"{Math.Min(99, cs / 60):00}:{cs % 60:00}" : "";
            string etTxt = et >= 0 ? $"{Math.Min(99, es / 3600):00}:{es / 60 % 60:00}" : "";
            _msBrakes = ((float)(v[42] / 1000.0), (float)(v[43] / 1000.0), (float)(v[44] / 1000.0));
            int etPos = (int)Math.Round(v[41]);
            if (etPos >= 0 && etPos <= 2) _msPos["MIP_CLK_RUN"] = etPos;
            int k = MsfsA320Display.Length;
            foreach (var (id, _) in MsfsA320Lights) _msLights[id] = v[k++] > 0.5;
            foreach (var (id, _, toSim) in MsfsA320Pos)
            {
                int simVal = (int)Math.Round(v[k++]);
                int idx = Array.IndexOf(toSim, simVal);
                if (idx >= 0) _msPos[id] = idx;
            }
            // variables de simulació: A/SKID (app 0 = ON) i hora UTC
            _msPos["MIP_ASKID_NWSTRG"] = v[k++] > 0.5 ? 0 : 1;
            int us = (int)Math.Floor(Math.Max(0, v[k++])) % 86400;
            int zm = (int)Math.Round(v[k++]), zd = (int)Math.Round(v[k++]), zy = (int)Math.Round(v[k++]);
            _msZulu = (us, zm, zd, zy);
            // tren: la petició (moure la palanca a la VC) arriba de seguida i és la que fa començar l'animació de l'app;
            // passats 0,3 s mana la palanca 3D: si no ha sortit de l'extrem on era, la palanca no s'ha mogut (p.ex. bloquejada
            // a terra) i l'app torna enrere. (La palanca 3D es dona per moguda en quan surt un 5% del seu extrem.)
            // (mesurat en vol: la petició, v[45], salta 0/1 a cada lectura mentre el tren es mou; no es fa servir)
            double gAnim = v[46]; k++;   // (GEAR HANDLE POSITION: tarda uns 5 s, no es fa servir)
            bool ad = _msGearAnimDown ?? gAnim > 0.5;
            if (ad && gAnim < 0.95) ad = false; else if (!ad && gAnim > 0.05) ad = true;
            _msGearAnimDown = ad;
            _msGearDown = ad;
            _msClock = (chrTxt, MsUtcText(), etTxt);
            _msSpdBrk = ((float)v[47], v[48] > 0.5);
            ApplyMsfsA320State();
        }

        /// <summary>Aplica al panell l'últim estat llegit del FlyByWire.</summary>
        private void ApplyMsfsA320State()
        {
            var ov = _a320Overhead;
            if (ov == null || !_msA320Data || (_xp != null && _xp.A320Loaded)) return;
            ov.SimDriven = true;
            if (_msReg != null) ov.SetRegistration(_msReg);
            if (_msFcu != null) ov.SetFcu(_msFcu);
            if (_msAltStep is bool st) ov.SetKnobRing("MIP_FCU_ALT", st);
            foreach (var kv in _msLights) ov.SetLight(kv.Key, kv.Value);
            foreach (var kv in _msPos) ov.SetPosition(kv.Key, kv.Value);
            // palanques de gas: només amb el FlyByWire (amb el Fenix aquestes LVARs no existeixen i valdrien 0)
            if (_msThr is var (tl, tr) && IsA320FBW()) ov.SetThrottle(tl, tr);
            if (IsA320FBW())
            {
                foreach (var kv in _msLegends)
                    ov.SetLegend(kv.Key.id, kv.Key.upper ? (kv.Value ? 1f : 0f) : null, kv.Key.upper ? null : (kv.Value ? 1f : 0f));
                if (_msClock is var (mc, mu, me)) ov.SetClock(mc, mu, me);
                if (_msBrakes is var (ba, bl, br)) ov.SetBrakeGauge(ba, bl, br);
                if (_msGearDown is bool gd) ov.SetGearLever(gd);
                if (_msSpdBrk is var (sbp, sba)) ov.SetSpeedBrake(sbp, sba);
            }
        }
    }
}
