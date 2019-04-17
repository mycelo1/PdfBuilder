using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PdfBuilder.Types;

namespace PdfBuilder.Fonts
{
    internal class PdfFontMetrics
    {
        public string FontName { get; }
        public string FamilyName { get; }
        public int CodePage { get; }
        public bool IsFixedPitch { get; }
        public bool IsBold { get; }
        public bool IsItalic { get; }
        public double ItalicAngle { get; }
        public int UnderlinePosition { get; }
        public int UnderlineThickness { get; }
        public PdfBoundingBox FontBBox { get; }
        public double LineHeight { get { return FontBBox.URy - FontBBox.LLy; } }

        private Dictionary<int, PdfGlyphMetrics> glyphMetrics;
        private Dictionary<int, int> kerningPairs;

        private const char char_space = '\x20';
        private const char char_emdash = (char)0x2014;

        public PdfFontMetrics(string font_name, string family_name, bool is_fixed_pitch, bool is_bold, bool is_italic, double italic_angle, int ul_pos, int ul_th, int bb_llx, int bb_lly, int bb_urx, int bb_ury)
        {
            FontName = font_name;
            FamilyName = family_name;
            IsFixedPitch = is_fixed_pitch;
            IsBold = is_bold;
            IsItalic = is_italic;
            ItalicAngle = italic_angle;
            UnderlinePosition = ul_pos;
            UnderlineThickness = ul_th;
            FontBBox = new PdfBoundingBox(bb_llx, bb_lly, bb_urx, bb_ury);
            glyphMetrics = new Dictionary<int, PdfGlyphMetrics>();
            kerningPairs = new Dictionary<int, int>();
        }

        public static (PdfTextOut[], double width, double height) TextCoordinates(double width, IEnumerable<PdfTextIn> lines)
        {
            var result = new List<PdfTextOut>();
            var lines_width = new List<(PdfTextIn text_in, string text, int width, bool broken)>();

            double block_width_points = 0;
            double block_height_points = 0;

            // calcular largura e altura de todas as linhas
            foreach (PdfTextIn line in lines)
            {
                // iterar linhas depois da quebra
                int box_width_units = PointsToUnits(line.TextScale, width);
                foreach (var line_width in line.Metrics.LineBreak(line.Text, box_width_units, line.Kerning))
                {
                    block_width_points = Math.Max(block_width_points, UnitsToPoints(line.TextScale, line_width.width));
                    block_height_points += UnitsToPoints(line.TextScale, line.Metrics.LineHeight);
                    lines_width.Add((line, line_width.text, line_width.width, line_width.broken));
                }
            }

            // posição vertical inicial
            double margin_y_points = 0;

            // calcular posição horizontal de cada linha da última à primeira
            foreach (var tuple in lines_width.ToArray().Reverse())
            {
                if (tuple.text != null) // não é uma linha em branco
                {
                    // largura da linha
                    double line_w_points = UnitsToPoints(tuple.text_in.TextScale, tuple.width);

                    // posição vertical da linha
                    double line_y_points = margin_y_points - UnitsToPoints(tuple.text_in.TextScale, tuple.text_in.Metrics.FontBBox.LLy);

                    // contar espaços entre as palavras (para cálculo do alinhamento justificado)
                    int spaces = tuple.broken && tuple.text_in.HAlign == PdfAlign.Justify ? tuple.text.Count(x => x == char_space) : 0;

                    // se alinhamento justificado, reverter para alinhamento à esquerda (cálculo é feito aqui)
                    PdfAlign new_align = tuple.text_in.HAlign == PdfAlign.Justify ? PdfAlign.Near : tuple.text_in.HAlign;

                    // calcular pixels entre as palavras
                    double word_spacing = spaces > 0 ? (width - line_w_points) / spaces : 0;

                    // produzir resultado
                    result.Add(new PdfTextOut(
                        h_align: new_align,
                        font_name: tuple.text_in.FontName,
                        text_scale: tuple.text_in.TextScale,
                        width: line_w_points,
                        y: line_y_points,
                        word_spacing: word_spacing,
                        codepage: tuple.text_in.CodePage,
                        color: tuple.text_in.Color,
                        text: tuple.text));
                }
                // displace bottom margin
                margin_y_points += UnitsToPoints(tuple.text_in.TextScale, tuple.text_in.Metrics.LineHeight);
            }

            return (result.ToArray(), block_width_points, block_height_points);
        }

        private static double UnitsToPoints(int scale, double units)
        {
            return Math.Round(scale * units / 1000f, 3);
        }

        private static int PointsToUnits(int scale, double points)
        {
            return (int)Math.Round(points / scale * 1000f);
        }

        private IEnumerable<(string text, int width, bool broken)> LineBreak(string text_line, int max_width, bool kerning)
        {
            if (String.IsNullOrWhiteSpace(text_line))
            {
                yield return (null, 0, false);
            }
            else
            {
                var l_text = CalcCharWidths(text_line.Trim(char_space), kerning);
                while ((l_text.Count > 0) && (l_text.Sum(x => x.Item2) > max_width))
                {
                    // quebrar linha na largura
                    int fit_sum = 0;
                    var l_fit = l_text.TakeWhile(x => (fit_sum += x.Item2) <= max_width).ToList();
                    l_text = l_text.Skip(l_fit.Count).ToList();

                    // procurar último espaço
                    int? last_spc_pos = l_fit.Select((t, index) => new { t, index }).LastOrDefault(o => o.t.Item1 == char_space)?.index;
                    var l_left = l_fit.Take(last_spc_pos ?? 0).SkipWhile(x => x.Item1 == char_space).ToArray();
                    string l_left_string = new String(l_left.Select(x => x.Item1).ToArray());
                    int l_left_width = l_left.Sum(x => x.Item2);

                    if (!String.IsNullOrEmpty(l_left_string))
                    {
                        // quebrar linha no último espaço
                        l_text.InsertRange(0, l_fit.Skip((int)last_spc_pos + 1).ToList());
                        yield return (l_left_string, l_left_width, true);
                    }
                    else
                    {
                        // espaço não encontrado, quebrar linha no grafo ofensor
                        string l_fit_string = new String(l_fit.Select(x => x.Item1).ToArray()).Trim();
                        int l_fit_width = l_fit.Sum(x => x.Item2);
                        yield return (l_fit_string, l_fit_width, true);
                    }
                }

                if (l_text.Count > 0)
                {
                    string l_text_string = new String(l_text.Select(x => x.Item1).ToArray());
                    int l_text_width = l_text.Sum(x => x.Item2);
                    yield return (l_text_string, l_text_width, false);
                }
            }
        }

        // char, largura
        private List<(char, int)> CalcCharWidths(string text_line, bool kerning)
        {
            char? last_char = null;
            List<(char, int)> result = new List<(char, int)>(text_line.Length);
            foreach (char current_char in text_line)
            {
                if ((current_char <= char_space) && (last_char == char_space))
                {
                    // ignorar espaços duplos e caracteres de controle
                    continue;
                }
                char this_char = (char)Math.Max((int)current_char, (int)char_space);
                PdfGlyphMetrics char_metrics = glyphMetrics[(int)this_char];
                int width = char_metrics.Width;
                if (kerning && (last_char != null))
                {
                    string kerning_pair = $"{glyphMetrics[(int)last_char].GlyphName}+{char_metrics.GlyphName}";
                    if (kerningPairs.TryGetValue(kerning_pair.GetHashCode(), out int adjust))
                    {
                        width += adjust;
                    }
                }
                result.Add((this_char, width));
            }
            return result;
        }

        public void AddGlyphMetrics(string glyph_name, int width, int llx, int lly, int urx, int ury)
        {
            PdfGlyphMetrics glyph_metrics = new PdfGlyphMetrics(glyph_name, width, llx, lly, urx, ury);
            int codepoint = PdfFontCharTable.GetCodePoint(glyph_name);
            if (!glyphMetrics.ContainsKey(codepoint))
            {
                glyphMetrics.Add(codepoint, glyph_metrics);
            }
        }

        public void AddKerningPair(string kerning_pair, int kerning)
        {
            int hash = kerning_pair.GetHashCode();
            if (!kerningPairs.ContainsKey(hash))
            {
                kerningPairs.Add(hash, kerning);
            }
        }
    }

    internal class PdfGlyphMetrics
    {
        public string GlyphName { get; }
        public int Width { get; }
        public PdfBoundingBox BBox { get; }

        public PdfGlyphMetrics(string glyph_name, int width, int llx, int lly, int urx, int ury)
        {
            GlyphName = glyph_name;
            Width = width;
            BBox = new PdfBoundingBox(llx, lly, urx, ury);
        }
    }

    internal class PdfTextIn
    {
        public PdfFontMetrics Metrics { get; }
        public string FontName { get; }
        public int CodePage { get; }
        public int TextScale { get; }
        public string Text { get; }
        public PdfAlign HAlign { get; }
        public PdfColor Color { get; }
        public bool Kerning { get; }
        public PdfTextIn(PdfFontMetrics metrics, string font_name, int codepage, int text_scale, string text, PdfAlign h_align, PdfColor color, bool kerning = false)
        {
            Metrics = metrics;
            FontName = font_name;
            CodePage = codepage;
            TextScale = text_scale;
            Text = text;
            HAlign = h_align;
            Color = color;
            Kerning = kerning;
        }
    }

    internal class PdfTextOut
    {
        public PdfAlign HAlign { get; }
        public string FontName { get; }
        public int TextScale { get; }
        public double Width { get; }
        public double Y { get; }
        public double WordSpacing { get; }
        public int CodePage { get; }
        public PdfColor Color { get; }
        public string Text { get; }
        public PdfTextOut(PdfAlign h_align, string font_name, int text_scale, double width, double y, double word_spacing, int codepage, PdfColor color, string text)
        {
            HAlign = h_align;
            FontName = font_name;
            TextScale = text_scale;
            Width = width;
            Y = y;
            WordSpacing = word_spacing;
            CodePage = codepage;
            Color = color;
            Text = text;
        }
    }
}