using System;

using PdfBuilder.Helpers;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // catálogo - indicação do objeto com o índice das páginas
    public class PdfCatalog : PdfObject
    {
        public PdfPageIndex Index { get { return _index; } }
        private PdfPageIndex _index;

        internal override void Initialize(PdfFile pdf_file, int number = -1, int version = 0)
        {
            base.Initialize(pdf_file, number, version);
            pdf_file.Add(_index = new PdfPageIndex());
        }

        protected override byte[] InternalData()
        {
            return Util.StringToBytes($"<< /Pages {Index.ObjectNumber} {Index.ObjectVersion} R /Type /Catalog >>\n");
        }
    }
}