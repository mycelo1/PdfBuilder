using System;

using PdfBuilder.Helpers;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // tipografia Type1 (fonte interna, codificação win-1252/ANSI)
    public class PdfType1Font : PdfFont
    {
        public static string DefaultEncoding = "WinAnsiEncoding";
        public static int DefaultCodePage = 1252;

        public PdfType1Font(string typeface, bool bold = false, bool italic = false)
            : base(typeface, bold, italic)
        {
            TextEncoding = DefaultEncoding;
            CodePage = DefaultCodePage;
        }

        protected override byte[] InternalData()
        {
            return Util.StringToBytes($"<< /Type /Font /Subtype /Type1 /Name {ElementName} /BaseFont /{FontMetrics.FontName} /Encoding /{TextEncoding} >>\n");
        }
    }
}