using System;
using System.Collections.Generic;

namespace panelAIR1000
{
    /// <summary>
    /// Contingut de la pantalla d'una MCDU (ToLiss A320, X-Plane): 14 files × 24 columnes. Cada cel·la té el caràcter, el
    /// color (w blanc, g verd, b blau, a àmbar, y groc, m magenta, s símbol especial) i si és de lletra petita.
    ///   fila 0: títol (title gran + stitle petit)
    ///   files 1..12: label1, cont1, label2, cont2 … (label: petita; labelNL: gran; cont: gran; scont: petita)
    ///   fila 13: scratchpad (spw blanc / spa àmbar, gran)
    /// Es construeix superposant les cadenes de tots els colors a la mateixa posició (on una no té text, hi ha espais).
    /// </summary>
    public sealed class McduScreen : IEquatable<McduScreen>
    {
        public const int Rows = 14, Cols = 24;
        public readonly char[,] Ch = new char[Rows, Cols];
        public readonly char[,] Col = new char[Rows, Cols];
        public readonly bool[,] Small = new bool[Rows, Cols];
        /// <summary>Fletxes ↑↓ de la cantonada de baix a la dreta (AirbusFBW/MCDUnVertSlewKeys; comprovat: 1 a F-PLN, 0 sense fletxes).</summary>
        public int VertSlew;

        public McduScreen()
        {
            for (int r = 0; r < Rows; r++) for (int c = 0; c < Cols; c++) { Ch[r, c] = ' '; Col[r, c] = 'w'; }
        }

        static readonly char[] Colors = { 'w', 'g', 'b', 'a', 'y', 'm', 's' };

        /// <summary>Construeix la pantalla de la MCDU <paramref name="unit"/> (1 o 2) a partir del text de cada dataref.</summary>
        public static McduScreen Build(int unit, Func<string, string?> get)
        {
            var s = new McduScreen();
            string P = "AirbusFBW/MCDU" + unit;
            void Lay(int row, string baseName, bool small)
            {
                foreach (char col in Colors)
                {
                    var t = get(P + baseName + col);
                    if (string.IsNullOrEmpty(t)) continue;
                    for (int i = 0; i < t.Length && i < Cols; i++)
                    {
                        if (t[i] == ' ' || t[i] == '\0') continue;
                        s.Ch[row, i] = t[i]; s.Col[row, i] = col; s.Small[row, i] = small;
                    }
                }
            }
            Lay(0, "title", false);
            Lay(0, "stitle", true);
            for (int i = 1; i <= 6; i++)
            {
                Lay(2 * i - 1, "label" + i, true);
                Lay(2 * i - 1, "label" + i + "L", false);
                Lay(2 * i, "cont" + i, false);
                Lay(2 * i, "scont" + i, true);
            }
            Lay(13, "sp", false);
            // "CLR" sol al scratchpad: el ToLiss el publica a la columna 0 però el dibuixa a partir de la columna 5
            // (mesurat sobre la seva pantalla)
            if (s.RowText(13).TrimEnd() == "CLR")
            {
                const int Shift = 5;
                for (int c = Cols - 1; c >= Shift; c--) { s.Ch[13, c] = s.Ch[13, c - Shift]; s.Col[13, c] = s.Col[13, c - Shift]; s.Small[13, c] = s.Small[13, c - Shift]; }
                for (int c = 0; c < Shift; c++) s.Ch[13, c] = ' ';
            }
            return s;
        }

        /// <summary>Text d'una fila (sense colors).</summary>
        public string RowText(int row)
        {
            var sb = new System.Text.StringBuilder(Cols);
            for (int c = 0; c < Cols; c++) sb.Append(Ch[row, c]);
            return sb.ToString();
        }

        public bool Equals(McduScreen? o)
        {
            if (o == null || VertSlew != o.VertSlew) return false;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (Ch[r, c] != o.Ch[r, c] || Col[r, c] != o.Col[r, c] || Small[r, c] != o.Small[r, c]) return false;
            return true;
        }
        public override bool Equals(object? obj) => Equals(obj as McduScreen);
        public override int GetHashCode() => 0;
    }
}
