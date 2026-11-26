using System.Drawing.Drawing2D;

namespace panelAIR1000
{
    // ═════════════════════════════════════════════════════════════════════════════════════════
    //  PEDESTAL A320 — capa independent que puja des de baix (botó PED), com l'overhead.
    //  Coordenades de referència: la imatge del pedestal girada en vertical (1998 x 3216 px),
    //  × 0,625 = unitats lògiques (amplada 1250, com l'overhead i el panell principal).
    //  Fase 1: dibuix i elements tocables i animats a l'app (sense enllaç amb el simulador, menys el
    //  quadrant de gas, que és el mateix element MIP_THROTTLE del panell principal).
    // ═════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>Element que s'arrossega amb el dit (el panell no es desplaça mentre s'arrossega).</summary>
    internal interface IOhDraggable { bool Grabbing { get; } void DragTo(PointF p); }

    public sealed partial class A320OverheadControl
    {
        const float PedK = 0.625f;
        /// <summary>Escala de px de referència a unitats lògiques (per als elements del pedestal).</summary>
        public const float PedKPublic = PedK;
        /// <summary>Alçada lògica del pedestal (inclou el pom de la GRAVITY GEAR, que sobresurt per baix).</summary>
        public const float PedLH = 3216f * PedK;

        /// <summary>Vidre de la MCDU de l'esquerra (unitats lògiques): on va la finestra pop-out de la MCDU.</summary>
        /// <summary>Desplaçament de la MCDU dreta respecte de l'esquerra (px de referència).</summary>
        const float PedMcdu2Dx = 1334f;
        static readonly RectangleF PedMcdu1Glass = RectangleF.FromLTRB(155 * PedK, (65 + McduScrDy) * PedK, 495 * PedK, (360 + McduScrDy) * PedK);
        /// <summary>
        /// MCDU esquerra: pantalla i 12 tecles laterals baixades fins que la vora de baix de l'embut queda a 5 de la planxa
        /// de les tecles de control (aquesta comença a 372 + 3/4 de l'alçada d'una tecla de control, 36 × 0,95 × 0,75).
        /// </summary>
        const float McduScrDy = 36f * 0.95f * 0.75f - 5f;

        // ── Graella de text de la pantalla de la MCDU (la mateixa per a ToLiss, FBW i Fenix): 14 files iguals dins el
        //    vidre, amb un marge de 4 (px de referència) a dalt i a baix. Les línies de dades (files 2, 4 … 12) queden
        //    a l'alçada de les ratlles de les tecles laterals. ──
        public const float McduTextInsetY = 4f;
        const float McduRowH = (295f - 2 * McduTextInsetY) / 14f;
        /// <summary>Centre vertical d'una fila de text de la MCDU (0 títol … 13 scratchpad), en px de referència.</summary>
        static float McduRowY(int row) => 65 + McduScrDy + McduTextInsetY + (row + 0.5f) * McduRowH;

        static readonly PointF[] PedFramePoly = MakePedFrame();
        static PointF[] MakePedFrame()
        {
            // Com l'overhead: marc de gruix constant (14 unitats lògiques entre la vora dels panells i la vora del
            // marc) i cantonades de radi 2,5 (el mateix que els panells). Envolupant dels panells (px de referència):
            //   part ampla x 66.6..1925.4, y 30..1930 · part estreta x 272..1696, fins a y 2987 (tires estriades fins a 3100)
            const float gap = 14f;
            float L = SideL0 * PedK - gap, Rr = SideR1 * PedK + gap, T = 30 * PedK - gap, Yw = 1930 * PedK + gap;
            float NL = 272 * PedK - gap, NR = 1696 * PedK + gap, B = 3100 * PedK + gap;   // a baix, sota les tires estriades
            return Oh.RoundAll(new PointF[]
            {
                new(L, T), new(Rr, T), new(Rr, Yw), new(NR, Yw), new(NR, B), new(NL, B), new(NL, Yw), new(L, Yw)
            }, 2.5f);
        }

        // Columnes laterals (MCDU, RMP, ACP...): estretes perquè quedin a 6 unitats lògiques (9,6 px de referència) de
        // la columna central, com l'espai entre panells de l'overhead; el mateix retall a la vora exterior (simètric).
        //   esquerra: 62..595 → 66.6..590.4 · dreta: 1396..1929 → 1399.6..1925.4
        const float SideL0 = 66.6f, SideL1 = 590.4f, SideR0 = 1399.6f, SideR1 = 1925.4f;
        static float SideX(float x) => x switch { 62f => SideL0, 595f => SideL1, 1396f => SideR0, 1929f => SideR1, _ => x };

        /// <summary>Pom de la GRAVITY GEAR (sobresurt per sota del marc; la capa no el retalla).</summary>
        static readonly PointF[] PedGearKnob =
        {
            new(946 * PedK, 3108 * PedK), new(1006 * PedK, 3108 * PedK), new(1056 * PedK, 3168 * PedK),
            new(1046 * PedK, 3206 * PedK), new(908 * PedK, 3206 * PedK), new(898 * PedK, 3168 * PedK)
        };

        // ── Mòdul MCDU del panell principal (botó MCDU): la MCDU esquerra sencera, desplaçada perquè quedi centrada a
        //    l'amplada lògica (així el control del mòdul la mostra sencera amb el mateix mecanisme de dibuix) ──
        /// <summary>Desplaçament (px de referència) de la MCDU del mòdul: el centre de la MCDU al centre de l'amplada lògica.</summary>
        const float McduModDx = LW / PedK / 2f - (SideL0 + SideL1) / 2f;
        /// <summary>Amplada i alçada lògiques de la MCDU sencera (planxa de sota: 66,6..590,4 × 30..856 px de referència).</summary>
        public const float McduUnitLW = (SideL1 - SideL0) * PedK, McduUnitLH = (856f - 30f) * PedK;
        /// <summary>Contorn de la MCDU del mòdul (unitats lògiques), per retallar-ne la finestra.</summary>
        static readonly PointF[] McduModFrame = Oh.RoundAll(new PointF[]
        {
            new((SideL0 + McduModDx) * PedK, 30 * PedK), new((SideL1 + McduModDx) * PedK, 30 * PedK),
            new((SideL1 + McduModDx) * PedK, 856 * PedK), new((SideL0 + McduModDx) * PedK, 856 * PedK)
        }, 2.5f);

        void BuildPedestal(bool mcduOnly = false)
        {
            const float k = PedK;
            RectangleF R(float x0, float y0, float x1, float y1) => RectangleF.FromLTRB(x0 * k, y0 * k, x1 * k, y1 * k);
            PointF Pt(float x, float y) => new(x * k, y * k);
            void Lb(string t, float x, float y, float size, bool bold = true) => _silk.Add(new OhLabel(t, x * k, y * k, size, Oh.Silk, bold));
            void Vt(string t, float x, float y, float size) => _silk.Add(new OhVText(t, x * k, y * k, size));
            // DZUS (mida i osca com a l'overhead) només on n'hi ha a la imatge de referència: l = alçades a la vora esquerra, r = a la dreta
            // lc / rc = alçades dels cargols d'estrella grans que van a la mateixa osca que un DZUS
            float[] Sk(float[]? v) => (v ?? Array.Empty<float>()).Select(y => y * k).ToArray();
            void Pp(float x0, float y0, float x1, float y1, float[]? l = null, float[]? r = null, float[]? lc = null, float[]? rc = null) =>
                _silk.Add(new OhPanel(R(SideX(x0), y0, SideX(x1), y1), Sk(l), Sk(r), 1.5f, 2.5f, Sk(lc), Sk(rc)));
            float[] Y(params float[] v) => v;
            // cargols solts (no DZUS), en px de referència: estrella gran, estrella petit, "+" encastat, ranura petit
            void ScX(params float[] xy) { for (int i = 0; i < xy.Length; i += 2) _silk.Add(new OhScrew(PedScrewKind.Cross, xy[i] * k, xy[i + 1] * k, 8.4f)); }
            void ScP(params float[] xy) { for (int i = 0; i < xy.Length; i += 2) _silk.Add(new OhScrew(PedScrewKind.Plus, xy[i] * k, xy[i + 1] * k, 6.2f)); }
            void ScL(params float[] xy) { for (int i = 0; i < xy.Length; i += 2) _silk.Add(new OhScrew(PedScrewKind.Slot, xy[i] * k, xy[i + 1] * k, 8.4f)); }
            void ScS(params float[] xy) { for (int i = 0; i < xy.Length; i += 2) _silk.Add(new OhScrew(PedScrewKind.Slot, xy[i] * k, xy[i + 1] * k, 6.2f)); }
            void Wl(params float[] xy)
            {
                var pts = new PointF[xy.Length / 2];
                for (int i = 0; i < pts.Length; i++) pts[i] = Pt(xy[i * 2], xy[i * 2 + 1]);
                _silk.Add(new OhLine(Oh.Silk, 1.3f, pts));
            }
            OhKey PK(string id, float cx, float cy, float w, float h, string l1, string? l2 = null, float maxText = 13f, bool momentary = true)
            {
                var kk = Key(id, (cx - w / 2) * k, (cy - h / 2) * k, w * k, h * k, l1);
                kk.Label2 = l2; kk.MaxText = maxText; kk.Momentary = momentary;
                return kk;
            }
            OhRoundKey RK(string id, float cx, float cy, float r, string label) => Add(new OhRoundKey(id, cx * k, cy * k, r * k, label));
            OhKnob Kn(string id, float cx, float cy, float r, int index, float[] angles, KnobStyle st = KnobStyle.Pointer) => Knob(id, cx * k, cy * k, r, index, angles, st);
            OhToggle HTog(string id, float cx, float cy, int n, int index)
            {
                var t = TogS(id, cx * k, cy * k, n, index, ToggleStyle.Metal, 8f).MakeHorizontal();
                t.ZoneWidth = 34f; t.ZoneHeight = 20f;
                return t;
            }
            float[] Brt = { -140, -70, 0, 70, 140 };

            // ═══════════════ MCDU (capità a l'esquerra, primer oficial a la dreta) ═══════════════
            void Mcdu(string p, float dx)
            {
                // L = MCDU refeta segons la imatge de referència i la captura del Fenix. Ara les dues MCDU són iguals
                // (la de la dreta, la mateixa desplaçada dx); la branca antiga (!L) es conserva però ja no s'usa.
                bool L = true;
                // el conjunt de tecles de control i la seva planxa, un 5% més petit (respecte del seu cantó de dalt a
                // l'esquerra, 118 / 372)
                const float CtlK = 0.95f;
                // i desplaçat 5 cap a la dreta i 3/4 de l'alçada d'una tecla de control cap avall (36 × 0,95 × 0,75)
                const float CtlDx = 5f, CtlDy = 36f * CtlK * 0.75f;
                float Cx(float x) => L ? 118 + (x - 118) * CtlK + CtlDx : x;
                float Cy(float y) => L ? 372 + (y - 372) * CtlK + CtlDy : y;
                PointF CP(float x, float y) => Pt(Cx(x) + dx, Cy(y));
                if (L)
                {
                    // 1) planxa de sota: llisa, sense osques (els DZUS hi queden per sobre)
                    // (mòdul MCDU del panell principal: la MCDU es construeix desplaçada; la planxa fa la mateixa amplada)
                    if (dx == 0 || dx == PedMcdu2Dx) Pp(62 + dx, 30, 595 + dx, 856);
                    else Pp(SideL0 + dx, 30, SideL1 + dx, 856);
                    // 2) planxa de la cara (tecles laterals, lletres i números): osques DZUS a la meitat de dalt; a la
                    //    meitat de baix va per dins de la nansa
                    _silk.Add(new OhMcduFace(k, dx));
                    // nansa d'extracció: braços des del capdamunt de les frontisses, desviament als DZUS de baix i tram de baix
                    _silk.Add(new OhMcduHandle(k, dx));
                    // 6 DZUS: 4 a les cantonades i 2 sobre les frontisses
                    foreach (var (sx, sy) in new[] { (83.04f, 75f), (573.96f, 75f), (83.04f, 453f), (573.96f, 453f), (83.04f, 813f), (573.96f, 813f) })
                        _silk.Add(new OhScrew(PedScrewKind.Dzus, (sx + dx) * k, sy * k, 8.4f));
                    // 3) planxa de les tecles de control (DIR…DATA i una tecla buida, F-PLN…MCDU MENU, AIR PORT i una buida,
                    //    fletxes), aixecada sobre la cara (tecles de 52 × 36 a pas de 64 × 48: dues files de 6 i bloc de 2
                    //    columnes a sota)
                    _silk.Add(new OhPanel(Oh.RoundAll(new[] { CP(118, 372), CP(502, 372), CP(502, 470),
                                                              CP(246, 470), CP(246, 616), CP(118, 616) }, 4f * k), Array.Empty<PointF>()));
                    // pantalla enfonsada dins la cara: forat (vora exterior = marc de la MCDU dreta), parets còniques i pantalla de sempre al fons
                    var glass = PedMcdu1Glass; glass.Offset(dx * k, 0);
                    _silk.Add(new OhMcduWell(R(143 + dx, 53 + McduScrDy, 507 + dx, 372 + McduScrDy), glass, 12f * k, 6f * k));
                    // text de la pantalla (ToLiss: llegit dels datarefs; amb el Fenix hi va la finestra pop-out a sobre)
                    Add(new OhMcduText(p + "_SCREEN", glass));
                    // 5 llums d'avís de la part de dalt (apagats: càpsules negres), entre la vora de la cara i l'embut,
                    // repartits per igual entre els dos LED i centrats a la MCDU
                    float ay = (36 + 53 + McduScrDy) / 2f;
                    for (int a = 0; a < 5; a++)
                        _silk.Add(new OhAnnPill(R(195.5f + a * 66 - 25 + dx, ay - 7, 195.5f + a * 66 + 25 + dx, ay + 7)));
                }
                else
                {
                    Pp(62 + dx, 30, 595 + dx, 856, Y(75, 215, 455), Y(75, 455));
                    _silk.Add(new OhScreen(R(143 + dx, 53, 507 + dx, 372), R(155 + dx, 65, 495 + dx, 360), 12f * k, false));
                }
                if (L)
                {
                    // dos cercles blancs encastats (com a la captura), a l'alçada dels llums d'avís i simètrics respecte d'ells
                    float wy = (36 + 53 + McduScrDy) / 2f;
                    _silk.Add(new OhWhiteDome((131 + dx) * k, wy * k, 15f * k));
                    _silk.Add(new OhWhiteDome((524 + dx) * k, wy * k, 15f * k));
                }
                else
                {
                    _silk.Add(new OhLed((131 + dx) * k, 48 * k, 8f * k));
                    _silk.Add(new OhLed((510 + dx) * k, 46 * k, 8f * k));
                }
                for (int i = 0; i < 6; i++)
                {
                    // (MCDU esquerra: + 22,5 perquè la ratlla de la 1a tecla arribi a la pantalla a l'alçada de la part de
                    //  baix de la 1a línia de dades, mesurat sobre la popout del Fenix i ajustat a ull per en Pere)
                    float y = 103 + i * 43.4f + (L ? McduScrDy + 22.5f : 0);
                    if (L)
                    {
                        // tecles com les de la MCDU dreta, una mica més cap a la vora del panell; la ratlla travessa
                        // la planxa i baixa per la paret del con fins a la pantalla (convergeix: la pantalla és més petita)
                        // model del ToLiss: la ratlla va recta de la tecla a la vora de la pantalla, on arriba al centre
                        // de la línia de dades; puja cap a la pantalla 0,5° a la 1a tecla, 1° a la 2a … 3° a la 6a
                        // (la tecla queda una mica més avall que la seva línia)
                        float yd = McduRowY(2 * (i + 1));
                        float rise = (float)Math.Tan((i + 1) * 0.5 * Math.PI / 180) * (155 - 131);
                        float yk = yd + rise;
                        PK(p + "_LSK" + (i + 1) + "L", 111 + dx, yk, 40, 26, "—");
                        PK(p + "_LSK" + (i + 1) + "R", 539 + dx, yk, 40, 26, "—");
                        // horitzontal fins a la vora de l'embut (143 / 507) i, a dins, inclinada fins a la pantalla
                        Wl(131 + dx, yk, 143 + dx, yk, 155 + dx, yd);
                        Wl(519 + dx, yk, 507 + dx, yk, 495 + dx, yd);
                    }
                    else
                    {
                        PK(p + "_LSK" + (i + 1) + "L", 117 + dx, y, 40, 26, "—");
                        PK(p + "_LSK" + (i + 1) + "R", 533 + dx, y, 40, 26, "—");
                    }
                }
                if (L)
                {
                    // BRT i DIM: dues tecles a la planxa de la cara, a la dreta de les tecles de control
                    PK(p + "_BRT", 522 + dx, Cy(398), 40, 36, "BRT");
                    PK(p + "_DIM", 522 + dx, Cy(446), 40, 36, "DIM");
                }
                else
                {
                    Lb("BRT", 533 + dx, 363, 9f);
                    Kn(p + "_BRT", 508 + dx, 393, 8f, 4, Brt, KnobStyle.Round);
                }
                // tecles de funció
                (string a, string? b, float x, float y, float h)[] fk = L
                    ? new (string, string?, float, float, float)[]
                    {
                        // més petites i repartides per igual; separació vertical entre files ×2 (pas de 48). Etiquetes i
                        // ordre com al Fenix: tecla buida a la dreta de DATA i a la dreta d'AIR PORT; fletxes ← ↑ i → ↓
                        ("DIR", null, 148, 398, 36), ("PROG", null, 212, 398, 36), ("PERF", null, 276, 398, 36), ("INIT", null, 340, 398, 36), ("DATA", null, 404, 398, 36), ("", null, 468, 398, 36),
                        ("F-PLN", null, 148, 446, 36), ("RAD", "NAV", 212, 446, 36), ("FUEL", "PRED", 276, 446, 36), ("SEC", "F-PLN", 340, 446, 36), ("ATC", "COMM", 404, 446, 36), ("MCDU", "MENU", 468, 446, 36),
                        ("AIR", "PORT", 148, 494, 36), ("", null, 212, 494, 36),
                        ("←", null, 148, 542, 36), ("↑", null, 212, 542, 36),
                        ("→", null, 148, 590, 36), ("↓", null, 212, 590, 36)
                    }
                    : new (string, string?, float, float, float)[]
                    {
                        ("DIR", null, 152, 403, 40), ("PROG", null, 222, 403, 40), ("PERF", null, 289, 403, 40), ("INIT", null, 359, 403, 40), ("DATA", null, 429, 403, 40),
                        ("F-PLN", null, 152, 455, 44), ("RAD", "NAV", 221, 455, 44), ("FUEL", "PRED", 289, 455, 44), ("SEC", "F-PLN", 360, 455, 44), ("ATC", "COMM", 429, 455, 44), ("MCDU", "MENU", 497, 455, 44),
                        ("AIR", "PORT", 152, 500, 44), ("↑", null, 221, 500, 44), ("NEXT", "PAGE", 152, 552, 44), ("↓", null, 221, 552, 44),
                        ("←", null, 152, 600, 38), ("→", null, 221, 600, 38)
                    };
                foreach (var f in fk)
                {
                    string id = f.a.Length == 0 ? p + "_BLANK" + (f.y < 420 ? "1" : "2")
                              : p + "_" + (f.a + (f.b ?? "")).Replace("-", "").Replace("↑", "UP").Replace("↓", "DOWN").Replace("←", "PREV").Replace("→", "NEXT");
                    bool arrow = f.a is "↑" or "↓" or "←" or "→";
                    var fkey = PK(id, Cx(f.x) + dx, Cy(f.y), L ? 52 * CtlK : 60, L ? f.h * CtlK : f.h, f.a, f.b, L && arrow ? 30f : 12f);
                    if (L && arrow) fkey.ArrowDir = f.a switch { "↑" => 1, "↓" => 2, "←" => 3, _ => 4 };
                }
                // lletres
                string[] rows = { "ABCDE", "FGHIJ", "KLMNO", "PQRST", "UVWXY" };
                float[] lx = { 296, 349, 401, 453, 505 };
                // (MCDU esquerra: les 6 files, de la A…E a la Z…CLR, repartides per igual entre 536 i 809)
                float[] ly = L ? new float[] { 536, 590.6f, 645.2f, 699.8f, 754.4f } : new float[] { 536, 589, 643, 697, 750 };
                for (int r = 0; r < 5; r++)
                    for (int c = 0; c < 5; c++)
                    {
                        var key = PK(p + "_KEY_" + rows[r][c], lx[c] + dx, ly[r], 42, 40, rows[r][c].ToString(), null, L ? 18f : 15f);
                        if (L) key.TextScale = 1.3f;
                        if (L && "ENSW".Contains(rows[r][c])) { key.BoxLabel = true; key.TextScale = 1.05f; }
                    }
                PK(p + "_KEY_Z", lx[0] + dx, 809, 42, 40, "Z", null, L ? 18f : 15f).TextScale = L ? 1.3f : 1f;
                if (L)
                {
                    // com al Fenix: barra de dividir i SP (espai)
                    PK(p + "_KEY_DIV", lx[1] + dx, 809, 42, 40, "/", null, 18f).TextScale = 1.3f;
                    PK(p + "_KEY_SP", lx[2] + dx, 809, 42, 40, "SP", null, 14f).TextScale = 1.1f;
                }
                else
                {
                    PK(p + "_KEY_SLASH2", lx[1] + dx, 809, 42, 40, "-", null, 15f);   // − (signe)
                    PK(p + "_KEY_PLUS", lx[2] + dx, 809, 42, 40, "+", null, 15f);
                }
                PK(p + "_KEY_OVFY", lx[3] + dx, 809, 42, 40, "OVFY", "Δ", 10f);
                PK(p + "_KEY_CLR", lx[4] + dx, 809, 42, 40, "CLR", null, 11f);
                // xifres (tecles rodones)
                // (MCDU esquerra, com al Fenix: l'última és +/-)
                string[] num = { "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0", L ? "+/-" : "/" };
                // (MCDU esquerra: més petites, i pujades fins que la fila de baix acaba on acaben les lletres, a 829)
                float[] nx = L ? new float[] { 145, 195, 245 } : new float[] { 142, 195, 247 };
                float[] ny = L ? new float[] { 661, 711, 761, 811 } : new float[] { 659, 713, 765, 818 };
                for (int i = 0; i < 12; i++)
                {
                    string id = num[i] == "." ? "DOT" : num[i] == "/" ? "SLASH" : num[i] == "+/-" ? "PLUSMINUS" : num[i];
                    RK(p + "_KEY_" + id, nx[i % 3] + dx, ny[i / 3], L ? 18 : 21, num[i]);
                }
                // llums FAIL / FMGC i MCDU MENU (franges verticals fosques); a la MCDU esquerra, simètriques respecte del
                // centre de la cara (FAIL/FMGC més a la dreta i MCDU MENU una mica més a la dreta)
                // (MCDU esquerra: gairebé a tocar del braç de la nansa)
                // (i 5 cap endins cadascuna, simètriques: FAIL/FMGC a la dreta amb les tecles de control, MCDU MENU a l'esquerra)
                // (sense text: només la part negra)
                float fx0 = (L ? 84 : 80) + dx, mx0 = (L ? 547 : 536) + dx;
                _silk.Add(new OhBlock(R(fx0, 578, fx0 + 26, 724), Color.FromArgb(44, 48, 53), Color.FromArgb(22, 24, 27), 3f));
                _silk.Add(new OhBlock(R(mx0, 578, mx0 + 26, 724), Color.FromArgb(44, 48, 53), Color.FromArgb(22, 24, 27), 3f));
            }
            // mòdul MCDU del panell principal: només la MCDU esquerra, centrada a l'amplada lògica (veure McduModDx)
            if (mcduOnly)
            {
                Mcdu("PED_MCDU1", McduModDx);
                // la tecla buida de la dreta de DATA és el botó ZOOM (només en aquest mòdul): taronja i amb el rètol
                if (_byId.TryGetValue("PED_MCDU1_BLANK1", out var zk) && zk is OhKey zkey) { zkey.Label = "ZOOM"; zkey.Orange = true; zkey.MaxText = 11f; }
                return;
            }
            Mcdu("PED_MCDU1", 0);
            Mcdu("PED_MCDU2", PedMcdu2Dx);

            // ═══════════════ RMP ═══════════════
            void Rmp(string p, float dx, string act, string stby)
            {
                Pp(62 + dx, 866, 595 + dx, 1182, Y(915, 1130), Y(915, 1130));
                Lb("ACTIVE", 190 + dx, 888, 15f); Lb("STBY/CRS", 452 + dx, 888, 15f);
                Seg(p + "_ACTIVE", (118 + dx) * k, 906 * k, 152 * k, 48 * k, act, 6);
                var xf = Key(p + "_XFER", (288 + dx) * k, 902 * k, 56 * k, 56 * k, "");
                xf.Arrows = true; xf.Momentary = true;
                Seg(p + "_STBY", (383 + dx) * k, 906 * k, 152 * k, 48 * k, stby, 6);
                xf.OnChange = _ =>
                {
                    var a = (OhSeg7)_byId[p + "_ACTIVE"]; var s = (OhSeg7)_byId[p + "_STBY"];
                    string t = a.Text; SetDisplay(p + "_ACTIVE", s.Text); SetDisplay(p + "_STBY", t);
                };
                void RKey(string id, float cx, float cy, string label, string? group, bool on = false)
                {
                    var kk = PK(id, cx + dx, cy, 58, 32, label, null, 12f, false);
                    kk.Led = true; kk.Group = group; kk.On = on;
                }
                RKey(p + "_VHF1", 117, 1000, "VHF1", p + "COM", true);
                RKey(p + "_VHF2", 186, 1000, "VHF2", p + "COM");
                RKey(p + "_VHF3", 259, 1000, "VHF3", p + "COM");
                RKey(p + "_HF1", 117, 1052, "HF1", p + "COM");
                Round(p + "_SEL", (186 + dx) * k, 1048 * k, 9.5f);
                Lb("SEL", 186 + dx, 1030, 8.2f);
                RKey(p + "_HF2", 259, 1052, "HF2", p + "COM");
                RKey(p + "_AM", 328, 1052, "AM", null);
                float[] dk = new float[12];
                for (int i = 0; i < 12; i++) dk[i] = i * 30f;
                Kn(p + "_TUNING_KNOB", 422 + dx, 1036, 28f, 0, dk, KnobStyle.Round).Wrap = true;
                _silk.Add(new OhTitle("STBY NAV", (316 + dx) * k, 1097 * k, (110 + dx) * k, (500 + dx) * k, 9f));
                RKey(p + "_NAV", 137, 1144, "NAV", null);
                RKey(p + "_VOR", 205, 1144, "VOR", p + "NAV");
                RKey(p + "_ILS", 270, 1144, "ILS", p + "NAV");
                RKey(p + "_MLS", 337, 1144, "MLS", p + "NAV");
                RKey(p + "_ADF", 402, 1144, "ADF", p + "NAV");
                RKey(p + "_BFO", 468, 1144, "BFO", null);
                Lb("ON", 532 + dx, 1110, 9f); Lb("OFF", 532 + dx, 1166, 9f);
                TogS(p + "_ON_OFF", (532 + dx) * k, 1138 * k, 2, 0, ToggleStyle.Bat, 10f);
            }
            Rmp("PED_RMP1", 0, "123.750", "132.500");
            Rmp("PED_RMP2", 1334, "122.500", "132.750");

            // ═══════════════ ACP ═══════════════
            void Acp(string p, float dx)
            {
                Pp(62 + dx, 1190, 595 + dx, 1496, Y(1265, 1440), Y(1265, 1440));
                float[] xs = { 126, 192, 258, 324, 390, 456, 522 };
                string[] tx = { "VHF1", "VHF2", "VHF3", "HF1", "HF2", "INT", "CAB" };
                for (int i = 0; i < 7; i++)
                {
                    string cap = i < 5 ? "CALL" : i == 5 ? "MECH" : "ATT";
                    var kk = Key(p + "_CALL_" + tx[i], (xs[i] - 29 + dx) * k, 1207 * k, 58 * k, 30 * k, cap);
                    kk.LitText = true; kk.MaxText = 11f;
                    Lb(tx[i], xs[i] + dx, 1254, 10f);
                    Vol(p + "_VOL_" + tx[i], (xs[i] + dx) * k, 1294 * k, 15f);
                }
                _silk.Add(new OhBlock(R(70 + dx, 1335, 88 + dx, 1398), Color.FromArgb(44, 48, 53), Color.FromArgb(20, 22, 25), 2f));
                Vt("VOICE", 101 + dx, 1366, 8.2f);
                Lb("INT", 128 + dx, 1326, 9f); Lb("RAD", 128 + dx, 1388, 9f);
                TogS(p + "_INT_RAD", (128 + dx) * k, 1356 * k, 3, 1, ToggleStyle.Bat, 10f);
                Vt("LS", 190 + dx, 1362, 8.2f);
                Vol(p + "_VOL_LS", (226 + dx) * k, 1362 * k, 15f);
                Vol(p + "_VOL_MKR", (291 + dx) * k, 1362 * k, 15f);
                Vt("MKR", 330 + dx, 1362, 8.2f);
                foreach (var (id, x) in new[] { ("_CALL_X1", 383f), ("_CALL_X2", 447f) })
                {
                    var kk = Key(p + id, (x - 28 + dx) * k, 1360 * k, 56 * k, 30 * k, "CALL");
                    kk.LitText = true; kk.MaxText = 11f;
                }
                var pa = Key(p + "_PA", (488 + dx) * k, 1358 * k, 52 * k, 32 * k, ""); pa.Momentary = true;
                Vt("RESET", 552 + dx, 1362, 8.2f);
                _silk.Add(new OhBlock(R(563 + dx, 1335, 581 + dx, 1398), Color.FromArgb(44, 48, 53), Color.FromArgb(20, 22, 25), 2f));
                Wl(352 + dx, 1408, 352 + dx, 1400, 480 + dx, 1400, 480 + dx, 1408);
                Wl(488 + dx, 1408, 488 + dx, 1400, 545 + dx, 1400, 545 + dx, 1408);
                Lb("1 - VOR - 2", 158 + dx, 1422, 10f); Lb("1 - ADF - 2", 290 + dx, 1422, 10f);
                Lb("1 - SAT - 2", 420 + dx, 1422, 10f); Lb("PA", 520 + dx, 1422, 10f);
                string[] nav = { "VOR1", "VOR2", "ADF1", "ADF2", "SAT1", "SAT2", "PA" };
                for (int i = 0; i < 7; i++) Vol(p + "_VOL_" + nav[i], (xs[i] + dx) * k, 1456 * k, 15f);
            }
            Acp("PED_ACP1", 0);
            Acp("PED_ACP2", 1334);

            // ═══════════════ FLOOD LT / INTEG LT (capità) i FLOOD LT PED / AIDS / DFDR (primer oficial) ═══════════════
            Pp(62, 1504, 595, 1672, Y(1545), Y(1615));
            Lb("FLOOD LT", 175, 1525, 14f); Lb("MAIN PNL", 175, 1547, 10f);
            Kn("PED_FLOOD_LT_MAIN", 178, 1598, 19f, 0, Brt, KnobStyle.Round);
            Lb("OFF", 138, 1640, 9f); Lb("BRT", 222, 1640, 9f);
            Lb("INTEG LT", 320, 1525, 14f); Lb("MAIN PNL & PED", 325, 1547, 10f);
            Kn("PED_INTEG_LT_MAIN", 320, 1598, 19f, 0, Brt, KnobStyle.Round);
            Lb("OFF", 284, 1640, 9f); Lb("BRT", 368, 1640, 9f);

            Pp(1396, 1504, 1929, 1672, Y(1545), Y(1615));
            Lb("FLOOD LT", 1510, 1522, 14f); Lb("PED", 1510, 1543, 10f);
            Kn("PED_FLOOD_LT_PED", 1513, 1592, 19f, 0, Brt, KnobStyle.Round);
            Lb("OFF", 1474, 1640, 9f); Lb("BRT", 1556, 1640, 9f);
            Wl(1588, 1514, 1588, 1652); Wl(1743, 1514, 1743, 1652);
            Lb("AIDS", 1667, 1522, 13f); Lb("PRINT", 1667, 1545, 10f);
            Round("PED_AIDS_PRINT", 1667 * k, 1606 * k, 13f);
            Lb("DFDR", 1813, 1522, 13f); Lb("EVENT", 1813, 1545, 10f);
            Round("PED_DFDR_EVENT", 1813 * k, 1606 * k, 13f);

            // ═══════════════ RADAR METEOROLÒGIC ═══════════════
            Pp(62, 1682, 595, 1930, Y(1730, 1875), Y(1730, 1875));
            Lb("GAIN", 167, 1698, 11f); Lb("MULTISCAN", 278, 1698, 11f); Lb("GCS", 387, 1698, 11f); Lb("TLT", 467, 1698, 11f);
            Kn("PED_WX_GAIN", 157, 1787, 19f, 4, new float[] { -140, -105, -70, -30, 0, 30, 70, 105, 140 }, KnobStyle.Fluted);
            foreach (var (t, x, y) in new[] { ("CAL", 162f, 1727f), ("-4", 120f, 1740f), ("+4", 205f, 1740f), ("-8", 104f, 1770f), ("+8", 230f, 1770f),
                                              ("-12", 108f, 1806f), ("+12", 236f, 1806f), ("MIN", 126f, 1840f), ("MAX", 212f, 1840f) })
                Lb(t, x, y, 9f);
            Lb("MAN", 214, 1722, 9f); HTog("PED_WX_MULTISCAN", 252, 1722, 2, 1); Lb("AUTO", 297, 1722, 9f);
            Lb("OFF", 348, 1722, 9f); HTog("PED_WX_GCS", 384, 1722, 2, 1); Lb("AUTO", 427, 1722, 9f);
            Lb("MODE", 330, 1756, 10f); Lb("WX+T+HZD", 330, 1774, 9f); Lb("WX+T", 283, 1790, 9f); Lb("MAP", 377, 1790, 9f); Lb("WX", 268, 1836, 9f);
            Kn("PED_WX_MODE", 330, 1830, 22f, 0, new float[] { -90, -40, 0, 40 });
            Kn("PED_WX_TILT", 467, 1787, 19f, 0, new float[] { -90, -15, 15, 45, 135, 165, 195 }, KnobStyle.Fluted);
            foreach (var (t, x, y) in new[] { ("0", 405f, 1788f), ("5", 452f, 1726f), ("10", 488f, 1728f), ("15", 518f, 1742f), ("UP", 527f, 1758f),
                                              ("DN", 527f, 1798f), ("15", 526f, 1818f), ("10", 490f, 1849f), ("5", 452f, 1851f) })
                Lb(t, x, y, 9f);
            Lb("OFF", 219, 1858, 9f); Lb("1", 186, 1884, 9f); HTog("PED_WX_SYS", 219, 1884, 3, 1); Lb("2", 252, 1884, 9f); Lb("SYS", 219, 1910, 9f);
            Lb("OFF", 394, 1884, 9f); HTog("PED_WX_PWS", 438, 1884, 2, 1); Lb("AUTO", 492, 1884, 9f); Lb("PWS", 438, 1910, 9f);

            // ═══════════════ ATC / TCAS ═══════════════
            Pp(1396, 1682, 1929, 1930, Y(1735, 1880), Y(1735, 1880));
            Vt("ATC", 1413, 1808, 9f); Vt("TCAS", 1912, 1800, 9f);
            Lb("STBY", 1441, 1705, 9f); Lb("AUTO", 1483, 1694, 9f); Lb("ON", 1523, 1705, 9f);
            Kn("PED_ATC_MODE", 1484, 1738, 12f, 1, new float[] { -40, 0, 40 });
            Lb("1", 1460, 1783, 9f); Lb("2", 1510, 1783, 9f);
            Kn("PED_ATC_SYS", 1485, 1811, 12f, 0, new float[] { -30, 30 });
            Lb("OFF", 1447, 1855, 9f); Lb("ON", 1518, 1855, 9f); Lb("ALT RPTG", 1483, 1914, 9f);
            Kn("PED_ATC_ALT_RPTG", 1485, 1882, 12f, 1, new float[] { -30, 30 });
            Lb("ATC", 1568, 1713, 9f); _silk.Add(new OhLed(1600 * k, 1713 * k, 9f * k, false)); Lb("FAIL", 1634, 1713, 9f);
            string[] atk = { "1", "2", "3", "4", "5", "6", "7", "0", "CLR" };
            for (int i = 0; i < 9; i++) RK("PED_ATC_KEY_" + atk[i], 1545 + (i % 3) * 55, 1757 + (i / 3) * 68, 20, atk[i]);
            Seg("PED_ATC_CODE", 1685 * k, 1705 * k, 181 * k, 68 * k, "4103", 4);
            Round("PED_ATC_IDENT", 1744 * k, 1813 * k, 8.5f); Lb("IDENT", 1792, 1813, 9f);
            Lb("THRT", 1694, 1880, 9f); Lb("ALL", 1718, 1858, 9f); Lb("ABV", 1764, 1858, 9f); Lb("BLW", 1786, 1880, 9f);
            Kn("PED_TCAS_TRAFFIC", 1740, 1890, 12f, 1, new float[] { -60, -20, 20, 60 });
            Lb("STBY", 1802, 1858, 9f); Lb("TA", 1835, 1858, 9f); Lb("TA/RA", 1881, 1858, 9f);
            Kn("PED_TCAS_MODE", 1835, 1890, 12f, 2, new float[] { -40, 0, 40 });

            // ═══════════════ SWITCHING ═══════════════
            Pp(600, 30, 1390, 272, Y(75, 215), Y(75, 215));
            Lb("SWITCHING", 990, 48, 14f);
            string[] swN = { "ATT HDG", "AIR DATA", "EIS DMC", "ECAM / ND XFR" };
            string[] swId = { "ATT_HDG", "AIR_DATA", "EIS_DMC", "ECAM_ND_XFR" };
            float[] swX = { 746, 912, 1079, 1244 };
            for (int i = 0; i < 4; i++)
            {
                float x = swX[i];
                Lb(swN[i], x, 81, 11f); Lb("NORM", x, 99, 11f);
                Lb("CAPT", x - 55, 117, 10f); Lb("F/O", x + 58, 117, 10f);
                if (i < 3) { Lb("3", x - 60, 134, 9f); Lb("3", x + 62, 134, 9f); }
                _silk.Add(new OhArcDial(x * k, 196 * k, 54 * k, -50, 50, new float[] { -45, 0, 45 }, false));
                Kn("PED_SW_" + swId[i], x, 196, 27f, 1, new float[] { -45, 0, 45 });
            }

            // ═══════════════ ECAM: lluminositat de les pantalles i panell de control ═══════════════
            Pp(600, 288, 768, 553, Y(330, 465));
            Lb("ECAM", 695, 322, 14f);
            Lb("UPPER DISPLAY", 697, 344, 9f);
            _silk.Add(new OhArrowArc(690 * k, 392 * k, 37 * k, -135, 135));
            Kn("PED_ECAM_UPPER_BRT", 690, 392, 16f, 4, Brt, KnobStyle.Round);
            Lb("OFF", 650, 425, 9f); Lb("BRT", 742, 425, 9f);
            Lb("LOWER DISPLAY", 697, 455, 9f);
            _silk.Add(new OhArrowArc(690 * k, 502 * k, 37 * k, -135, 135));
            Kn("PED_ECAM_LOWER_BRT", 690, 502, 16f, 4, Brt, KnobStyle.Round);
            Lb("OFF", 650, 537, 9f); Lb("BRT", 742, 537, 9f);

            Pp(775, 288, 1390, 553, null, Y(330, 465));
            {
                string?[,] grid =
                {
                    { "", "T.O|CONFIG", "", null, "EMER|CANC", null, "" },
                    { "ENG", "BLEED", "PRESS", "ELEC", "HYD", "FUEL", "" },
                    { "APU", "COND", "DOOR", "WHEEL", "F/CTL", "ALL", "" },
                    { "CLR", "", "STS", "RCL", "", "CLR", "" }
                };
                float[] gy = { 325, 393, 455, 516 };
                string[] page = { "ENG", "BLEED", "PRESS", "ELEC", "HYD", "FUEL", "APU", "COND", "DOOR", "WHEEL", "F/CTL", "STS" };
                for (int r = 0; r < 4; r++)
                    for (int c = 0; c < 7; c++)
                    {
                        var s = grid[r, c];
                        if (s == null) continue;
                        float x = 845 + c * 68, y = gy[r];
                        if (s == "") { _silk.Add(new OhBlankCap(R(x - 28, y - 20, x + 28, y + 20))); continue; }
                        var parts = s.Split('|');
                        string id = "PED_ECP_" + parts[0].Replace("/", "").Replace(".", "") + (parts.Length > 1 ? parts[1] : "") + (s == "CLR" ? (c == 0 ? "_L" : "_R") : "");
                        bool isPage = page.Contains(s);
                        var kk = PK(id, x, y, 56, 40, parts[0], parts.Length > 1 ? parts[1] : null, 11f, !isPage);
                        if (isPage || s == "CLR" || s == "ALL" || s == "RCL" || s == "T.O|CONFIG" || s == "EMER|CANC") { kk.Led = true; kk.LedWide = true; }
                        if (isPage) kk.Group = "ECAM_PAGE";
                    }
            }

            // planxes llises entre l'ECAM i el quadrant de gas
            Pp(600, 562, 1390, 652, Y(605), Y(605));
            Pp(600, 668, 790, 1960); Pp(1195, 668, 1390, 1960);   // columnes de les rodes del trim: una sola planxa fins a baix
            Pp(790, 668, 1195, 830); Pp(790, 830, 1195, 895);

            // ═══════════════ QUADRANT DE GAS, RODES DEL TRIM I ESCALES DEL THS ═══════════════
            _silk.Add(new OhThsScale(R(700, 905, 790, 1600), true, k));
            _silk.Add(new OhThsScale(R(1195, 905, 1285, 1600), false, k));
            _silk.Add(new OhTrimWheel(R(605, 820, 690, 1680)));
            _silk.Add(new OhTrimWheel(R(1295, 820, 1380, 1680)));
            // mateix element que el quadrant opcional del panell principal (TH): també mou els motors del simulador
            _throttle = Add(new OhThrottle("MIP_THROTTLE", R(790, 787.2f, 1195, 1532.6f)) { Shown = true });
            Pp(790, 1612, 1195, 1672);
            Lb("1", 909, 1644, 12f); Lb("2", 1086, 1644, 12f);

            // ═══════════════ ENG ═══════════════
            Pp(795, 1675, 1192, 1960, null, null, Y(1693, 1941), Y(1693, 1941));   // ENG: estrelles grans

            Lb("ENG", 1003, 1700, 15f);
            Lb("MASTER 1", 917, 1702, 9f); Lb("MASTER 2", 1095, 1702, 9f);
            Add(new OhEngMaster("PED_ENG_MASTER_1", R(865, 1714, 965, 1788), "1"));
            Add(new OhEngMaster("PED_ENG_MASTER_2", R(1043, 1714, 1143, 1788), "2"));
            Lb("ON", 850, 1748, 9f); Lb("OFF", 852, 1800, 9f);
            Lb("ON", 1158, 1748, 9f); Lb("OFF", 1158, 1800, 9f);
            Lb("MODE", 997, 1783, 9f); Lb("NORM", 997, 1798, 9f);
            Lb("CRANK", 920, 1834, 9f); Lb("IGN", 1062, 1815, 9f); Lb("START", 1066, 1830, 9f);
            _silk.Add(new OhArcDial(995 * k, 1886 * k, 60 * k, -55, 55, new float[] { -50, 0, 50 }, false));
            Kn("PED_ENG_MODE", 995, 1886, 30f, 1, new float[] { -50, 0, 50 });
            Add(new OhAnnunciator("PED_ENG_FIRE_1", R(848, 1846, 912, 1914), "FIRE", "FAULT"));
            Add(new OhAnnunciator("PED_ENG_FIRE_2", R(1078, 1846, 1142, 1914), "FIRE", "FAULT"));
            Lb("1", 880, 1932, 10f); Lb("2", 1110, 1932, 10f);

            // ═══════════════ SPEED BRAKE ═══════════════
            Pp(272, 1966, 797, 2330, null, null, Y(2015, 2290), Y(2030, 2290));   // SPEED BRAKE: estrelles grans
            Lb("SPEED", 443, 2143, 14f); Lb("BRAKE", 443, 2167, 14f);
            _silk.Add(new OhBoxLabel("GND SPLRS ARMED", 668 * k, 1993 * k, 9f, Oh.Silk));
            Wl(610, 2258, 622, 2258, 622, 2030); Wl(610, 2105, 622, 2105); Wl(610, 2159, 622, 2159); Wl(610, 2210, 622, 2210);
            Wl(734, 2258, 722, 2258, 722, 2030); Wl(734, 2105, 722, 2105); Wl(734, 2159, 722, 2159); Wl(734, 2210, 722, 2210);
            _silk.Add(new OhArrow(622 * k, 2026 * k, -90, 3.2f, Oh.Silk));
            _silk.Add(new OhArrow(722 * k, 2026 * k, -90, 3.2f, Oh.Silk));
            Lb("RET", 585, 2066, 11f); Lb("RET", 760, 2066, 11f);
            Lb("1/2", 585, 2159, 11f); Lb("1/2", 760, 2159, 11f);
            Lb("FULL", 578, 2258, 11f); Lb("FULL", 766, 2258, 11f);
            Add(new OhSpeedBrake("PED_SPEEDBRAKE", 672 * k, 2015 * k, 2290 * k, 17 * k, 1925 * k, 2232 * k, 120 * k, 92 * k));

            // ═══════════════ RUD TRIM i PARKING BRK ═══════════════
            Pp(810, 1970, 1145, 2750, Y(2018, 2715), Y(2018, 2715));
            Wl(822, 2155, 1135, 2155); Wl(822, 2390, 1135, 2390); Wl(822, 2525, 1135, 2525);
            Seg("PED_RUD_TRIM_DISP", 850 * k, 2194 * k, 116 * k, 66 * k, "L0.0", 3);
            Lb("+/-", 830, 2216, 9f); Lb("20°", 830, 2240, 9f);
            Lb("RUD TRIM", 1044, 2177, 14f);
            Lb("NOSE", 1000, 2201, 9f); Lb("NOSE", 1088, 2201, 9f);
            Wl(1024, 2201, 1064, 2201);
            Lb("L", 992, 2232, 10f); Lb("R", 1098, 2232, 10f);
            var rt = Kn("PED_RUD_TRIM", 1044, 2310, 38f, 1, new float[] { -35, 0, 35 });
            rt.SpringCenter = true; rt.MomentaryReturn = 1;
            Lb("RESET", 905, 2297, 10f);
            Round("PED_RUD_TRIM_RESET", 907 * k, 2336 * k, 14f);
            Lb("PARKING BRK", 910, 2540, 14f); Lb("PULL & TURN", 893, 2564, 10f);
            Lb("OFF", 1033, 2541, 9f);
            _silk.Add(new OhArcDial(1033 * k, 2668 * k, 112 * k, 0, 95, Array.Empty<float>(), false));
            Add(new OhParkBrake("PED_PARK_BRK", 1033 * k, 2672 * k, 232 * k, 64 * k, 80 * k));

            // ═══════════════ FLAPS ═══════════════
            Pp(1154, 1975, 1690, 2336, null, null, Y(2025, 2305), Y(2025, 2305));   // FLAPS: estrelles grans
            Lb("FLAPS", 1471, 2178, 15f);
            Add(new OhFlapsLever("PED_FLAPS", R(1204, 2030, 1364, 2322), new[] { 2046f, 2108f, 2166f, 2230f, 2292f }.Select(y => y * k).ToArray(),
                                 new[] { "0", "1", "2", "3", "FULL" }, 112 * k, 128 * k));

            // ═══════════════ COCKPIT DOOR ═══════════════
            Pp(272, 2345, 797, 2547, Y(2400), Y(2400));
            Lb("COCKPIT DOOR", 443, 2384, 14f);
            Add(new OhAnnunciator("PED_COCKPIT_DOOR", R(330, 2437, 392, 2502), "FAULT", "OFF"));
            TogS("PED_COCKPIT_DOOR_SW", 452 * k, 2465 * k, 3, 1, ToggleStyle.Metal, 12f);
            Lb("UNLOCK", 535, 2432, 8.2f); Lb("NORM", 527, 2463, 8.2f); Lb("LOCK", 527, 2494, 8.2f);
            Pp(272, 2555, 800, 2987, Y(2607, 2940), Y(2607, 2940));

            // ═══════════════ GRAVITY GEAR EXTN ═══════════════
            Pp(808, 2755, 1149, 2987);
            Vt("GEAR", 833, 2862, 11f); Vt("GEAR", 1121, 2862, 11f);
            _silk.Add(new OhArrowArc(976 * k, 2878 * k, 124 * k, 245, 295));
            _silk.Add(new OhArrowArc(976 * k, 2878 * k, 124 * k, 65, 115));
            Add(new OhGravityGear("PED_GRAVITY_GEAR", 976 * k, 2878 * k, 104 * k, k));

            // ═══════════════ part de darrere: barra cromada, safata, SLEW ═══════════════
            Pp(1154, 2345, 1696, 2560, Y(2510), Y(2510));
            _silk.Add(new OhChromeBar(R(1194, 2352, 1656, 2472)));
            _silk.Add(new OhSlotPlate(R(1196, 2478, 1655, 2546)));
            Pp(1154, 2565, 1696, 2987, Y(2895), Y(2895));
            _silk.Add(new OhPenBar(R(1205, 2836, 1638, 2864)));
            PK("PED_SLEW", 1290, 2953, 56, 34, "SLEW", null, 9f);
            _silk.Add(new OhBall(1430 * k, 2955 * k, 20 * k));

            // sòcols de la part de baix (estriats)
            _silk.Add(new OhRibbed(R(262, 2995, 880, 3100)));
            _silk.Add(new OhRibbed(R(1074, 2995, 1695, 3100)));

            // ═══════════════ cargols que no són DZUS (posicions de la imatge de referència) ═══════════════
            // SWITCHING i panell de tecles de l'ECAM
            ScP(1295, 48, 673, 243);
            ScP(802, 300, 1070, 300, 1300, 299, 790, 372, 790, 465);
            // sobre el throttle i a sota de les rodes del trim
            ScP(755, 696, 808, 696, 1179, 697, 1236, 696, 755, 793, 808, 793, 1178, 793, 1236, 793);
            ScP(753, 1668, 1233, 1668, 753, 1766, 1235, 1766);
            // FLOOD LT / INTEG LT i radar
            ScP(513, 1622, 120, 1712, 120, 1878, 520, 1713, 520, 1879);
            // franges de sobre el throttle i "1 2": cargols de ranura grans, sense osca
            ScL(820, 860, 1165, 860, 820, 1642, 1165, 1642);
            // SPEED BRAKE
            ScS(365, 1992, 375, 2297, 767, 2110, 767, 2218);
            ScP(522, 2070, 522, 2235);
            // COCKPIT DOOR
            ScS(335, 2375, 583, 2375, 731, 2525);
            // RUD TRIM / PARK BRK
            ScS(975, 1990, 865, 2410, 1075, 2410, 925, 2722);
            // GRAVITY GEAR: estrelles grans a les quatre cantonades
            ScX(840, 2780, 1105, 2780, 840, 2945, 1105, 2945);
            // FLAPS
            ScP(1183, 2080, 1183, 2220, 1604, 2001, 1603, 2318);
        }
    }

    // ═══════════════════════════════ SERIGRAFIA I PECES FIXES ═══════════════════════════════

    /// <summary>Posició buida d'una tecla (ECAM): quadrat del color de la planxa amb el cantell marcat.</summary>
    /// <summary>
    /// Planxa de la cara de la MCDU de l'esquerra (px de referència × k). Meitat de dalt: vores amb les osques DZUS
    /// (completa a la de dalt; a la del mig només la meitat de dalt de l'arc, i des del punt més endins baixa recte per
    /// esquivar la frontissa). Un cop passada la frontissa fa un tomb cap enfora per incloure les tires FAIL/FMGC i
    /// MCDU MENU, i després ressegueix per dins la nansa fins al tram de baix.
    /// </summary>
    internal sealed class OhMcduFace : OhSilk
    {
        // vores de la cara i DZUS (mateixa mida i osca que a l'overhead: radi del vis 8,4, osca ×1,28, arrodoniment fix)
        const float X0 = 72f, X1 = 585f, Top = 36f, Rc = 4f;
        readonly float _k, _dx; readonly RectangleF _rect;
        /// <param name="dx">desplaçament horitzontal en px de referència (0 la MCDU esquerra, 1334 la dreta)</param>
        public OhMcduFace(float k, float dx = 0)
        {
            _k = k; _dx = dx;
            _rect = RectangleF.FromLTRB((X0 + dx) * k, Top * k, (X1 + dx) * k, 842 * k);
            Bounds = RectangleF.Inflate(_rect, 6, 6);
        }
        GraphicsPath MakePath()
        {
            float k = _k;
            float R = 8.4f * 1.28f / k, f = 5.6f * 1.28f / 2f / k, din = (8.4f - 1.5f) / k;   // en px de referència
            float xL = X0 + din, xR = X1 - din, ixL = xL + R, ixR = xR - R;
            const float yT = 75f, yM = 453f;
            var p = new GraphicsPath();
            p.AddArc(X0, Top, 2 * Rc, 2 * Rc, 180, 90);
            p.AddArc(X1 - 2 * Rc, Top, 2 * Rc, 2 * Rc, 270, 90);
            // dreta: osca completa del DZUS de dalt
            p.AddArc(X1 - 2 * f, yT - R - 2 * f, 2 * f, 2 * f, 0, 90);
            p.AddArc(xR - R, yT - R, 2 * R, 2 * R, 270, -180);
            p.AddArc(X1 - 2 * f, yT + R, 2 * f, 2 * f, 270, 90);
            // dreta: DZUS del mig, només la meitat de dalt de l'arc; després recte avall per dins de la frontissa
            p.AddArc(X1 - 2 * f, yM - R - 2 * f, 2 * f, 2 * f, 0, 90);
            p.AddArc(xR - R, yM - R, 2 * R, 2 * R, 270, -90);
            p.AddLine(ixR, yM, ixR, 564f);
            // tomb cap enfora (la tira MCDU MENU queda a dins) i per dins del braç de la nansa
            p.AddLine(ixR, 564f, 578.5f, 572f);
            p.AddLine(578.5f, 572f, 578.5f, 771f);
            p.AddLine(578.5f, 771f, 546.5f, 795f);
            p.AddLine(546.5f, 795f, 546.5f, 834f);
            p.AddArc(546.5f - 2 * Rc, 834f - Rc, 2 * Rc, 2 * Rc, 0, 90);
            // tram de baix (per dins del tram horitzontal de la nansa)
            p.AddArc(110.5f, 834f - Rc, 2 * Rc, 2 * Rc, 90, 90);
            p.AddLine(110.5f, 834f, 110.5f, 795f);
            p.AddLine(110.5f, 795f, 78.5f, 771f);
            p.AddLine(78.5f, 771f, 78.5f, 572f);
            // tomb cap endins (la tira FAIL/FMGC queda a dins) i recte amunt per dins de la frontissa
            p.AddLine(78.5f, 572f, ixL, 564f);
            p.AddLine(ixL, 564f, ixL, yM);
            // esquerra: DZUS del mig, meitat de dalt de l'arc
            p.AddArc(xL - R, yM - R, 2 * R, 2 * R, 0, -90);
            p.AddArc(X0, yM - R - 2 * f, 2 * f, 2 * f, 90, 90);
            // esquerra: osca completa del DZUS de dalt
            p.AddArc(X0, yT + R, 2 * f, 2 * f, 180, 90);
            p.AddArc(xL - R, yT - R, 2 * R, 2 * R, 90, -180);
            p.AddArc(X0, yT - R - 2 * f, 2 * f, 2 * f, 90, 90);
            p.CloseFigure();
            using var m = new Matrix(k, 0, 0, k, _dx * k, 0);
            p.Transform(m);
            return p;
        }
        public override void Draw(Graphics g)
        {
            // el mateix relleu que les planxes: ombra, filet fosc i de llum, degradat i gra
            using var path = MakePath();
            Oh.SoftShadow(g, path, 1.8f, 2.6f, 4f, 140);
            var st = g.Save();
            g.TranslateTransform(1.0f, 1.0f);
            using (var lo = new Pen(Oh.PlateLo, 1.4f)) g.DrawPath(lo, path);
            g.Restore(st); st = g.Save();
            g.TranslateTransform(-0.8f, -0.8f);
            using (var hi = new Pen(Oh.PlateHi, 1.4f)) g.DrawPath(hi, path);
            g.Restore(st);
            using (var fill = new LinearGradientBrush(new RectangleF(_rect.X, _rect.Y - 1, _rect.Width, _rect.Height + 2), Oh.PlateA, Oh.PlateB, 90f))
                g.FillPath(fill, path);
            Oh.Grain(g, path);
        }
    }

    /// <summary>Pantalla de la MCDU enfonsada dins la planxa de la cara: forat, quatre parets còniques i la pantalla al fons.</summary>
    internal sealed class OhMcduWell : OhSilk
    {
        readonly RectangleF _outer, _glass; readonly float _ro, _rg;
        public OhMcduWell(RectangleF outer, RectangleF glass, float rOuter, float rGlass)
        {
            _outer = outer; _glass = glass; _ro = rOuter; _rg = rGlass;
            Bounds = RectangleF.Inflate(outer, 3, 3);
        }
        public override void Draw(Graphics g)
        {
            var o = _outer; var i = _glass;
            using var op = Oh.RoundRect(o, _ro);
            var st = g.Save();
            g.SetClip(op);
            // quatre parets inclinades (trapezis entre la vora del forat i la vora de la pantalla), cadascuna amb la seva llum:
            // la de dalt a l'ombra, la de baix i les dels costats més clares
            void Wall(PointF[] q, Color c0, Color c1, float ang)
            {
                var rb = new RectangleF(q.Min(p => p.X) - 0.5f, q.Min(p => p.Y) - 0.5f, q.Max(p => p.X) - q.Min(p => p.X) + 1f, q.Max(p => p.Y) - q.Min(p => p.Y) + 1f);
                using var b = new LinearGradientBrush(rb, c0, c1, ang);
                g.FillPolygon(b, q);
            }
            PointF oTL = new(o.X, o.Y), oTR = new(o.Right, o.Y), oBR = new(o.Right, o.Bottom), oBL = new(o.X, o.Bottom);
            PointF iTL = new(i.X, i.Y), iTR = new(i.Right, i.Y), iBR = new(i.Right, i.Bottom), iBL = new(i.X, i.Bottom);
            Wall(new[] { oTL, oTR, iTR, iTL }, Oh.T(70, 78, 86, 64, 76, 88), Oh.T(36, 40, 45, 32, 40, 48), 90f);          // dalt
            Wall(new[] { oBL, iBL, iBR, oBR }, Oh.T(150, 160, 170, 140, 160, 178), Oh.T(186, 194, 202, 176, 194, 210), 90f); // baix
            Wall(new[] { oTL, iTL, iBL, oBL }, Oh.T(168, 177, 186, 158, 177, 194), Oh.T(128, 138, 148, 120, 138, 156), 0f);  // esquerra
            Wall(new[] { iTR, oTR, oBR, iBR }, Oh.T(120, 130, 140, 112, 130, 148), Oh.T(150, 160, 170, 140, 160, 178), 0f);  // dreta
            // arestes entre parets (diagonals), molt suaus
            using (var ep = new Pen(Color.FromArgb(50, 20, 24, 28), 0.6f))
            {
                g.DrawLine(ep, oTL, iTL); g.DrawLine(ep, oTR, iTR); g.DrawLine(ep, oBL, iBL); g.DrawLine(ep, oBR, iBR);
            }
            g.Restore(st);
            // vora del forat a la planxa: fil fosc a dalt-esquerra (on la planxa cau) i fil de llum a baix-dreta
            st = g.Save(); g.TranslateTransform(0.7f, 0.7f);
            using (var hp = new Pen(Color.FromArgb(150, Oh.PlateHi), 0.9f)) g.DrawPath(hp, op);
            g.Restore(st);
            using (var lp = new Pen(Color.FromArgb(220, Oh.PlateLo), 0.9f)) g.DrawPath(lp, op);
            // pantalla al fons (mateixa mida i lloc de sempre: hi van els popouts)
            using (var gp = Oh.RoundRect(i, _rg))
            {
                using (var gb = new SolidBrush(Color.FromArgb(6, 7, 8))) g.FillPath(gb, gp);
                st = g.Save(); g.SetClip(gp);
                // pantalla totalment negra (sense reflex: el text ha de quedar sobre negre uniforme)
                g.Restore(st);
                using var pe = new Pen(Color.FromArgb(200, 10, 12, 14), 0.8f); g.DrawPath(pe, gp);
            }
        }
    }

    /// <summary>
    /// Nansa d'extracció de la MCDU de l'esquerra (px de referència × k): frontisses a l'alçada d'AIR PORT, dos braços
    /// que baixen per les vores, es desvien cap a dins per esquivar els DZUS de baix, i un tram horitzontal per sota
    /// de la darrera fila de tecles.
    /// </summary>
    internal sealed class OhMcduHandle : OhSilk
    {
        readonly float _k; readonly PointF[] _line; readonly RectangleF _hl, _hr;
        /// <param name="dx">desplaçament horitzontal en px de referència (0 la MCDU esquerra, 1334 la dreta)</param>
        public OhMcduHandle(float k, float dx = 0)
        {
            _k = k;
            PointF P(float x, float y) => new((x + dx) * k, y * k);
            _line = Oh.RoundAll(new[]
            {
                P(72, 478), P(72, 776), P(104, 800), P(104, 848), P(553, 848), P(553, 800), P(585, 776), P(585, 478)
            }, 10f * k);
            // RoundAll tanca el polígon: es treuen els punts de l'arrodoniment entre l'últim i el primer (no hi ha tram de dalt)
            // (els braços pugen fins al capdamunt de les frontisses)
            _line = TrimClosing(_line, P(72, 478), P(585, 478));
            // frontisses estretes, entre el braç i la vora vertical de la planxa de la cara
            _hl = RectangleF.FromLTRB((77 + dx) * k, 478 * k, (91 + dx) * k, 562 * k);
            _hr = RectangleF.FromLTRB((566 + dx) * k, 478 * k, (580 + dx) * k, 562 * k);
            Bounds = RectangleF.FromLTRB((60 + dx) * k, 470 * k, (598 + dx) * k, 856 * k);
        }
        static PointF[] TrimClosing(PointF[] pts, PointF a, PointF b)
        {
            // es queda amb el tram que va d'a (inici del braç esquerre) fins a b (final del braç dret)
            int ia = Nearest(pts, a), ib = Nearest(pts, b);
            var o = new List<PointF>();
            for (int i = ia; ; i = (i + 1) % pts.Length) { o.Add(pts[i]); if (i == ib) break; }
            o[0] = a; o[^1] = b;
            return o.ToArray();
        }
        static int Nearest(PointF[] pts, PointF p)
        {
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < pts.Length; i++) { float dx = pts[i].X - p.X, dy = pts[i].Y - p.Y, d = dx * dx + dy * dy; if (d < bd) { bd = d; best = i; } }
            return best;
        }
        public override void Draw(Graphics g)
        {
            float w = 9f * _k;
            using (var path = new GraphicsPath())
            {
                path.AddLines(_line);
                // ombra, vora fosca, cos de la barra i fil de llum a dalt-esquerra
                var st = g.Save(); g.TranslateTransform(1.6f, 2.2f);
                using (var sh = new Pen(Color.FromArgb(90, 0, 0, 0), w + 2f) { LineJoin = LineJoin.Round, StartCap = LineCap.Flat, EndCap = LineCap.Flat }) g.DrawPath(sh, path);
                g.Restore(st);
                using (var ed = new Pen(Oh.PlateLo, w + 1.4f) { LineJoin = LineJoin.Round }) g.DrawPath(ed, path);
                using (var body = new Pen(Oh.T(128, 142, 156, 120, 146, 168), w) { LineJoin = LineJoin.Round }) g.DrawPath(body, path);
                st = g.Save(); g.TranslateTransform(-w * 0.22f, -w * 0.22f);
                using (var hi = new Pen(Color.FromArgb(150, 225, 232, 240), w * 0.18f) { LineJoin = LineJoin.Round }) g.DrawPath(hi, path);
                g.Restore(st);
            }
            // frontisses (blocs aixecats amb el passador)
            foreach (var r in new[] { _hl, _hr })
            {
                using var p = Oh.RoundRect(r, 3f * _k);
                Oh.SoftShadow(g, p, 1.4f, 2f, 3f, 130);
                using (var b = new LinearGradientBrush(RectangleF.Inflate(r, 0.5f, 0.5f), Oh.T(150, 164, 176, 142, 166, 186), Oh.T(100, 114, 128, 94, 118, 140), 0f)) g.FillPath(b, p);
                using (var pe = new Pen(Oh.PlateLo, 0.8f)) g.DrawPath(pe, p);
                float py = r.Y + r.Height * 0.3f;
                using (var pin = new Pen(Color.FromArgb(160, 30, 36, 42), 1f)) g.DrawLine(pin, r.X + 1.5f, py, r.Right - 1.5f, py);
                using (var pinH = new Pen(Color.FromArgb(120, 230, 236, 242), 0.7f)) g.DrawLine(pinH, r.X + 1.5f, py + 1f, r.Right - 1.5f, py + 1f);
            }
        }
    }

    /// <summary>Llum d'avís de la MCDU apagat: càpsula negra (vidre fosc) encastada, amb un reflex suau a dalt.</summary>
    internal sealed class OhAnnPill : OhSilk
    {
        readonly RectangleF _r;
        public OhAnnPill(RectangleF r) { _r = r; Bounds = RectangleF.Inflate(r, 3, 3); }
        public override void Draw(Graphics g)
        {
            using var p = Oh.RoundRect(_r, _r.Height / 2f);
            // cantell: fosc a dalt-esquerra (encastat), llum a baix-dreta
            var st = g.Save(); g.TranslateTransform(0.6f, 0.7f);
            using (var hp = new Pen(Color.FromArgb(140, Oh.PlateHi), 1.0f)) g.DrawPath(hp, p);
            g.Restore(st);
            using (var b = new LinearGradientBrush(RectangleF.Inflate(_r, 0.5f, 0.5f), Color.FromArgb(10, 11, 13), Color.FromArgb(34, 37, 41), 90f)) g.FillPath(b, p);
            using (var pe = new Pen(Color.FromArgb(220, 8, 9, 10), 0.8f)) g.DrawPath(pe, p);
            // reflex del vidre
            var hl = new RectangleF(_r.X + _r.Height * 0.4f, _r.Y + _r.Height * 0.16f, _r.Width - _r.Height * 0.8f, _r.Height * 0.3f);
            using (var hb = new LinearGradientBrush(RectangleF.Inflate(hl, 0.5f, 0.5f), Color.FromArgb(55, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
            using (var hpth = Oh.RoundRect(hl, hl.Height / 2f)) g.FillPath(hb, hpth);
        }
    }

    /// <summary>
    /// Cercle blanc de la part de dalt de la MCDU: encastat a la planxa (no sobresurt, com els llums d'avís): disc
    /// blanc-gris pla amb el cantell fosc a dalt-esquerra i llum a baix-dreta, sense ombra.
    /// </summary>
    internal sealed class OhWhiteDome : OhSilk
    {
        readonly float _cx, _cy, _r;
        public OhWhiteDome(float cx, float cy, float r) { _cx = cx; _cy = cy; _r = r; Bounds = new RectangleF(cx - r - 3, cy - r - 3, r * 2 + 6, r * 2 + 6); }
        public override void Draw(Graphics g)
        {
            var o = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            // cantell: llum a baix-dreta (fora), disc, i fil fosc a la vora
            using (var hp = new Pen(Color.FromArgb(140, Oh.PlateHi), 1.0f)) g.DrawEllipse(hp, o.X + 0.6f, o.Y + 0.7f, o.Width, o.Height);
            using (var b = new LinearGradientBrush(RectangleF.Inflate(o, 0.5f, 0.5f), Color.FromArgb(196, 200, 204), Color.FromArgb(236, 238, 240), 70f)) g.FillEllipse(b, o);
            // ombra interior a dalt-esquerra (el disc és enfonsat respecte de la planxa)
            var st = g.Save();
            using (var cp = new GraphicsPath()) { cp.AddEllipse(o); g.SetClip(cp); }
            using (var sp = new Pen(Color.FromArgb(90, 20, 24, 28), _r * 0.18f)) g.DrawEllipse(sp, o.X + _r * 0.08f, o.Y + _r * 0.1f, o.Width, o.Height);
            g.Restore(st);
            using (var pe = new Pen(Color.FromArgb(200, 30, 34, 38), 0.8f)) g.DrawEllipse(pe, o);
        }
    }

    /// <summary>
    /// Text de la pantalla d'una MCDU (14 files × 24 columnes, veure McduScreen), dibuixat a sobre del vidre negre.
    /// Lletra monoespaiada; files de label i scont en lletra petita. "#" és la casella buida (requadre), "`" el símbol de grau.
    /// </summary>
    internal sealed class OhMcduText : OhElement
    {
        readonly RectangleF _r;
        public McduScreen? Screen;
        public OhMcduText(string id, RectangleF glass) { Id = id; _r = glass; Bounds = glass; }
        public override bool HitTest(PointF p) => false;
        public override RectangleF DirtyBounds => Bounds;

        static Color ColorOf(char c) => c switch
        {
            'g' => Color.FromArgb(40, 230, 90),
            'b' => Color.FromArgb(60, 200, 235),
            'a' => Color.FromArgb(255, 150, 25),
            'y' => Color.FromArgb(250, 240, 60),
            'm' => Color.FromArgb(255, 90, 255),
            'r' => Color.FromArgb(255, 40, 30),   // vermell (Fenix)
            // els símbols ("[ ]" dels camps per omplir) surten en blau, com a la MCDU real
            's' => Color.FromArgb(60, 200, 235),
            _ => Color.FromArgb(240, 242, 244),
        };

        /// <summary>Color del text (no símbol) més proper a la mateixa fila: primer cap a la dreta, després cap a l'esquerra.</summary>
        static char? NeighbourColor(McduScreen s, int row, int col)
        {
            for (int c = col + 1; c < McduScreen.Cols; c++)
            {
                if (s.Ch[row, c] == ' ') break;
                if (s.Col[row, c] != 's') return s.Col[row, c];
            }
            for (int c = col - 1; c >= 0; c--)
            {
                if (s.Ch[row, c] == ' ') break;
                if (s.Col[row, c] != 's') return s.Col[row, c];
            }
            return null;
        }

        public override void Draw(Graphics g)
        {
            var s = Screen;
            if (s == null) return;
            // graella: la mateixa que posa les tecles laterals (marge vertical McduTextInsetY, 14 files iguals); marge
            // horitzontal petit perquè el text no toqui la vora del vidre
            float iy = A320OverheadControl.McduTextInsetY * A320OverheadControl.PedKPublic;
            var r = RectangleF.FromLTRB(_r.Left + _r.Width * 0.035f, _r.Top + iy, _r.Right - _r.Width * 0.035f, _r.Bottom - iy);
            float cw = r.Width / McduScreen.Cols, rh = r.Height / McduScreen.Rows;
            var st = g.Save();
            g.SetClip(_r);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            // lletra com la del ToLiss: gruixuda i estreta (negreta, i s'estreny en horitzontal)
            using var big = new Font("Consolas", rh * 1.12f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var small = new Font("Consolas", rh * 0.86f, FontStyle.Regular, GraphicsUnit.Pixel);
            using var sf = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            for (int row = 0; row < McduScreen.Rows; row++)
                for (int col = 0; col < McduScreen.Cols; col++)
                {
                    char ch = s.Ch[row, col];
                    if (ch == ' ') continue;
                    var color = ColorOf(s.Col[row, col]);
                    bool sm = s.Small[row, col];
                    var cell = new RectangleF(r.X + col * cw, r.Y + row * rh, cw, rh);
                    // símbols del ToLiss (color "s", comprovats a PROG i TAKE OFF): E casella àmbar, A "[", B "]" (blaus)
                    if (s.Col[row, col] == 's' && ch == 'E') { ch = '#'; color = ColorOf('a'); }
                    if (ch == '#')
                    {
                        // casella per omplir: requadre de la mida de la lletra
                        float bh = rh * (sm ? 0.62f : 0.8f), bw = cw * 0.86f;
                        using var bp = new Pen(color, Math.Max(0.8f, rh * 0.07f));
                        g.DrawRectangle(bp, cell.X + (cw - bw) / 2, cell.Y + (rh - bh) / 2, bw, bh);
                        continue;
                    }
                    if (ch == '`') ch = '°';
                    // "|" és el triangle Δ al tipus de lletra del ToLiss (p.ex. "700Δ" a F-PLN, en qualsevol color)
                    if (ch == '|') ch = 'Δ';
                    if (ch == 'Δ')
                    {
                        // triangle dibuixat (no el de la lletra): buit, més baix i ample que una lletra, assegut a la
                        // línia de base, perquè no es pugui confondre mai amb una A (ToLiss "|" i Fenix "&")
                        var fd = sm ? small : big;
                        float capH = fd.Size * 0.64f;
                        float cyc = cell.Y + rh / 2 + (sm ? rh * 0.06f : 0);
                        float yb = cyc + capH * 0.5f, th = capH * 0.55f, tw = cw * 0.78f, xc = cell.X + cw / 2;
                        using var tp = new Pen(color, fd.Size * (sm ? 0.08f : 0.1f)) { LineJoin = LineJoin.Round };
                        g.DrawPolygon(tp, new[] { new PointF(xc - tw / 2, yb), new PointF(xc + tw / 2, yb), new PointF(xc, yb - th) });
                        continue;
                    }
                    // símbols especials del ToLiss (cadenes de color "s"), comprovats a la pàgina PROG: A "[" i B "]"
                    // (els altres, de moment tal com arriben)
                    // (2 i 3: fletxes de canvi de pàgina ← → del títol, en blanc; 4: fletxa ← davant d'un text, p.ex.
                    //  "←UNDO" a F-PLN). Els altres símbols prenen el color del text que tenen al costat (←UNDO àmbar,
                    //  [M] blau); sense text al costat, blau.
                    if (s.Col[row, col] == 's')
                    {
                        if (ch is '2' or '3') color = ColorOf('w');
                        else color = ColorOf(NeighbourColor(s, row, col) ?? 'b');
                        // (0: fletxa ← davant de les SID/STAR, p.ex. "←NO SID" a DEPARTURE)
                        // (1: fletxa → darrere d'un text, p.ex. "ABEAM PTS→" a DIR TO)
                        ch = ch switch { 'A' => '[', 'B' => ']', '0' => '←', '1' => '→', '2' => '←', '3' => '→', '4' => '←', _ => ch };
                    }
                    // la lletra s'estreny si cal perquè hi càpiga a l'amplada de la columna
                    var f = sm ? small : big;
                    var sz = g.MeasureString(ch.ToString(), f, PointF.Empty, sf);
                    float sx = Math.Min(0.84f, sz.Width > 0 ? cw * 0.92f / sz.Width : 1f);
                    var st2 = g.Save();
                    g.TranslateTransform(cell.X + cw / 2, cell.Y + rh / 2 + (sm ? rh * 0.06f : 0));
                    g.ScaleTransform(sx, 1f);
                    if (ch == 'Q')
                    {
                        // Q com la del ToLiss: una O amb un traç curt en diagonal a baix a la dreta (la de Consolas
                        // té una cua que baixa per sota de la línia)
                        using (var b = new SolidBrush(color)) g.DrawString("O", f, b, 0, 0, sf);
                        float em = f.Size;
                        using var qp = new Pen(color, em * (sm ? 0.09f : 0.12f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                        // el traç va sobretot per dins de la O i a penes en surt, a baix a la dreta
                        g.DrawLine(qp, em * 0.03f, em * 0.05f, em * 0.25f, em * 0.29f);
                    }
                    else
                        using (var b = new SolidBrush(color)) g.DrawString(ch.ToString(), f, b, 0, 0, sf);
                    g.Restore(st2);
                }
            // fletxes de la cantonada de baix a la dreta (pàgines amb més línies), en blanc i petites. VertSlewKeys
            // (comprovat al ToLiss): 1 ↑↓ (F-PLN), 2 només ↑ (DEPARTURE); 3 = només ↓ (suposat, no vist encara)
            if (s.VertSlew != 0)
            {
                var wb = ColorOf('w');
                var arrows = s.VertSlew switch
                {
                    2 => new[] { ('↑', 23) },
                    3 => new[] { ('↓', 23) },
                    _ => new[] { ('↑', 22), ('↓', 23) },
                };
                foreach (var (ch, col) in arrows)
                {
                    var cell = new RectangleF(r.X + col * cw, r.Y + 13 * rh, cw, rh);
                    var sz = g.MeasureString(ch.ToString(), small, PointF.Empty, sf);
                    float sx = Math.Min(0.84f, sz.Width > 0 ? cw * 0.92f / sz.Width : 1f);
                    var st2 = g.Save();
                    g.TranslateTransform(cell.X + cw / 2, cell.Y + rh / 2 + rh * 0.06f);
                    g.ScaleTransform(sx, 1f);
                    using (var b = new SolidBrush(wb)) g.DrawString(ch.ToString(), small, b, 0, 0, sf);
                    g.Restore(st2);
                }
            }
            g.Restore(st);
        }
    }

    internal sealed class OhBlankCap : OhSilk
    {
        readonly RectangleF _r;
        public OhBlankCap(RectangleF r) { _r = r; Bounds = RectangleF.Inflate(r, 3, 3); }
        public override void Draw(Graphics g)
        {
            using var p = Oh.RoundRect(_r, Math.Min(_r.Width, _r.Height) * 0.12f);
            using (var b = new LinearGradientBrush(RectangleF.Inflate(_r, 0.5f, 0.5f), Oh.PlateB, Oh.PlateA, 90f)) g.FillPath(b, p);
            var st = g.Save(); g.TranslateTransform(0.5f, 0.5f);
            using (var pe = new Pen(Color.FromArgb(150, Oh.PlateHi), 0.7f)) g.DrawPath(pe, p);
            g.Restore(st);
            using (var pe = new Pen(Color.FromArgb(190, Oh.PlateLo), 0.7f)) g.DrawPath(pe, p);
        }
    }

    /// <summary>Llumet rodó (verd encès o fosc apagat).</summary>
    /// <summary>Cargols del pedestal que no són DZUS (imatge de referència): estrella (gran o petit), "+" petit encastat i ranura petit.</summary>
    internal static class PedScrew
    {
        /// <summary>Cargol d'estrella: cap bombat metàl·lic amb una creu en aspa.</summary>
        public static void Cross(Graphics g, float cx, float cy, float r)
        {
            var o = new RectangleF(cx - r, cy - r, r * 2, r * 2);
            Oh.SoftShadowEllipse(g, o, r * 0.1f, r * 0.14f, r * 0.35f, 150);
            using (var lg = new LinearGradientBrush(o, Color.Black, Color.Black, 45f)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.FromArgb(0xd6, 0xdb, 0xe0), Color.FromArgb(0x8e, 0x96, 0x9e), Color.FromArgb(0x4a, 0x51, 0x58) },
                    Positions = new[] { 0f, 0.5f, 1f }
                }
            }) g.FillEllipse(lg, o);
            using (var pe = new Pen(Color.FromArgb(217, 15, 20, 25), Math.Max(0.6f, r * 0.06f))) g.DrawEllipse(pe, o);
            // creu en aspa (dues ranures a ±45°), fosca amb el cantell de baix il·luminat
            float L = r * 0.62f, W = Math.Max(0.9f, r * 0.30f);
            var st = g.Save();
            g.TranslateTransform(cx, cy);
            foreach (float a in new[] { 45f, -45f })
            {
                var s2 = g.Save();
                g.RotateTransform(a);
                using (var hb = new SolidBrush(Color.FromArgb(150, 225, 232, 238))) g.FillRectangle(hb, -L, -W / 2 + W * 0.25f, L * 2, W);
                g.Restore(s2);
            }
            foreach (float a in new[] { 45f, -45f })
            {
                var s2 = g.Save();
                g.RotateTransform(a);
                using (var db = new SolidBrush(Color.FromArgb(0x26, 0x2b, 0x30))) g.FillRectangle(db, -L, -W / 2, L * 2, W);
                g.Restore(s2);
            }
            g.Restore(st);
        }

        /// <summary>Cap gris fosc comú als cargols petits interiors (creu i ranura): ombra, cap i cantell.</summary>
        static void DarkHead(Graphics g, RectangleF o, float r)
        {
            Oh.SoftShadowEllipse(g, o, r * 0.08f, r * 0.1f, r * 0.25f, 90);
            using (var lg = new LinearGradientBrush(o, Color.FromArgb(0x6a, 0x71, 0x78), Color.FromArgb(0x33, 0x38, 0x3d), 45f)) g.FillEllipse(lg, o);
            using (var eb = new LinearGradientBrush(RectangleF.Inflate(o, 0.5f, 0.5f), Color.FromArgb(220, 20, 24, 28), Color.FromArgb(200, 150, 160, 170), 45f))
            using (var ep = new Pen(eb, Math.Max(0.6f, r * 0.1f))) g.DrawEllipse(ep, o);
        }

        /// <summary>Cargol de creu interior, gris fosc (mateixa mida que el de ranura).</summary>
        public static void Plus(Graphics g, float cx, float cy, float r)
        {
            var o = new RectangleF(cx - r, cy - r, r * 2, r * 2);
            DarkHead(g, o, r);
            float L = r * 0.62f;
            using (var hp = new Pen(Color.FromArgb(120, 170, 178, 186), Math.Max(0.6f, r * 0.2f)))
            {
                g.DrawLine(hp, cx - L, cy + r * 0.1f, cx + L, cy + r * 0.1f);
                g.DrawLine(hp, cx + r * 0.1f, cy - L, cx + r * 0.1f, cy + L);
            }
            using var pe = new Pen(Color.FromArgb(0x16, 0x19, 0x1c), Math.Max(0.6f, r * 0.22f));
            g.DrawLine(pe, cx - L, cy, cx + L, cy);
            g.DrawLine(pe, cx, cy - L, cx, cy + L);
        }

        /// <summary>Cargol de ranura interior (cap pla), gris fosc, amb la ranura en diagonal.</summary>
        public static void Slot(Graphics g, float cx, float cy, float r)
        {
            var o = new RectangleF(cx - r, cy - r, r * 2, r * 2);
            DarkHead(g, o, r);
            float L = r * 0.8f, W = Math.Max(0.8f, r * 0.22f);
            var st = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(-45f);
            using (var hb = new SolidBrush(Color.FromArgb(120, 170, 178, 186))) g.FillRectangle(hb, -L, -W / 2 + W * 0.35f, L * 2, W);
            using (var db = new SolidBrush(Color.FromArgb(0x16, 0x19, 0x1c))) g.FillRectangle(db, -L, -W / 2, L * 2, W);
            g.Restore(st);
        }
    }

    internal enum PedScrewKind { Cross, Plus, Slot, Dzus }

    /// <summary>Cargol solt (no DZUS) dibuixat sobre la planxa.</summary>
    internal sealed class OhScrew : OhSilk
    {
        readonly float _cx, _cy, _r; readonly PedScrewKind _kind;
        public OhScrew(PedScrewKind kind, float cx, float cy, float r)
        {
            _kind = kind; _cx = cx; _cy = cy; _r = r;
            Bounds = new RectangleF(cx - r - 3, cy - r - 3, r * 2 + 7, r * 2 + 7);
        }
        public override void Draw(Graphics g)
        {
            switch (_kind)
            {
                case PedScrewKind.Cross: PedScrew.Cross(g, _cx, _cy, _r); break;
                case PedScrewKind.Plus: PedScrew.Plus(g, _cx, _cy, _r); break;
                case PedScrewKind.Dzus: Oh.DzusRing(g, _cx, _cy, _r); break;
                default: PedScrew.Slot(g, _cx, _cy, _r); break;
            }
        }
    }

    internal sealed class OhLed : OhSilk
    {
        readonly float _cx, _cy, _r; readonly bool _lit;
        public OhLed(float cx, float cy, float r, bool lit = true) { _cx = cx; _cy = cy; _r = r; _lit = lit; Bounds = new RectangleF(cx - r - 3, cy - r - 3, r * 2 + 6, r * 2 + 6); }
        public override void Draw(Graphics g)
        {
            var o = new RectangleF(_cx - _r - 1f, _cy - _r - 1f, _r * 2 + 2f, _r * 2 + 2f);
            using (var b = new SolidBrush(Color.FromArgb(40, 44, 48))) g.FillEllipse(b, o);
            var i = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            if (_lit) Oh.Specular(g, i, Color.FromArgb(190, 240, 120), Color.FromArgb(60, 120, 30));
            else Oh.Specular(g, i, Color.FromArgb(90, 96, 100), Color.FromArgb(20, 22, 24));
        }
    }

    /// <summary>Bola negra (pom de la safata de darrere).</summary>
    internal sealed class OhBall : OhSilk
    {
        readonly float _cx, _cy, _r;
        public OhBall(float cx, float cy, float r) { _cx = cx; _cy = cy; _r = r; Bounds = new RectangleF(cx - r - 4, cy - r - 4, r * 2 + 10, r * 2 + 10); }
        public override void Draw(Graphics g)
        {
            var o = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            Oh.SoftShadowEllipse(g, o, _r * 0.25f, _r * 0.3f, _r * 0.4f, 120);
            Oh.Specular(g, o, Color.FromArgb(90, 94, 98), Color.FromArgb(10, 11, 12));
        }
    }

    /// <summary>Roda del trim de profunditat: cilindre negre vertical brillant amb dues franges blanques.</summary>
    internal sealed class OhTrimWheel : OhSilk
    {
        readonly RectangleF _r;
        public OhTrimWheel(RectangleF r) { _r = r; Bounds = RectangleF.Inflate(r, 6, 6); }
        public override void Draw(Graphics g)
        {
            var r = _r; float rad = r.Width / 2f;
            using var p = Oh.RoundRect(r, rad);
            Oh.SoftShadow(g, p, r.Width * 0.12f, r.Width * 0.08f, r.Width * 0.18f, 150);
            using (var b = new LinearGradientBrush(new RectangleF(r.X - 0.01f, r.Y, r.Width + 0.02f, r.Height), Color.Black, Color.Black, 0f)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.FromArgb(8, 8, 9), Color.FromArgb(40, 42, 45), Color.FromArgb(150, 154, 158), Color.FromArgb(52, 54, 58), Color.FromArgb(14, 15, 16), Color.FromArgb(4, 4, 5) },
                    Positions = new[] { 0f, 0.18f, 0.32f, 0.46f, 0.8f, 1f }
                }
            }) g.FillPath(b, p);
            // franges blanques (corbades: la roda és un cilindre)
            var st = g.Save();
            g.SetClip(p, CombineMode.Intersect);
            foreach (float f in new[] { 0.28f, 0.66f })
            {
                float y = r.Y + r.Height * f, h = r.Height * 0.07f;
                using var band = new GraphicsPath();
                band.AddArc(r.X, y - r.Width * 0.12f, r.Width, r.Width * 0.24f, 180, -180);
                band.AddArc(r.X, y + h - r.Width * 0.12f, r.Width, r.Width * 0.24f, 0, 180);
                band.CloseFigure();
                using var bb = new LinearGradientBrush(new RectangleF(r.X - 0.01f, r.Y, r.Width + 0.02f, r.Height), Color.Black, Color.Black, 0f)
                {
                    InterpolationColors = new ColorBlend
                    {
                        Colors = new[] { Color.FromArgb(120, 124, 128), Color.FromArgb(225, 228, 230), Color.FromArgb(250, 250, 250), Color.FromArgb(205, 208, 212), Color.FromArgb(110, 114, 118) },
                        Positions = new[] { 0f, 0.25f, 0.38f, 0.7f, 1f }
                    }
                };
                g.FillPath(bb, band);
            }
            g.Restore(st);
            using (var pe = new Pen(Color.FromArgb(200, 0, 0, 0), 0.6f)) g.DrawPath(pe, p);
        }
    }

    /// <summary>Escala del THS al costat de cada roda del trim: franja clara, línia blanca, ratlla verda i les marques CG / UP / DN.</summary>
    internal sealed class OhThsScale : OhSilk
    {
        readonly RectangleF _r; readonly bool _left; readonly float _k;
        public OhThsScale(RectangleF r, bool left, float k) { _r = r; _left = left; _k = k; Bounds = RectangleF.Inflate(r, 4, 4); }
        public override void Draw(Graphics g)
        {
            var r = _r;
            // franja metàl·lica clara, més clara cap a la roda
            using (var b = new LinearGradientBrush(new RectangleF(r.X - 0.01f, r.Y, r.Width + 0.02f, r.Height),
                       _left ? Color.FromArgb(150, 165, 178) : Color.FromArgb(214, 224, 232), _left ? Color.FromArgb(214, 224, 232) : Color.FromArgb(150, 165, 178), 0f))
                g.FillRectangle(b, r);
            using (var pe = new Pen(Color.FromArgb(160, Oh.PlateLo), 0.8f)) g.DrawRectangle(pe, r.X, r.Y, r.Width, r.Height);
            float k = _k;
            float lineX = _left ? r.Right - 20 * k : r.X + 20 * k;     // línia blanca vertical
            float greenX = _left ? r.Right - 13 * k : r.X + 13 * k;    // ratlla verda
            using (var gp = new Pen(Color.FromArgb(40, 190, 90), 6 * k)) g.DrawLine(gp, greenX, r.Y + 8 * k, greenX, r.Bottom - 8 * k);
            using (var wp = new Pen(Color.White, 1.3f)) g.DrawLine(wp, lineX, r.Y + 30 * k, lineX, r.Bottom - 20 * k);
            // (y de referència, número, UP/DN, CG)
            (float y, string n, string d, string cg)[] m =
            {
                (948, "", "UP", ""), (1010, "3", "UP", "15"), (1085, "2", "UP", "20"), (1185, "1", "UP", "25"), (1280, "0", "", "30"),
                (1385, "1", "DN", "35"), (1475, "2", "DN", ""), (1545, "3", "DN", "41")
            };
            var dark = Color.FromArgb(70, 84, 98);
            float numX = _left ? lineX - 14 * k : lineX + 14 * k, cgX = _left ? r.X + 22 * k : r.Right - 22 * k;
            using var tp = new Pen(Color.White, 1.2f);
            foreach (var (yr, n, d, cg) in m)
            {
                float y = yr * k;
                g.DrawLine(tp, lineX, y, lineX + (_left ? -7 : 7) * k, y);
                if (n != "") Oh.Text(g, n, numX, y - 5 * k, 8.2f, Color.White);
                if (d != "") Oh.Text(g, d, numX, y + (n != "" ? 7 : 0) * k, 8.2f, Color.White);
                if (cg != "") { Oh.Text(g, "CG", cgX, y - 6 * k, 8.2f, dark); Oh.Text(g, cg, cgX, y + 7 * k, 8.2f, dark); }
            }
        }
    }

    /// <summary>Barra cromada horitzontal (part de darrere del pedestal).</summary>
    internal sealed class OhChromeBar : OhSilk
    {
        readonly RectangleF _r;
        public OhChromeBar(RectangleF r) { _r = r; Bounds = RectangleF.Inflate(r, 5, 5); }
        public override void Draw(Graphics g)
        {
            var r = _r;
            using var p = Oh.RoundRect(r, 3f);
            Oh.SoftShadow(g, p, 1f, 2f, 3f, 120);
            using (var b = new LinearGradientBrush(new RectangleF(r.X, r.Y - 0.01f, r.Width, r.Height + 0.02f), Color.Black, Color.Black, 90f)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.FromArgb(120, 126, 132), Color.FromArgb(240, 242, 244), Color.FromArgb(190, 196, 202), Color.FromArgb(100, 108, 116), Color.FromArgb(70, 80, 90) },
                    Positions = new[] { 0f, 0.18f, 0.4f, 0.55f, 1f }
                }
            }) g.FillPath(b, p);
            using var pe = new Pen(Color.FromArgb(160, 30, 36, 42), 0.7f); g.DrawPath(pe, p);
        }
    }

    /// <summary>Placa clara amb una ranura negra i cinc cargolets (sota la barra cromada).</summary>
    internal sealed class OhSlotPlate : OhSilk
    {
        readonly RectangleF _r;
        public OhSlotPlate(RectangleF r) { _r = r; Bounds = RectangleF.Inflate(r, 4, 4); }
        public override void Draw(Graphics g)
        {
            var r = _r;
            using var p = Oh.RoundRect(r, r.Height * 0.15f);
            Oh.SoftShadow(g, p, 0.8f, 1.5f, 2f, 110);
            using (var b = new LinearGradientBrush(new RectangleF(r.X, r.Y - 0.01f, r.Width, r.Height + 0.02f), Color.FromArgb(226, 230, 234), Color.FromArgb(170, 176, 182), 90f)) g.FillPath(b, p);
            using (var pe = new Pen(Color.FromArgb(120, 40, 46, 52), 0.6f)) g.DrawPath(pe, p);
            var slot = new RectangleF(r.X + r.Width * 0.05f, r.Y + r.Height * 0.18f, r.Width * 0.9f, r.Height * 0.24f);
            using (var sb = new SolidBrush(Color.FromArgb(14, 15, 16))) g.FillRectangle(sb, slot);
            for (int i = 0; i < 5; i++)
            {
                float cx = r.X + r.Width * (0.06f + 0.22f * i), cy = r.Y + r.Height * 0.7f, sr = r.Height * 0.11f;
                Oh.Screw(g, cx, cy, sr, 45f);
            }
        }
    }

    /// <summary>Barra negra arrodonida (porta-llapis) amb la seva ombra.</summary>
    internal sealed class OhPenBar : OhSilk
    {
        readonly RectangleF _r;
        public OhPenBar(RectangleF r) { _r = r; Bounds = RectangleF.Inflate(r, 10, 14); }
        public override void Draw(Graphics g)
        {
            var r = _r;
            using var p = Oh.RoundRect(r, r.Height / 2f);
            Oh.SoftShadow(g, p, r.Height * 0.3f, r.Height * 0.8f, r.Height * 0.5f, 140);
            using (var b = new LinearGradientBrush(new RectangleF(r.X, r.Y - 0.01f, r.Width, r.Height + 0.02f), Color.Black, Color.Black, 90f)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.FromArgb(70, 72, 76), Color.FromArgb(120, 124, 128), Color.FromArgb(30, 31, 34), Color.FromArgb(6, 6, 7) },
                    Positions = new[] { 0f, 0.25f, 0.6f, 1f }
                }
            }) g.FillPath(b, p);
        }
    }

    /// <summary>Sòcol estriat de baix (franges horitzontals fosques).</summary>
    internal sealed class OhRibbed : OhSilk
    {
        readonly RectangleF _r;
        public OhRibbed(RectangleF r) { _r = r; Bounds = RectangleF.Inflate(r, 4, 4); }
        public override void Draw(Graphics g)
        {
            var r = _r;
            using var p = Oh.RoundRect(r, 3f);
            Oh.SoftShadow(g, p, 0.8f, 1.6f, 2.4f, 120);
            using (var b = new SolidBrush(Color.FromArgb(52, 56, 62))) g.FillPath(b, p);
            var st = g.Save();
            g.SetClip(p, CombineMode.Intersect);
            float step = Math.Max(2.2f, r.Height / 16f);
            using (var dp = new Pen(Color.FromArgb(28, 30, 34), step * 0.45f))
            using (var lp = new Pen(Color.FromArgb(110, 116, 124), step * 0.18f))
                for (float y = r.Y + step * 0.6f; y < r.Bottom; y += step)
                {
                    g.DrawLine(dp, r.X, y, r.Right, y);
                    g.DrawLine(lp, r.X, y + step * 0.35f, r.Right, y + step * 0.35f);
                }
            g.Restore(st);
            using var pe = new Pen(Color.FromArgb(150, 160, 175, 190), 0.6f); g.DrawPath(pe, p);
        }
    }

    // ═══════════════════════════════ ELEMENTS INTERACTIUS DEL PEDESTAL ═══════════════════════════════

    /// <summary>Tecla rodona fosca amb el rètol blanc (xifres del MCDU, teclat de l'ATC).</summary>
    internal sealed class OhRoundKey : OhElement
    {
        readonly float _cx, _cy, _r; readonly string _label; float _hold;
        public OhRoundKey(string id, float cx, float cy, float r, string label)
        {
            Id = id; _cx = cx; _cy = cy; _r = r; _label = label;
            Bounds = new RectangleF(cx - r, cy - r, r * 2, r * 2);
        }
        public override bool Animating => _hold > 0;
        public override void Tick(float dt) => _hold = Math.Max(0, _hold - dt);
        public override void Tap(PointF p) { _hold = 0.16f; Owner.RaiseCommand(Id, "PUSH"); Changed(); }
        public override void Draw(Graphics g)
        {
            bool pr = _hold > 0;
            float r = pr ? _r * 0.96f : _r;
            var o = new RectangleF(_cx - r, _cy - r, r * 2, r * 2);
            using (var sb = new SolidBrush(Color.FromArgb(110, 15, 25, 35))) g.FillEllipse(sb, o.X + r * 0.08f, o.Y + r * 0.14f, o.Width, o.Height);
            using (var lg = new LinearGradientBrush(o, Color.FromArgb(78, 82, 86), Color.FromArgb(16, 18, 20), 70f)) g.FillEllipse(lg, o);
            var i = RectangleF.Inflate(o, -r * 0.12f, -r * 0.12f);
            using (var ib = new LinearGradientBrush(i, Color.FromArgb(50, 53, 57), Color.FromArgb(28, 30, 33), 90f)) g.FillEllipse(ib, i);
            using (var pe = new Pen(Color.FromArgb(10, 10, 12), 0.6f)) g.DrawEllipse(pe, o);
            float size = Math.Min(r * 0.95f, r * 2.4f / Math.Max(1, _label.Length * 0.62f + 0.3f));
            Oh.Text(g, _label, _cx, _cy + r * 0.04f, size, Color.FromArgb(240, 242, 244));
        }
    }

    /// <summary>Llum de dues llegendes que no es pot prémer (FIRE / FAULT de l'ENG, FAULT / OFF de la porta).</summary>
    internal sealed class OhAnnunciator : OhElement, IOhTestable
    {
        public string Upper, Lower; public Color UpperColor, LowerColor; public float UpperLevel, LowerLevel;
        public bool Test { get; set; }
        public OhAnnunciator(string id, RectangleF r, string upper, string lower)
        {
            Id = id; Bounds = r; Upper = upper; Lower = lower;
            UpperColor = upper == "FIRE" ? Oh.Red : Oh.LegendColor(upper);
            LowerColor = Oh.LegendColor(lower);
        }
        public override bool HitTest(PointF p) => false;
        public override IEnumerable<RectangleF> HitAreas { get { yield break; } }
        public override void Draw(Graphics g)
        {
            var r = Bounds;
            using (var sp = Oh.RoundRect(r, 3f)) Oh.SoftShadow(g, sp, 1f, 1.6f, 2.2f, 110);
            using (var bp = Oh.RoundRect(r, 3f))
            {
                using (var bb = new LinearGradientBrush(r, Color.FromArgb(98, 104, 110), Color.FromArgb(28, 31, 34), 90f)) g.FillPath(bb, bp);
                using var pe = new Pen(Color.FromArgb(24, 26, 28), 0.7f); g.DrawPath(pe, bp);
            }
            var f = RectangleF.Inflate(r, -r.Width * 0.09f, -r.Width * 0.09f);
            using (var fb = new SolidBrush(Color.FromArgb(18, 20, 22))) g.FillRectangle(fb, f);
            float half = f.Height / 2f;
            Oh.Legend(g, new RectangleF(f.X, f.Y, f.Width, half), Upper, UpperColor, Owner.Level(UpperLevel, Test), false);
            Oh.Legend(g, new RectangleF(f.X, f.Y + half, f.Width, half), Lower, LowerColor, Owner.Level(LowerLevel, Test), true);
        }
    }

    /// <summary>ENG MASTER: palanca quadrada fosca amb "ENG 1/2" que s'aixeca (ON) o s'abaixa (OFF) dins el seu sòcol.</summary>
    internal sealed class OhEngMaster : OhElement
    {
        readonly string _num; public bool On; float _o;
        public OhEngMaster(string id, RectangleF r, string num) { Id = id; Bounds = r; _num = num; }
        public override RectangleF DirtyBounds => RectangleF.Inflate(Bounds, Bounds.Width * 0.12f + 4, Bounds.Height * 0.45f + 4);
        public override int State => On ? 1 : 0;
        public override bool Animating => Math.Abs(_o - (On ? 1 : 0)) > 0.001f;
        public override void Tick(float dt) => _o = Approach(_o, On ? 1 : 0, dt * 6f);
        public override void Tap(PointF p) { On = !On; Owner.RaiseCommand(Id, On ? "ON" : "OFF"); Changed(); }
        public override void Draw(Graphics g)
        {
            var r = Bounds;
            // sòcol rodó fosc (es veu per sota de la palanca)
            var sock = new RectangleF(r.X + r.Width * 0.08f, r.Y + r.Height * 0.32f, r.Width * 0.84f, r.Height * 0.95f);
            using (var sb = new LinearGradientBrush(sock, Color.FromArgb(70, 72, 76), Color.FromArgb(14, 15, 16), 90f)) g.FillEllipse(sb, sock);
            using (var pe = new Pen(Color.FromArgb(120, 0, 0, 0), 0.8f)) g.DrawEllipse(pe, sock);
            // palanca: amunt (ON) / avall (OFF)
            float dy = (-0.12f + 0.12f * (1 - _o) * 0.6f) * r.Height - _o * r.Height * 0.05f;
            var lr = new RectangleF(r.X, r.Y + dy, r.Width, r.Height);
            using (var lp = Oh.RoundRect(lr, r.Width * 0.14f))
            {
                Oh.SoftShadow(g, lp, r.Width * 0.02f, r.Height * (0.05f + 0.06f * _o), r.Width * 0.05f, 150);
                using (var lb = new LinearGradientBrush(lr, Color.FromArgb(84 + (int)(14 * _o), 87, 92), Color.FromArgb(26, 27, 30), 90f)) g.FillPath(lb, lp);
                using (var hp = new Pen(Color.FromArgb(90, 255, 255, 255), 0.8f)) g.DrawLine(hp, lr.X + r.Width * 0.12f, lr.Y + 0.8f, lr.Right - r.Width * 0.12f, lr.Y + 0.8f);
                using var pe = new Pen(Color.FromArgb(10, 10, 12), 0.8f); g.DrawPath(pe, lp);
            }
            var tc = Color.FromArgb(160, 163, 168);
            Oh.Text(g, "ENG", lr.X + lr.Width / 2, lr.Y + lr.Height * 0.33f, lr.Height * 0.34f, tc);
            Oh.Text(g, _num, lr.X + lr.Width / 2, lr.Y + lr.Height * 0.72f, lr.Height * 0.38f, tc);
        }
    }

    /// <summary>
    /// SPEED BRAKE: palanca que llisca per una ranura (RET a dalt, FULL a baix). S'arrossega amb el dit; tocar el pom a
    /// RET l'arma (GND SPLRS ARMED: el pom puja); tocar per sobre / per sota del pom el mou a RET / 1/2 / FULL.
    /// </summary>
    internal sealed class OhSpeedBrake : OhElement, IOhDraggable
    {
        readonly float _cx, _top, _bot, _hw, _y0, _y1, _gw, _gh;
        public float Pos; public bool Armed;
        float _arm; bool _grab, _moved; float _grabDy, _startPos; double _lastSend;
        public OhSpeedBrake(string id, float cx, float slotTop, float slotBot, float slotHalfW, float gripY0, float gripY1, float gripW, float gripH)
        {
            Id = id; _cx = cx; _top = slotTop; _bot = slotBot; _hw = slotHalfW; _y0 = gripY0; _y1 = gripY1; _gw = gripW; _gh = gripH;
            Bounds = RectangleF.FromLTRB(cx - gripW / 2 - 2, gripY0 - gripH / 2 - 2, cx + gripW / 2 + 2, Math.Max(slotBot, gripY1 + gripH / 2) + 2);
        }
        public override RectangleF DirtyBounds => RectangleF.Inflate(Bounds, 8, 10);
        float Y(float p) => _y0 + (_y1 - _y0) * p;
        RectangleF Grip(float p) => new(_cx - _gw / 2, Y(p) - _gh / 2, _gw, _gh);
        public override int State => (int)Math.Round(Pos * 100);
        public override bool Animating => Math.Abs(_arm - (Armed ? 1 : 0)) > 0.001f;
        public override void Tick(float dt) => _arm = Approach(_arm, Armed ? 1 : 0, dt * 8f);
        public bool Grabbing => _grab;

        public override void PressStart(PointF p)
        {
            _grab = RectangleF.Inflate(Grip(Pos), 4, 4).Contains(p);
            _moved = false; _grabDy = p.Y - Y(Pos); _startPos = Pos;
        }
        public void DragTo(PointF p)
        {
            if (!_grab) return;
            float v = Math.Clamp((p.Y - _grabDy - _y0) / (_y1 - _y0), 0f, 1f);
            if (!_moved && Math.Abs(v - _startPos) < 0.03f) return;
            _moved = true;
            if (Math.Abs(v - Pos) < 0.002f) return;
            Pos = v;
            if (Pos > 0.01f) Armed = false;
            Changed();
            if (OhThrottle.Now - _lastSend >= 0.05) Send();
        }
        void Send() { _lastSend = OhThrottle.Now; Owner.RaiseCommand(Id, "POS:" + Pos.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)); }
        public override void PressEnd()
        {
            if (!_grab) return;
            _grab = false;
            if (!_moved) return;
            // a prop d'una marca (RET, 1/2, FULL) s'hi queda enganxada
            foreach (float d in new[] { 0f, 0.5f, 1f }) if (Math.Abs(Pos - d) < 0.05f) Pos = d;
            Send(); Changed();
        }
        public override void Tap(PointF p)
        {
            if (_moved) { _moved = false; return; }
            if (RectangleF.Inflate(Grip(Pos), 4, 4).Contains(p))
            {
                if (Pos > 0.02f) return;
                Armed = !Armed;
                Owner.RaiseCommand(Id, Armed ? "ARM" : "DISARM");
                Changed();
                return;
            }
            Pos = p.Y < Y(Pos) ? (Pos > 0.5f ? 0.5f : 0f) : (Pos < 0.5f ? 0.5f : 1f);
            Armed = false;
            Send(); Changed();
        }

        public override void Draw(Graphics g)
        {
            // ranura fosca
            var slot = new RectangleF(_cx - _hw, _top, _hw * 2, _bot - _top);
            using (var sp = Oh.RoundRect(slot, _hw * 0.8f))
            {
                using (var sb = new LinearGradientBrush(new RectangleF(slot.X - 0.01f, 0, slot.Width + 0.02f, 1), Color.Black, Color.Black, 0f)
                { InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(8, 9, 10), Color.FromArgb(40, 42, 46), Color.FromArgb(10, 11, 12) }, Positions = new[] { 0f, 0.5f, 1f } } })
                    g.FillPath(sb, sp);
                using (var lip = new Pen(Color.FromArgb(120, Oh.PlateHi), 0.6f)) { var st0 = g.Save(); g.TranslateTransform(0.5f, 0.5f); g.DrawPath(lip, sp); g.Restore(st0); }
                using (var ep = new Pen(Color.FromArgb(220, 8, 10, 12), 0.8f)) g.DrawPath(ep, sp);
            }
            float gy = Y(Pos), lift = _arm * _gh * 0.08f;
            var grip = Grip(Pos); grip.Offset(0, -lift);
            // tija: de sota el pom cap avall, dins la ranura
            float stemTop = grip.Bottom - _gh * 0.2f, stemBot = Math.Min(_bot - _hw * 0.6f, gy + _gh * 1.9f);
            if (stemBot > stemTop)
            {
                var stem = new[] { new PointF(_cx - _gw * 0.22f, stemTop), new PointF(_cx + _gw * 0.22f, stemTop), new PointF(_cx + _hw * 1.0f, stemBot), new PointF(_cx - _hw * 1.0f, stemBot) };
                using (var sh = new SolidBrush(Color.FromArgb(90, 0, 0, 0))) g.FillPolygon(sh, stem.Select(q => new PointF(q.X + _hw * 0.5f, q.Y + 2f)).ToArray());
                using (var sb = new LinearGradientBrush(new RectangleF(_cx - _gw * 0.22f - 0.01f, 0, _gw * 0.44f + 0.02f, 1), Color.Black, Color.Black, 0f)
                { InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(14, 15, 17), Color.FromArgb(70, 73, 78), Color.FromArgb(24, 25, 28), Color.FromArgb(8, 8, 9) }, Positions = new[] { 0f, 0.35f, 0.7f, 1f } } })
                    g.FillPolygon(sb, stem);
                // anella clara (collar) just sota el pom
                var col = new RectangleF(_cx - _gw * 0.2f, stemTop + _gh * 0.18f, _gw * 0.4f, _gh * 0.12f);
                using (var cb = new LinearGradientBrush(new RectangleF(col.X - 0.01f, 0, col.Width + 0.02f, 1), Color.FromArgb(150, 154, 160), Color.FromArgb(240, 242, 244), 0f)) g.FillEllipse(cb, col);
            }
            // pom: bloc negre amb la cara de dalt més clara
            using var gp = Oh.RoundRect(grip, _gw * 0.12f);
            Oh.SoftShadow(g, gp, _gw * 0.04f, _gh * (0.08f + 0.1f * _arm), _gw * 0.08f, 160);
            using (var gb = new LinearGradientBrush(new RectangleF(grip.X - 0.01f, 0, grip.Width + 0.02f, 1), Color.Black, Color.Black, 0f)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.FromArgb(20, 21, 23), Color.FromArgb(70, 73, 78), Color.FromArgb(108, 112, 118), Color.FromArgb(46, 48, 52), Color.FromArgb(12, 12, 14) },
                    Positions = new[] { 0f, 0.25f, 0.42f, 0.7f, 1f }
                }
            }) g.FillPath(gb, gp);
            var top = new RectangleF(grip.X + _gw * 0.06f, grip.Y + _gh * 0.05f, grip.Width - _gw * 0.12f, _gh * 0.34f);
            using (var tp = Oh.RoundRect(top, _gw * 0.08f))
            using (var tb = new LinearGradientBrush(new RectangleF(top.X, top.Y - 0.01f, top.Width, top.Height + 0.02f), Color.FromArgb(120 + (int)(40 * _arm), 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f)) g.FillPath(tb, tp);
            using (var pe = new Pen(Color.FromArgb(220, 4, 4, 5), 0.7f)) g.DrawPath(pe, gp);
        }
    }

    /// <summary>
    /// FLAPS: palanca dins una guia metàl·lica amb cinc posicions (0, 1, 2, 3, FULL). S'arrossega amb el dit i en deixar-la
    /// s'enganxa a la posició més propera; tocar per sobre / per sota del pom la mou una posició.
    /// </summary>
    internal sealed class OhFlapsLever : OhElement, IOhDraggable
    {
        readonly RectangleF _gate; readonly float[] _det; readonly string[] _lab; readonly float _gw, _gh;
        public int Index; float _y; bool _grab, _moved; float _grabDy;
        public OhFlapsLever(string id, RectangleF gate, float[] detents, string[] labels, float gripW, float gripH)
        {
            Id = id; _gate = gate; _det = detents; _lab = labels; _gw = gripW; _gh = gripH;
            _y = detents[0];
            float cx = gate.X + gate.Width / 2;
            Bounds = RectangleF.FromLTRB(gate.X - 2, Math.Min(gate.Y, detents[0] - gripH / 2) - 2, gate.Right + 2, Math.Max(gate.Bottom, detents[^1] + gripH / 2) + 2);
        }
        public override RectangleF DirtyBounds => RectangleF.Inflate(Bounds, 8, 10);
        float Cx => _gate.X + _gate.Width / 2;
        RectangleF Grip(float y) => new(Cx - _gw / 2, y - _gh / 2, _gw, _gh);
        public override int State => Index;
        public override bool Animating => !_grab && Math.Abs(_y - _det[Index]) > 0.05f;
        public override void Tick(float dt) => _y = Approach(_y, _det[Index], dt * 260f);
        public bool Grabbing => _grab;
        public override void PressStart(PointF p)
        {
            _grab = RectangleF.Inflate(Grip(_y), 4, 4).Contains(p);
            _moved = false; _grabDy = p.Y - _y;
        }
        public void DragTo(PointF p)
        {
            if (!_grab) return;
            float y = Math.Clamp(p.Y - _grabDy, _det[0], _det[^1]);
            if (!_moved && Math.Abs(y - _y) < 3f) return;
            _moved = true;
            if (Math.Abs(y - _y) < 0.2f) return;
            _y = y;
            Changed();
        }
        public override void PressEnd()
        {
            if (!_grab) return;
            _grab = false;
            if (!_moved) return;
            int best = 0;
            for (int i = 1; i < _det.Length; i++) if (Math.Abs(_det[i] - _y) < Math.Abs(_det[best] - _y)) best = i;
            SetTo(best);
        }
        void SetTo(int i)
        {
            bool ch = i != Index;
            Index = i;
            if (ch) Owner.RaiseCommand(Id, "POS:" + i);
            Changed();
        }
        public override void Tap(PointF p)
        {
            if (_moved) { _moved = false; return; }
            if (RectangleF.Inflate(Grip(_y), 4, 4).Contains(p)) return;
            int ni = Math.Clamp(Index + (p.Y < _y ? -1 : 1), 0, _det.Length - 1);
            if (ni != Index) SetTo(ni);
        }

        public override void Draw(Graphics g)
        {
            var gt = _gate;
            // guia metàl·lica
            using (var pp = Oh.RoundRect(gt, gt.Width * 0.06f))
            {
                Oh.SoftShadow(g, pp, 1.2f, 2.2f, 3f, 130);
                using (var b = new LinearGradientBrush(new RectangleF(gt.X - 0.01f, gt.Y, gt.Width + 0.02f, gt.Height), Color.Black, Color.Black, 0f)
                {
                    InterpolationColors = new ColorBlend
                    {
                        Colors = new[] { Color.FromArgb(104, 116, 128), Color.FromArgb(176, 188, 198), Color.FromArgb(132, 146, 158), Color.FromArgb(176, 188, 198), Color.FromArgb(96, 108, 120) },
                        Positions = new[] { 0f, 0.2f, 0.5f, 0.8f, 1f }
                    }
                }) g.FillPath(b, pp);
                using var pe = new Pen(Color.FromArgb(170, 40, 48, 56), 0.8f); g.DrawPath(pe, pp);
            }
            // ranura central
            float sw = gt.Width * 0.14f;
            var slot = RectangleF.FromLTRB(Cx - sw / 2, _det[0] - sw * 0.6f, Cx + sw / 2, _det[^1] + sw * 0.6f);
            using (var sp = Oh.RoundRect(slot, sw / 2))
            using (var sb = new SolidBrush(Color.FromArgb(12, 13, 15))) g.FillPath(sb, sp);
            // marques de les posicions (en L, a banda i banda) i rètols
            float lx0 = gt.X + gt.Width * 0.18f, lx1 = gt.Right - gt.Width * 0.18f;
            using (var wp = new Pen(Color.FromArgb(235, 240, 244), 1.2f))
            {
                g.DrawLine(wp, lx0, _det[0] - 2, lx0, _det[^1]);
                g.DrawLine(wp, lx1, _det[0] - 2, lx1, _det[^1]);
                for (int i = 0; i < _det.Length; i++)
                {
                    g.DrawLine(wp, lx0, _det[i], lx0 + gt.Width * 0.05f, _det[i]);
                    g.DrawLine(wp, lx1, _det[i], lx1 - gt.Width * 0.05f, _det[i]);
                    float fs = _lab[i].Length > 2 ? 7.2f : 8.2f;
                    Oh.Text(g, _lab[i], gt.Right - gt.Width * 0.08f, _det[i] - 4f, fs, Color.White);
                    if (i == 0 || i == _det.Length - 1 || i == 3) Oh.Text(g, _lab[i], gt.X + gt.Width * 0.08f, _det[i] - 4f, fs, Color.White);
                }
            }
            // pom: hexàgon arrodonit negre (més ample al mig) amb la tija a sota
            var gr = Grip(_y);
            var stem = new RectangleF(Cx - _gw * 0.16f, gr.Y + _gh * 0.6f, _gw * 0.32f, _gh * 0.62f);
            using (var stp = Oh.RoundRect(stem, _gw * 0.14f))
            using (var stb = new LinearGradientBrush(new RectangleF(stem.X - 0.01f, 0, stem.Width + 0.02f, 1), Color.FromArgb(10, 10, 12), Color.FromArgb(50, 52, 56), 0f)) g.FillPath(stb, stp);
            using var hp = new GraphicsPath();
            float w = gr.Width, h = gr.Height, x = gr.X, y = gr.Y;
            hp.AddPolygon(new[]
            {
                new PointF(x + w * 0.24f, y), new PointF(x + w * 0.76f, y), new PointF(x + w, y + h * 0.32f), new PointF(x + w * 0.92f, y + h * 0.78f),
                new PointF(x + w * 0.7f, y + h * 0.9f), new PointF(x + w * 0.3f, y + h * 0.9f), new PointF(x + w * 0.08f, y + h * 0.78f), new PointF(x, y + h * 0.32f)
            });
            Oh.SoftShadow(g, hp, w * 0.05f, h * 0.08f, w * 0.08f, 170);
            using (var b = new LinearGradientBrush(new RectangleF(x - 0.01f, 0, w + 0.02f, 1), Color.Black, Color.Black, 0f)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.FromArgb(14, 15, 17), Color.FromArgb(52, 55, 60), Color.FromArgb(78, 82, 88), Color.FromArgb(30, 31, 34), Color.FromArgb(8, 8, 9) },
                    Positions = new[] { 0f, 0.28f, 0.42f, 0.72f, 1f }
                }
            }) g.FillPath(b, hp);
            // pivot de dalt (ressalt arrodonit)
            var knob = new RectangleF(Cx - w * 0.14f, y - h * 0.12f, w * 0.28f, h * 0.22f);
            using (var kp = Oh.RoundRect(knob, w * 0.12f))
            using (var kb = new LinearGradientBrush(new RectangleF(knob.X - 0.01f, 0, knob.Width + 0.02f, 1), Color.FromArgb(20, 21, 24), Color.FromArgb(90, 94, 100), 0f)) g.FillPath(kb, kp);
            using (var pe = new Pen(Color.FromArgb(220, 4, 4, 5), 0.7f)) g.DrawPath(pe, hp);
        }
    }

    /// <summary>PARK BRK: maneta negra amb el rètol i una agulla metàl·lica; gira 90° entre OFF (amunt) i ON (a la dreta).</summary>
    internal sealed class OhParkBrake : OhElement
    {
        readonly float _cx, _cy, _w, _h, _spike; public bool On; float _ang;
        public OhParkBrake(string id, float cx, float cy, float w, float h, float spike)
        {
            Id = id; _cx = cx; _cy = cy; _w = w; _h = h; _spike = spike;
            float e = Math.Max(w / 2f, h / 2f + spike) + 4;
            Bounds = new RectangleF(cx - e, cy - e, e * 2, e * 2);
        }
        public override bool HitTest(PointF p)
        {
            float dx = p.X - _cx, dy = p.Y - _cy;
            return dx * dx + dy * dy <= (_w * 0.55f) * (_w * 0.55f);
        }
        public override IEnumerable<RectangleF> HitAreas { get { float r = _w * 0.55f; yield return new RectangleF(_cx - r, _cy - r, r * 2, r * 2); } }
        public override int State => On ? 1 : 0;
        public override bool Animating => Math.Abs(_ang - (On ? 90 : 0)) > 0.05f;
        public override void Tick(float dt) => _ang = Approach(_ang, On ? 90 : 0, dt * 300f);
        public override void Tap(PointF p) { On = !On; Owner.RaiseCommand(Id, On ? "ON" : "OFF"); Changed(); }
        public override void Draw(Graphics g)
        {
            var st = g.Save();
            g.TranslateTransform(_cx, _cy);
            g.RotateTransform(_ang);
            // agulla metàl·lica (cap amunt)
            float sb = _h * 0.5f, top = -_h / 2f - _spike;
            var spike = new[] { new PointF(-sb, -_h * 0.3f), new PointF(sb, -_h * 0.3f), new PointF(sb * 0.12f, top), new PointF(-sb * 0.12f, top) };
            using (var shp = new SolidBrush(Color.FromArgb(90, 0, 0, 0))) g.FillPolygon(shp, spike.Select(q => new PointF(q.X + 2f, q.Y + 3f)).ToArray());
            using (var mb = new LinearGradientBrush(new RectangleF(-sb - 0.01f, 0, sb * 2 + 0.02f, 1), Color.Black, Color.Black, 0f)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.FromArgb(40, 42, 46), Color.FromArgb(160, 164, 170), Color.FromArgb(248, 250, 252), Color.FromArgb(120, 124, 130), Color.FromArgb(30, 32, 36) },
                    Positions = new[] { 0f, 0.3f, 0.5f, 0.7f, 1f }
                }
            }) g.FillPolygon(mb, spike);
            // maneta
            var bar = new RectangleF(-_w / 2, -_h / 2, _w, _h);
            using (var bp = Oh.RoundRect(bar, _h * 0.3f))
            {
                Oh.SoftShadow(g, bp, 2f, 3f, 4f, 160);
                using (var b = new LinearGradientBrush(new RectangleF(bar.X, bar.Y - 0.01f, bar.Width, bar.Height + 0.02f), Color.FromArgb(70, 72, 76), Color.FromArgb(8, 8, 9), 90f)) g.FillPath(b, bp);
                using var pe = new Pen(Color.FromArgb(150, 170, 176, 182), 0.9f); g.DrawPath(pe, bp);
            }
            Oh.Text(g, "PARK BRK", 0, _h * 0.04f, _h * 0.58f, Color.White);
            g.Restore(st);
        }
    }

    /// <summary>
    /// GRAVITY GEAR EXTN: disc fosc amb la placa vermella i, a sota, la maneta negra en T. Tocar-la la fa girar tres voltes
    /// (PULL &amp; TURN) i, tornant-la a tocar, tres voltes enrere.
    /// </summary>
    internal sealed class OhGravityGear : OhElement
    {
        readonly float _cx, _cy, _r, _k; public bool Extended; float _rot;
        public OhGravityGear(string id, float cx, float cy, float r, float k)
        {
            Id = id; _cx = cx; _cy = cy; _r = r; _k = k;
            Bounds = RectangleF.FromLTRB(cx - r * 1.05f, cy - r * 1.05f, cx + r * 1.05f, 3205 * k);
        }
        public override bool HitTest(PointF p)
        {
            float dx = p.X - _cx, dy = p.Y - _cy;
            return dx * dx + dy * dy <= _r * _r * 1.1f || (Math.Abs(dx) < 100 * _k && p.Y > _cy && p.Y < 3200 * _k);
        }
        public override IEnumerable<RectangleF> HitAreas { get { yield return new RectangleF(_cx - _r, _cy - _r, _r * 2, 3200 * _k - (_cy - _r)); } }
        public override int State => Extended ? 1 : 0;
        public override bool Animating => Math.Abs(_rot - (Extended ? 1080 : 0)) > 0.1f;
        public override void Tick(float dt) => _rot = Approach(_rot, Extended ? 1080 : 0, dt * 900f);
        public override void Tap(PointF p) { Extended = !Extended; Owner.RaiseCommand(Id, Extended ? "DOWN" : "UP"); Changed(); }
        public override void Draw(Graphics g)
        {
            float k = _k;
            // disc fosc amb vora grisa
            var o = new RectangleF(_cx - _r, _cy - _r, _r * 2, _r * 2);
            using (var rb = new LinearGradientBrush(RectangleF.Inflate(o, 4 * k, 4 * k), Color.FromArgb(200, 206, 212), Color.FromArgb(90, 98, 106), 60f)) g.FillEllipse(rb, RectangleF.Inflate(o, 4 * k, 4 * k));
            using (var db = new LinearGradientBrush(o, Color.FromArgb(48, 50, 54), Color.FromArgb(10, 10, 12), 70f)) g.FillEllipse(db, o);
            // maneta en T (fixa) sota el disc
            void Dark(RectangleF r, float rad)
            {
                using var p = Oh.RoundRect(r, rad);
                Oh.SoftShadow(g, p, 2f, 3f, 4f, 140);
                using var b = new LinearGradientBrush(new RectangleF(r.X - 0.01f, r.Y, r.Width + 0.02f, r.Height), Color.Black, Color.Black, 0f)
                {
                    InterpolationColors = new ColorBlend
                    {
                        Colors = new[] { Color.FromArgb(8, 8, 9), Color.FromArgb(46, 48, 52), Color.FromArgb(70, 73, 78), Color.FromArgb(18, 19, 21), Color.FromArgb(4, 4, 5) },
                        Positions = new[] { 0f, 0.3f, 0.42f, 0.75f, 1f }
                    }
                };
                g.FillPath(b, p);
            }
            Dark(RectangleF.FromLTRB(938 * k, 2985 * k, 1012 * k, 3118 * k), 6 * k);
            Dark(RectangleF.FromLTRB(880 * k, 3006 * k, 1072 * k, 3074 * k), 10 * k);
            // pom de bolet a baix de tot
            var mush = RectangleF.FromLTRB(904 * k, 3112 * k, 1050 * k, 3200 * k);
            using (var mp = new GraphicsPath())
            {
                mp.AddPolygon(new[] { new PointF(950 * k, 3112 * k), new PointF(1002 * k, 3112 * k), new PointF(1050 * k, 3168 * k), new PointF(1040 * k, 3200 * k), new PointF(914 * k, 3200 * k), new PointF(904 * k, 3168 * k) });
                Oh.SoftShadow(g, mp, 2f, 3f, 4f, 140);
                using var mb = new LinearGradientBrush(new RectangleF(mush.X - 0.01f, mush.Y, mush.Width + 0.02f, mush.Height), Color.Black, Color.Black, 0f)
                {
                    InterpolationColors = new ColorBlend
                    {
                        Colors = new[] { Color.FromArgb(10, 10, 12), Color.FromArgb(60, 62, 66), Color.FromArgb(150, 154, 158), Color.FromArgb(40, 42, 46), Color.FromArgb(6, 6, 7) },
                        Positions = new[] { 0f, 0.3f, 0.48f, 0.72f, 1f }
                    }
                };
                g.FillPath(mb, mp);
            }
            // placa vermella (gira amb la maneta)
            var st = g.Save();
            g.TranslateTransform(_cx, _cy);
            g.RotateTransform(_rot);
            var plate = RectangleF.FromLTRB(-38 * k, -66 * k, 38 * k, 114 * k);
            using (var pp = Oh.RoundRect(plate, 6 * k))
            {
                Oh.SoftShadow(g, pp, 1.5f, 2.5f, 3f, 150);
                using (var b = new LinearGradientBrush(plate, Color.FromArgb(244, 104, 74), Color.FromArgb(214, 70, 46), 90f)) g.FillPath(b, pp);
                using var pe = new Pen(Color.White, 1.2f); g.DrawPath(pe, pp);
            }
            Oh.Text(g, "GRAVITY", 0, -46 * k, 8.2f, Color.White, false);
            Oh.Text(g, "GEAR", 0, -17 * k, 12f, Color.White, false);
            Oh.Text(g, "EXTN", 0, 9 * k, 12f, Color.White, false);
            Oh.Text(g, "PULL", 0, 71 * k, 8.2f, Color.White, false);
            Oh.Text(g, "&", 0, 88 * k, 8.2f, Color.White, false);
            Oh.Text(g, "TURN", 0, 104 * k, 8.2f, Color.White, false);
            g.Restore(st);
        }
    }
}
