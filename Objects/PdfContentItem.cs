using System;

using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // elementos da página que não são objetos pdf (texto, desenho, etc)
    public abstract class PdfContentItem
    {
        protected PdfPage Page { get { return _page; } }
        private PdfPage _page;
        internal virtual void Initialize(PdfPage page) { _page = page; }
        internal abstract byte[] Contents();
    }
}