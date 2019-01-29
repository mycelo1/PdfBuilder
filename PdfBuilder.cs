using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using Mycelo.Pdf.Fonts;
using Mycelo.Pdf.Objects;
using Mycelo.Pdf.Types;

namespace Mycelo.Pdf
{
    public class PdfBuilder : IDisposable
    {
        private PdfFile pdf_file;

        public PdfBuilder()
        {
            pdf_file = new PdfFile();
        }

        public PdfPage AddPage(int width, int height)
        {
            return new PdfPage(pdf_file, pdf_file.PdfCatalog.Index, width, height);
        }

        public PdfImage AddImage(PdfPage page, Bitmap bitmap, int width, int height, int h_pos, int v_pos)
        {
            return new PdfImage(
                pdf_file: pdf_file,
                parent: page,
                bitmap: bitmap,
                width: width,
                height: height,
                h_pos: h_pos,
                v_pos: v_pos);
        }

        public PdfFont AddFont(PdfPage page, string typeface, bool bold, bool italic, string encoding)
        {
            return new PdfType1Font(
                pdf_file: pdf_file,
                parent: page,
                typeface: typeface,
                bold: bold,
                italic: italic,
                encoding: encoding);
        }

        public PdfText AddText(PdfPage page, PdfFont font, int scale, int ulx, int uly, int lrx, int lry, TextAlign h_align, TextAlign v_align, string text)
        {
            return new PdfWinAnsiText(
                pdf_file: pdf_file,
                parent: page,
                font: (PdfType1Font)font,
                scale: scale,
                box_ulx: ulx,
                box_uly: uly,
                box_lrx: lrx,
                box_lry: lry,
                h_align: h_align,
                v_align: v_align,
                text: text,
                rectangle: true);
        }

        public static byte[] Build(PdfFile pdf_file)
        {
            List<byte> pdf_bytes = new List<byte>();

            // header
            byte[] header = Encoding.ASCII.GetBytes("%PDF-1.4\n");
            pdf_bytes.AddRange(header);

            // objects
            foreach (PdfObject pdf_obj in pdf_file.ObjectList.Skip(1))
            {
                pdf_bytes.AddRange(pdf_obj.Data(pdf_bytes.Count));
            }

            // xref
            int xref_pos = pdf_bytes.Count;
            pdf_bytes.AddRange(Encoding.ASCII.GetBytes($"xref\n"));
            pdf_bytes.AddRange(Encoding.ASCII.GetBytes($"{pdf_file.ObjectList.First().ObjectNumber} {pdf_file.ObjectList.Count()}\n"));
            pdf_bytes.AddRange(Encoding.ASCII.GetBytes($"0000000000 65535 f \n"));
            foreach (PdfObject pdf_obj in pdf_file.ObjectList.Skip(1))
            {
                pdf_bytes.AddRange(
                    Encoding.ASCII.GetBytes(
                        $"{pdf_obj.ObjectXRef.ToString("D10")} " +
                        $"{pdf_obj.ObjectVersion.ToString("D5")} n \n"));
            }

            // trailer
            pdf_bytes.AddRange(Encoding.ASCII.GetBytes(
                $"trailer << /Root {pdf_file.PdfCatalog.ObjectNumber} {pdf_file.PdfCatalog.ObjectVersion} R " +
                $"/Size {pdf_file.ObjectList.Count()} >>\n"));
            pdf_bytes.AddRange(Encoding.ASCII.GetBytes($"startxref\n"));
            pdf_bytes.AddRange(Encoding.ASCII.GetBytes($"{xref_pos}\n"));
            pdf_bytes.AddRange(Encoding.ASCII.GetBytes($"%%EOF"));

            return pdf_bytes.ToArray();
        }

        public byte[] Build()
        {
            return Build(pdf_file);
        }

        public void Dispose()
        {
            //
        }
    }
}
