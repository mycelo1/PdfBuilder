using System;
using System.Linq;

using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // linha de texto (dentro do box)
    public class PdfTextLine
    {
        public string TextLine { get; }
        public int TextScale { get; }
        public PdfFont Font { get { return _font; } }
        public PdfAlign HAlign { get; }
        public PdfColor Color { get; }
        public PdfPage Page { get { return _page; } }

        private PdfPage _page;
        private PdfFont _font;
        private string _typeface;
        private bool _bold;
        private bool _italic;

        public PdfTextLine(string text_line, int text_scale, PdfFont font, PdfAlign h_align, PdfColor color = null)
        {
            TextLine = text_line;
            TextScale = text_scale;
            HAlign = h_align;
            Color = color;
            _font = font;
        }

        public PdfTextLine(string text_line, int text_scale, string typeface, PdfAlign h_align, bool bold = false, bool italic = false, PdfColor color = null)
        {
            TextLine = text_line;
            TextScale = text_scale;
            HAlign = h_align;
            Color = color;
            _typeface = typeface;
            _bold = bold;
            _italic = italic;
        }

        internal void Initialize(PdfPage page)
        {
            _page = page;
            if ((_font == null) && !String.IsNullOrEmpty(_typeface))
            {
                _font = page.Elements
                    .Where(x => x is PdfFont)
                        .Select(x => x as PdfFont)
                            .FirstOrDefault(x =>
                                (x.Typeface == _typeface) &&
                                (x.Bold == _bold) &&
                                (x.Italic == _italic));

                if (_font == null)
                {
                    _font = new PdfType1Font(_typeface, _bold, _italic);
                    page.AddFont(_font);
                }
            }
        }
    }
}