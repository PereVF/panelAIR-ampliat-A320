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
        };

        // lectura: pantalla + llums + selectors, tot en un sol bloc de nombres
        const int MsfsA320ReadCount = 14 + 15 + 4;
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
            foreach (var name in MsfsA320Display) { simconnect.AddToDataDefinition(readDef, "L:" + name, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            foreach (var l in MsfsA320Lights) { simconnect.AddToDataDefinition(readDef, "L:" + l.lvar, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            foreach (var p in MsfsA320Pos) { simconnect.AddToDataDefinition(readDef, "L:" + p.lvar, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            if (n != MsfsA320ReadCount) throw new InvalidOperationException("A320 MSFS: el nombre de LVARs llegides no quadra amb MsfsA320ReadCount");
            simconnect.RegisterDataDefineStruct<A320GlareStruct>(readDef);
            simconnect.RequestDataOnSimObject((REQUESTS)MsfsA320ReadReq, readDef, SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.VISUAL_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
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
            int k = MsfsA320Display.Length;
            foreach (var (id, _) in MsfsA320Lights) _msLights[id] = v[k++] > 0.5;
            foreach (var (id, _, toSim) in MsfsA320Pos)
            {
                int simVal = (int)Math.Round(v[k++]);
                int idx = Array.IndexOf(toSim, simVal);
                if (idx >= 0) _msPos[id] = idx;
            }
            ApplyMsfsA320State();
        }

        /// <summary>Aplica al panell l'últim estat llegit del FlyByWire.</summary>
        private void ApplyMsfsA320State()
        {
            var ov = _a320Overhead;
            if (ov == null || !_msA320Data || (_xp != null && _xp.A320Loaded)) return;
            ov.SimDriven = true;
            if (_msFcu != null) ov.SetFcu(_msFcu);
            if (_msAltStep is bool st) ov.SetKnobRing("MIP_FCU_ALT", st);
            foreach (var kv in _msLights) ov.SetLight(kv.Key, kv.Value);
            foreach (var kv in _msPos) ov.SetPosition(kv.Key, kv.Value);
        }
    }
}
