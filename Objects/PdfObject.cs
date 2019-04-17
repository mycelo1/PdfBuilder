using System;
using System.Collections.Generic;
using System.Text;

using PdfBuilder.Helpers;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
        // representação dos objetos internos do PDF
    public abstract class PdfObject
    {
        public int ObjectNumber { get { return _number; } }
        public int ObjectVersion { get { return _version; } }
        public int ObjectXRef { get { return _xref; } }
        public PdfFile File { get { return _pdf_file; } }

        private int _xref;
        private int _number;
        private int _version;
        private PdfFile _pdf_file;
        private bool _initialized = false;

        internal virtual void Initialize(PdfFile pdf_file, int number = -1, int version = 0)
        {
            if (_initialized) { throw new InvalidOperationException(); }
            _pdf_file = pdf_file;
            _number = number;
            _version = version;
            _initialized = true;
        }

        static PdfObject()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // montagem dos dados do objeto PDF
        internal virtual byte[] Data(int xref)
        {
            _xref = xref;
            List<byte> data = new List<byte>();
            data.AddRange(Util.StringToBytes($"{ObjectNumber} {ObjectVersion} obj\n"));
            data.AddRange(InternalData() ?? new byte[0]);
            data.AddRange(Util.StringToBytes($"endobj\n"));
            return data.ToArray();
        }

        protected abstract byte[] InternalData();
    }

}