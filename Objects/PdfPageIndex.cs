using System;
using System.Collections.Generic;
using System.Text;

using PdfBuilder.Helpers;

namespace PdfBuilder.Objects
{
        // índice das páginas do documento
    public class PdfPageIndex : PdfObject
    {
        private readonly List<PdfPage> _pages = new List<PdfPage>();

        internal void AddPage(PdfPage pdf_page)
        {
            _pages.Add(pdf_page);
            pdf_page.SetParent(this);
        }

        protected override byte[] InternalData()
        {
            StringBuilder kids = new StringBuilder();
            foreach (PdfPage page in _pages)
            {
                kids.Append($"{page.ObjectNumber} {page.ObjectVersion} R ");
            }
            return Util.StringToBytes($"<< /Count {_pages.Count} /Kids [ {kids.ToString()}] /Type /Pages >>\n");
        }
    }
}