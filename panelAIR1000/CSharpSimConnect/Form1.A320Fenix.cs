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
            ("MIP_TERR_ON_ND_1", "PUSH", Set("S_MIP_GPWS_TERRAIN_ON_ND_CAPT", 1), Set("S_MIP_GPWS_TERRAIN_ON_ND_CAPT", 0)),
        };

        /// <summary>Selectors: element → L-var (la numeració del Fenix és la mateixa que la de l'app).</summary>
        static readonly (string id, string lvar)[] FenixPos =
        {
            ("MIP_ND_MODE", "S_FCU_EFIS1_ND_MODE"),        // 0 LS, 1 VOR, 2 NAV, 3 ARC, 4 PLAN
            ("MIP_ND_RANGE", "S_FCU_EFIS1_ND_ZOOM"),       // 0 = 10 … 5 = 320 NM
            ("MIP_EFIS_ADFVOR1", "S_FCU_EFIS1_NAV1"),      // 0 ADF, 1 OFF, 2 VOR
            ("MIP_EFIS_ADFVOR2", "S_FCU_EFIS1_NAV2"),
        };

        /// <summary>Llums: element → L-var.</summary>
        static readonly (string id, string lvar)[] FenixLights =
        {
            ("MIP_FCU_AP1", "I_FCU_AP1"), ("MIP_FCU_AP2", "I_FCU_AP2"), ("MIP_FCU_ATHR", "I_FCU_ATHR"),
            ("MIP_FCU_LOC", "I_FCU_LOC"), ("MIP_FCU_APPR", "I_FCU_APPR"), ("MIP_FCU_EXPED", "I_FCU_EXPED"),
            ("MIP_EFIS_FD", "I_FCU_EFIS1_FD"), ("MIP_EFIS_LS", "I_FCU_EFIS1_LS"), ("MIP_EFIS_CSTR", "I_FCU_EFIS1_CSTR"),
            ("MIP_EFIS_WPT", "I_FCU_EFIS1_WPT"), ("MIP_EFIS_VORD", "I_FCU_EFIS1_VORD"), ("MIP_EFIS_NDB", "I_FCU_EFIS1_NDB"),
            ("MIP_EFIS_ARPT", "I_FCU_EFIS1_ARPT"),
            ("MIP_MASTER_WARN", "I_MIP_MASTER_WARNING_CAPT"), ("MIP_MASTER_CAUT", "I_MIP_MASTER_CAUTION_CAPT"),
            ("MIP_TERR_ON_ND_1", "I_MIP_GPWS_TERRAIN_ON_ND_CAPT_L"),
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

        const int FenixReadCount = 20 + 16 + 4;
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

        private void RegisterFenixGlareshield()
        {
            if (simconnect == null) return;
            var def = (DEFINITIONS)FenixReadDef;
            int n = 0;
            foreach (var name in FenixDisplay) { simconnect.AddToDataDefinition(def, "L:" + name, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            foreach (var l in FenixLights) { simconnect.AddToDataDefinition(def, "L:" + l.lvar, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
            foreach (var p in FenixPos) { simconnect.AddToDataDefinition(def, "L:" + p.lvar, "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED); n++; }
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
            if (cmd.StartsWith("POS:") && int.TryParse(cmd.AsSpan(4), out int pos))
                foreach (var p in FenixPos)
                    if (p.id == id) { _fxPos[id] = pos; SendGenericRpn(Set(p.lvar, pos)); return true; }
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
            foreach (var (id, _) in FenixPos) _fxPos[id] = (int)Math.Round(v[k++]);
            ApplyFenixState();
        }

        private void ApplyFenixState()
        {
            var ov = _a320Overhead;
            if (ov == null || !_fxData || !IsA320Fenix() || (_xp != null && _xp.A320Loaded)) return;
            ov.SimDriven = true;
            if (_fxFcu != null) ov.SetFcu(_fxFcu);
            if (_fxAltStep is bool st) ov.SetKnobRing("MIP_FCU_ALT", st);
            if (_fxBaro is var (bp, bm, bv, bh)) ov.SetBaro(bp, bm, bv, bh);
            if (_fxThr is var (tl, tr)) ov.SetThrottle(tl, tr);
            foreach (var kv in _fxLights) ov.SetLight(kv.Key, kv.Value);
            foreach (var kv in _fxPos) ov.SetPosition(kv.Key, kv.Value);
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
        private void PlaceFenixScreens()
        {
            var wins = XPlaneWindows.FindAll("WASMINSTRUMENT", MsfsProcesses);
            foreach (var h in new List<IntPtr>(_fxSlots.Keys)) if (!wins.Contains(h)) _fxSlots.Remove(h);
            foreach (var h in new List<IntPtr>(_fxSeen.Keys)) if (!wins.Contains(h)) _fxSeen.Remove(h);
            var now = DateTime.UtcNow;
            // ordre d'aparició: les noves de la de sota a la de dalt de l'ordre de Windows (la més antiga, primer)
            for (int i = wins.Count - 1; i >= 0; i--) if (!_fxSeen.ContainsKey(wins[i])) _fxSeen[wins[i]] = (now, _fxSeq++);
            if (wins.Exists(h => !_fxSlots.ContainsKey(h)) && (now - _fxLastClassify).TotalSeconds >= 2)
            {
                _fxLastClassify = now;
                // 1) reconeixement per la imatge
                foreach (var h in wins)
                {
                    if (_fxSlots.ContainsKey(h)) continue;
                    int kind;
                    using (var bmp = XPlaneWindows.Capture(h)) kind = bmp == null ? -1 : ClassifyFenix(bmp);
                    if (kind >= 0 && !_fxSlots.ContainsValue(kind)) _fxSlots[h] = kind;
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
                    _fxSlots[h] = slot;
                }
            }
            foreach (var kv in _fxSlots)
                try { if (SimWindowRect(kv.Value, MsfsExtraHeight) is System.Drawing.Rectangle wr) XPlaneWindows.Place(kv.Key, wr, this.Handle); }
                catch { }
            UpdatePopoutMessage(4 - _fxSlots.Count, 4, _msfsSince, ref _fxPopoutMsgShown, FenixPopoutText);
        }
        private DateTime _fxLastClassify = DateTime.MinValue;
        private bool _fxPopoutMsgShown;
        private readonly Dictionary<IntPtr, (DateTime t, long seq)> _fxSeen = new();
        private long _fxSeq;
        private const string FenixPopoutText = "In Microsoft Flight Simulator, hold Right Alt and click the A320 displays one at a time, in this order: PFD, ND, upper ECAM, lower ECAM. panelAIR will place them in the panel.";

        /// <summary>
        /// Quina pantalla del Fenix és (0 PFD, 1 ND, 2 ECAM superior, 3 ECAM inferior; -1 si no se sap, p.ex. negra):
        ///  - PFD: hi ha prou cel blau i terra marró (l'horitzó artificial);
        ///  - ECAM inferior (SD): línia horitzontal clara de gairebé tota l'amplada al 78-95% de l'alçada (franja TAT/SAT/GW);
        ///  - ECAM superior (EWD): línia horitzontal clara llarga al 55-78% (separació entre motors i missatges);
        ///  - ND: la resta (pantalla amb contingut però sense cap d'aquestes línies llargues).
        /// </summary>
        private static int ClassifyFenix(System.Drawing.Bitmap b)
        {
            int w = b.Width, h = b.Height;
            var data = b.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var px = new byte[data.Stride * h];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, px, 0, px.Length);
            int stride = data.Stride;
            b.UnlockBits(data);
            int x0 = (int)(w * 0.06), x1 = (int)(w * 0.94), y0 = (int)(h * 0.04), y1 = (int)(h * 0.96);
            long horizon = 0, tot = 0, lit = 0;
            double ewd = 0, sd = 0;
            for (int y = y0; y < y1; y++)
            {
                int run = 0, maxRun = 0;
                for (int x = x0; x < x1; x++)
                {
                    int i = y * stride + x * 4;
                    int bl = px[i], gr = px[i + 1], rd = px[i + 2];
                    if ((y & 1) == 0 && (x & 1) == 0)
                    {
                        tot++;
                        if ((bl > rd + 50 && bl > 100) || (rd > bl + 40 && rd > 60 && gr > 30 && gr < rd)) horizon++;
                        if (rd + gr + bl > 150) lit++;
                    }
                    if ((rd + gr + bl) / 3 > 110) { if (++run > maxRun) maxRun = run; } else run = 0;
                }
                double f = (double)y / h, frac = (double)maxRun / (x1 - x0);
                if (f > 0.55 && f < 0.78 && frac > ewd) ewd = frac;
                if (f >= 0.78 && f < 0.95 && frac > sd) sd = frac;
            }
            if (tot == 0 || (double)lit / tot < 0.005) return -1;   // encara negra (sense corrent o carregant)
            if ((double)horizon / tot > 0.03) return 0;
            if (sd >= 0.85) return 3;
            if (ewd >= 0.5) return 2;
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
                          $"In Microsoft Flight Simulator, hold Right Alt and click the {FenixScreenNames[step]}. " +
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
            foreach (var h in XPlaneWindows.FindAll("WASMINSTRUMENT", MsfsProcesses)) XPlaneWindows.Close(h);
        }
    }
}
