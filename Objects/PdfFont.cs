using System;

using PdfBuilder.Fonts;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // tipografia
    public abstract class PdfFont : PdfElement
    {
        public string Typeface { get; }
        public bool Bold { get; }
        public bool Italic { get; }
        public bool Kerning { get; set; } = false;
        public string TextEncoding { get; set; }
        public int CodePage { get; set; }
        internal PdfFontMetrics FontMetrics { get { return _metrics; } }
        private PdfFontMetrics _metrics;

        public PdfFont(string typeface, bool bold = false, bool italic = false)
        {
            Typeface = typeface;
            Bold = bold;
            Italic = italic;
            _metrics = PdfFontDatabase.GetFont(typeface, bold, italic);
        }

        protected override string GetElementName() => $"/F{ElementNumber}";
        internal override byte[] Contents() => null;
    }
}