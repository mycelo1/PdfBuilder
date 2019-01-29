using System;
using System.Collections.Generic;
using System.Linq;

using Mycelo.Pdf.Types;

namespace Mycelo.Pdf.Fonts
{
    public class PdfFontMetrics
    {
        public string FontName { get; }
        public string FamilyName { get; }
        public bool IsFixedPitch { get; }
        public bool IsBold { get; }
        public bool IsItalic { get; }
        public int ItalicAngle { get; }
        public int UnderlinePosition { get; }
        public int UnderlineThickness { get; }
        public PdfBoundingBox FontBBox { get; }
        public int LineHeight { get { return FontBBox.URy - FontBBox.LLy; } }

        private Dictionary<int, PdfGlyphMetrics> glyphMetrics;
        private Dictionary<int, int> kerningPairs;

        public PdfFontMetrics(string font_name, string family_name, bool is_fixed_pitch, bool is_bold, bool is_italic, int italic_angle, int ul_pos, int ul_th, int bb_llx, int bb_lly, int bb_urx, int bb_ury)
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

        public (double, double)[] TextCoordinates(double llx, double lly, double urx, double ury, TextAlign h_align, TextAlign v_align, int scale, string[] lines)
        {
            List<(double, double)> result = new List<(double, double)>();
            List<int> lines_width = new List<int>(lines.Length);

            int block_height = lines.Length * LineHeight;
            int block_width = 0;

            int box_llx_units = PointsToUnits(scale, llx);
            int box_lly_units = PointsToUnits(scale, lly);
            int box_urx_units = PointsToUnits(scale, urx);
            int box_ury_units = PointsToUnits(scale, ury);

            foreach (string line in lines)
            {
                int line_width = CalcTextLength(line);
                lines_width.Insert(0, line_width);
                if (line_width > block_width)
                {
                    block_width = line_width;
                }
            }

            int block_rllx = CalcHAlign(h_align, box_urx_units - box_llx_units, block_width);
            int block_rlly = CalcVAlign(v_align, box_ury_units - box_lly_units, block_height);
            int line_ry = block_rlly - FontBBox.LLy;

            foreach (string line in lines.Reverse())
            {
                int line_llx = box_llx_units + block_rllx + CalcHAlign(h_align, block_width, lines_width[0]);
                int line_lly = box_lly_units + line_ry;
                result.Insert(0, (UnitsToPoints(scale, line_llx), UnitsToPoints(scale, line_lly)));
                lines_width.RemoveAt(0);
                line_ry += LineHeight;
            }

            return result.ToArray();
        }

        private double UnitsToPoints(int scale, int units)
        {
            return Math.Round(scale * units / 1000f, 3);
        }

        private int PointsToUnits(int scale, double points)
        {
            return (int)Math.Round(points / scale * 1000f);
        }

        private int CalcHAlign(TextAlign align, int range, int content)
        {
            switch (align)
            {
                case TextAlign.Middle:
                    return (int)Math.Round((range - content) / 2f);
                case TextAlign.Far:
                    return range - content;
                default:
                    return 0;
            }
        }

        private int CalcVAlign(TextAlign align, int range, int content)
        {
            switch (align)
            {
                case TextAlign.Middle:
                    return (int)Math.Round((range - content) / 2f);
                case TextAlign.Near:
                    return range - content;
                default:
                    return 0;
            }
        }

        private int CalcTextLength(string text_line)
        {
            int width = 0;
            PdfGlyphMetrics last_char = null;

            foreach (char char_glyph in text_line)
            {
                int int_glyph = (int)char_glyph;
                PdfGlyphMetrics this_char = glyphMetrics[int_glyph];
                width += this_char.Width;
                if (last_char != null)
                {
                    string kerning_pair = $"{last_char.GlyphName}+{this_char.GlyphName}";
                    if (kerningPairs.TryGetValue(kerning_pair.GetHashCode(), out int kerning))
                    {
                        width += kerning;
                    }
                }
                last_char = this_char;
            }
            return width;
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

    public class PdfGlyphMetrics
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

    public class PdfBoundingBox
    {
        public int LLx { get; }
        public int LLy { get; }
        public int URx { get; }
        public int URy { get; }

        public PdfBoundingBox(int llx, int lly, int urx, int ury)
        {
            LLx = llx;
            LLy = lly;
            URx = urx;
            URy = ury;
        }
    }
}