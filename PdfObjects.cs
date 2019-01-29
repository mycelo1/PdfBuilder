using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Mycelo.Pdf.Fonts;
using Mycelo.Pdf.Types;

namespace Mycelo.Pdf.Objects
{
    public class PdfFile
    {
        public PdfDocument PdfDocument { get; }
        public PdfCatalog PdfCatalog { get; }
        public IEnumerable<PdfObject> ObjectList { get { return object_list; } }
        private readonly List<PdfObject> object_list = new List<PdfObject>();

        public PdfFile()
        {
            PdfDocument = new PdfDocument(this);
            PdfCatalog = new PdfCatalog(this, PdfDocument);
        }

        public int AddObject(PdfObject pdf_object)
        {
            object_list.Add(pdf_object);
            return object_list.Count - 1;
        }
    }

    public abstract class PdfObject
    {
        public int ObjectNumber { get; }
        public int ObjectVersion { get; }
        public int ObjectXRef { get { return object_xref; } }
        public PdfObject Parent { get; }
        protected int object_xref;

        public PdfObject(PdfFile pdf_file, PdfObject parent, int version)
        {
            Parent = parent;
            if (pdf_file != null)
            {
                ObjectNumber = pdf_file.AddObject(this);
                ObjectVersion = version;
            }
        }

        static PdfObject()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public virtual byte[] Data(int xref)
        {
            object_xref = xref;
            List<byte> data = new List<byte>();
            data.AddRange(Encoding.ASCII.GetBytes($"{ObjectNumber} {ObjectVersion} obj\n"));
            data.AddRange(InternalData() ?? new byte[0]);
            data.AddRange(Encoding.ASCII.GetBytes($"endobj\n"));
            return data.ToArray();
        }

        protected string FormatDouble(double number, int digits = 3)
        {
            return Math.Round(number, digits).ToString(CultureInfo.InvariantCulture);
        }

        protected abstract byte[] InternalData();
    }

    public class PdfDocument : PdfObject
    {
        public PdfDocument(PdfFile pdf_file) : base(pdf_file, null, 65535) { }
        protected override byte[] InternalData() => null;
    }

    public class PdfCatalog : PdfObject
    {
        public PdfPageIndex Index { get; }

        public PdfCatalog(PdfFile pdf_file, PdfDocument parent) : base(pdf_file, parent, 0)
        {
            Index = new PdfPageIndex(pdf_file, this);
        }

        protected override byte[] InternalData()
        {
            return Encoding.ASCII.GetBytes($"<< /Pages {Index.ObjectNumber} {Index.ObjectVersion} R /Type /Catalog >>\n");
        }
    }

    public class PdfPageIndex : PdfObject
    {
        public readonly List<PdfPage> Pages = new List<PdfPage>();
        public PdfPageIndex(PdfFile pdf_file, PdfCatalog parent) : base(pdf_file, parent, 0) { }

        protected override byte[] InternalData()
        {
            StringBuilder kids = new StringBuilder();
            foreach (PdfPage page in Pages)
            {
                kids.Append($"{page.ObjectNumber} {page.ObjectVersion} R ");
            }
            return Encoding.ASCII.GetBytes($"<< /Count {Pages.Count} /Kids [ {kids.ToString()}] /Type /Pages >>\n");
        }
    }

    public class PdfPage : PdfObject
    {
        public int Width { get; }
        public int Height { get; }
        public PdfContents HolderContents { get; }
        public readonly List<PdfElement> Elements = new List<PdfElement>();

        public PdfPage(PdfFile pdf_file, PdfPageIndex parent, int width, int height) : base(pdf_file, parent, 0)
        {
            Width = width;
            Height = height;
            HolderContents = new PdfContents(pdf_file, this);
            parent.Pages.Add(this);
        }

        protected override byte[] InternalData()
        {
            StringBuilder resources = new StringBuilder();
            if (Elements.Count > 0)
            {
                resources.Append("/Resources << ");
                foreach (PdfElement element in Elements)
                {
                    if (element is PdfXObject)
                    {
                        PdfXObject x_object = element as PdfXObject;
                        resources.Append($"/XObject << {x_object.ElementName} {x_object.ObjectNumber} {x_object.ObjectVersion} R >> ");
                    }
                    else if (element is PdfFont)
                    {
                        PdfFont font = element as PdfFont;
                        resources.Append($"/Font << {font.ElementName} {font.ObjectNumber} {font.ObjectVersion} R >> ");
                    }
                }
                resources.Append(">> ");
            }
            return Encoding.ASCII.GetBytes(
                $"<< /Contents {HolderContents.ObjectNumber} 0 R /Parent {Parent.ObjectNumber} {Parent.ObjectVersion} R " +
                $"/MediaBox [ 0 0 {Width} {Height} ] " +
                resources.ToString() +
                $"/Type /Pages >>\n");
        }
    }

    public class PdfContents : PdfObject
    {
        public PdfContents(PdfFile pdf_file, PdfPage parent) : base(pdf_file, parent, 0) { }

        protected override byte[] InternalData()
        {
            List<byte> contents_stream = new List<byte>();
            foreach (PdfElement element in ((PdfPage)Parent).Elements)
            {
                contents_stream.AddRange(element.Contents() ?? new byte[0]);
            }
            List<byte> contents = new List<byte>();
            contents.AddRange(Encoding.ASCII.GetBytes(
                $"<< /Length {contents_stream.Count} >>\n" +
                $"stream\n"
                ));
            contents.AddRange(contents_stream);
            contents.AddRange(Encoding.ASCII.GetBytes($"endstream\n"));
            return contents.ToArray();
        }
    }

    public abstract class PdfElement : PdfObject
    {
        protected int ElementNumber;
        public string ElementName { get { return GetElementName(); } }

        public PdfElement(PdfFile pdf_file, PdfPage parent, int version) : base(pdf_file, parent, version)
        {
            parent.Elements.Add(this);
            ElementNumber = parent.Elements.Count - 1;
        }

        protected abstract string GetElementName();
        public abstract byte[] Contents();
    }

    public abstract class PdfXObject : PdfElement
    {
        public int Width { get; }
        public int Height { get; }
        public int HPos { get; }
        public int VPos { get; }

        public PdfXObject(PdfFile pdf_file, PdfPage parent, int width, int height, int h_pos, int v_pos)
            : base(pdf_file, parent, 0)
        {
            Width = width;
            Height = height;
            HPos = h_pos;
            VPos = v_pos;
        }
    }

    public class PdfImage : PdfXObject
    {
        public int BitsPerColor { get; } = 8;
        public int ImageWidth { get; }
        public int ImageHeight { get; }
        public byte[] ImageData { get; }

        public PdfImage(PdfFile pdf_file, PdfPage parent, Bitmap bitmap, int width, int height, int h_pos, int v_pos)
        : base(pdf_file, parent, width, height, h_pos, v_pos)
        {
            ImageWidth = bitmap.Width;
            ImageHeight = bitmap.Height;
            using (EncoderParameters encoder_params = new EncoderParameters(1))
            using (encoder_params.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, BitsPerColor * 3))
            using (MemoryStream memory = new MemoryStream())
            {
                ImageCodecInfo encoder = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
                bitmap.Save(memory, encoder, encoder_params);
                ImageData = memory.ToArray();
            }
        }

        public PdfImage(PdfFile pdf_file, PdfPage parent, byte[] jpeg, int jpeg_width, int jpeg_height, int width, int height, int h_pos, int v_pos)
        : base(pdf_file, parent, width, height, h_pos, v_pos)
        {
            ImageWidth = jpeg_width;
            ImageHeight = jpeg_height;
            ImageData = jpeg;
        }

        protected override byte[] InternalData()
        {
            List<byte> data = new List<byte>();
            data.AddRange(Encoding.ASCII.GetBytes(
                $"<< /BitsPerComponent {BitsPerColor} /ColorSpace /DeviceRGB /Filter /DCTDecode /Subtype /Image /Type /XObject " +
                $"/Height {ImageHeight} /Width {ImageWidth} /Length {ImageData.Length + 1} >>\n" +
                $"stream\n"
                ));
            data.AddRange(ImageData);
            data.AddRange(Encoding.ASCII.GetBytes($"\n"));
            data.AddRange(Encoding.ASCII.GetBytes($"endstream\n"));
            return data.ToArray();
        }

        protected override string GetElementName() => $"/I{ElementNumber}";

        public override byte[] Contents()
        {
            return Encoding.ASCII.GetBytes(
                $"q\n" +
                $"{Width} 0 0 {Height} {HPos} {VPos} cm\n" +
                $"{ElementName} Do\n" +
                $"Q\n");
        }
    }

    public abstract class PdfFont : PdfElement
    {
        public string Typeface { get; }
        public string TextEncoding { get; }
        public bool Bold { get; }
        public bool Italic { get; }

        public PdfFont(PdfFile pdf_file, PdfPage parent, string typeface, bool bold, bool italic, string encoding) : base(pdf_file, parent, 0)
        {
            Typeface = typeface;
            TextEncoding = encoding;
            Bold = bold;
            Italic = italic;
        }

        protected override string GetElementName() => $"/F{ElementNumber}";
        public override byte[] Contents() => null;
    }

    public class PdfType1Font : PdfFont
    {
        public PdfFontMetrics FontMetrics { get; }
        public PdfType1Font(PdfFile pdf_file, PdfPage parent, string typeface, bool bold, bool italic, string encoding)
            : base(pdf_file, parent, typeface, bold, italic, encoding)
        {
            FontMetrics = PdfFontDatabase.GetFont(typeface, bold, italic);
        }

        protected override byte[] InternalData()
        {
            return Encoding.ASCII.GetBytes($"<< /Type /Font /Subtype /Type1 /Name {ElementName} /BaseFont /{Typeface} /Encoding /{TextEncoding} >>\n");
        }
    }

    public abstract class PdfText : PdfElement
    {
        public PdfType1Font Font { get; }
        public int TextScale { get; }
        public int BoxLLx { get; }
        public int BoxLLy { get; }
        public int BoxURx { get; }
        public int BoxURy { get; }
        public TextAlign HAlign { get; }
        public TextAlign VAlign { get; }
        public string[] TextLines { get; }
        public bool DrawRectangle { get; }

        public PdfText(PdfFile pdf_file, PdfPage parent, PdfType1Font font, int scale, int box_ulx, int box_uly, int box_lrx, int box_lry, TextAlign h_align, TextAlign v_align, string text, bool rectangle = false)
            : base(null, parent, 0)
        {
            Font = font;
            TextScale = scale;
            BoxLLx = box_ulx;
            BoxLLy = parent.Height - box_lry;
            BoxURx = box_lrx;
            BoxURy = parent.Height - box_uly;
            HAlign = h_align;
            VAlign = v_align;
            TextLines = text.Split(new string[] { "\n\r", "\n", "\r" }, StringSplitOptions.None);
            DrawRectangle = rectangle;
        }

        protected override string GetElementName() => String.Empty;
        protected override byte[] InternalData() => null;
    }

    public class PdfWinAnsiText : PdfText
    {
        public PdfWinAnsiText(PdfFile pdf_file, PdfPage parent, PdfType1Font font, int scale, int box_ulx, int box_uly, int box_lrx, int box_lry, TextAlign h_align, TextAlign v_align, string text, bool rectangle = false)
            : base(pdf_file, parent, font, scale, box_ulx, box_uly, box_lrx, box_lry, h_align, v_align, text, rectangle) { }

        public override byte[] Contents()
        {
            List<byte> contents = new List<byte>();
            contents.AddRange(Encoding.ASCII.GetBytes(
                $"BT\n" +
                $"{Font.ElementName} {TextScale} Tf\n"));
            if (DrawRectangle)
            {
                contents.AddRange(Encoding.ASCII.GetBytes(
                    $"q\n" +
                    $"0.5 0.5 0.5 rg\n" +
                    $"{BoxLLx} {BoxLLy} {BoxURx - BoxLLx} {BoxURy - BoxLLy} re f\n" +
                    $"Q\n"));
            }
            var coordinates = Font.FontMetrics.TextCoordinates(
                llx: BoxLLx,
                lly: BoxLLy,
                urx: BoxURx,
                ury: BoxURy,
                h_align: HAlign,
                v_align: VAlign,
                scale: TextScale,
                lines: TextLines);
            double pos_x = 0;
            double pos_y = 0;
            for (int index = 0; index < TextLines.Count(); index++)
            {
                byte[] pdf_string = Encoding.GetEncoding(1252).GetBytes(TextLines[index]);
                contents.AddRange(Encoding.ASCII.GetBytes(
                    $"{FormatDouble(coordinates[index].Item1 - pos_x)} {FormatDouble(coordinates[index].Item2 - pos_y)} Td\n" +
                    $"("));
                foreach (byte text_byte in pdf_string)
                {
                    if ((text_byte == 0x28) || (text_byte == 0x29) || (text_byte == 0x5c))
                    {
                        contents.Add((byte)'\\');
                    }
                    contents.Add(text_byte);
                }
                contents.AddRange(Encoding.ASCII.GetBytes($") Tj\n"));
                pos_x = coordinates[index].Item1;
                pos_y = coordinates[index].Item2;
            }
            contents.AddRange(Encoding.ASCII.GetBytes($"ET\n"));
            return contents.ToArray();
        }
    }
}