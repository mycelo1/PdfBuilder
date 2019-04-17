using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using PdfBuilder.Helpers;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // construção do arquivo PDF
    public class PdfFile : IDisposable
    {
        public string PdfVersion { get; set; } = "1.4";
        public int DefaultObjectVersion { get; set; } = 0;
        public PdfCompression Compression { get; }

        public PdfDocument PdfDocument { get; }
        public PdfCatalog PdfCatalog { get; }
        public IEnumerable<PdfObject> ObjectList { get { return object_list; } }

        private readonly List<PdfObject> object_list = new List<PdfObject>();
        private int _object_count = 0;

        private PdfCompression _compression;
        private Func<byte[], byte[]> _compressor;

        public PdfFile()
        {
            (PdfDocument = new PdfDocument()).Initialize(this, 0, 65535);
            Add(PdfCatalog = new PdfCatalog());
            _compression = PdfCompression.None;
        }

        public PdfFile(PdfCompression compression, Func<byte[], byte[]> compressor)
        {
            _compression = compression;
            _compressor = compressor;
        }

        // adicionar nova página
        public PdfPage AddPage(PdfPage pdf_page)
        {
            return (PdfPage)Add(pdf_page);
        }

        internal PdfObject Add(PdfObject pdf_object)
        {
            object_list.Add(pdf_object);
            pdf_object.Initialize(this, ++_object_count, DefaultObjectVersion);
            return pdf_object;
        }

        internal byte[] Compress(byte[] data)
        {
            if (_compression != PdfCompression.None)
            {
                return _compressor(data);
            }
            else
            {
                return data;
            }
        }

        // montar arquivo PDF
        public void Build(Stream output)
        {
            List<byte> pdf_bytes = new List<byte>();

            // header
            byte[] header = Util.StringToBytes($"%PDF-{PdfVersion}\n");
            pdf_bytes.AddRange(header);

            // objetos
            foreach (PdfObject pdf_obj in ObjectList)
            {
                pdf_bytes.AddRange(pdf_obj.Data(pdf_bytes.Count));
            }

            // xref
            int xref_pos = pdf_bytes.Count;
            pdf_bytes.AddRange(Util.StringToBytes($"xref\n"));
            pdf_bytes.AddRange(Util.StringToBytes($"{ObjectList.First().ObjectNumber} {ObjectList.Count() + 1}\n"));
            pdf_bytes.AddRange(Util.StringToBytes($"{PdfDocument.ObjectNumber.ToString("D10")} {PdfDocument.ObjectVersion.ToString("D5")} f \n"));
            foreach (PdfObject pdf_obj in ObjectList)
            {
                pdf_bytes.AddRange(
                    Util.StringToBytes(
                        $"{pdf_obj.ObjectXRef.ToString("D10")} " +
                        $"{pdf_obj.ObjectVersion.ToString("D5")} n \n"));
            }

            // trailer
            pdf_bytes.AddRange(Util.StringToBytes(
                $"trailer << /Root {PdfCatalog.ObjectNumber} {PdfCatalog.ObjectVersion} R " +
                $"/Size {ObjectList.Count() + 1} >>\n"));
            pdf_bytes.AddRange(Util.StringToBytes($"startxref\n"));
            pdf_bytes.AddRange(Util.StringToBytes($"{xref_pos}\n"));
            pdf_bytes.AddRange(Util.StringToBytes($"%%EOF"));

            // gravar no output
            output.Write(pdf_bytes.ToArray(), 0, pdf_bytes.Count);
        }

        public void Dispose() { }
    }
}