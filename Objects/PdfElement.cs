using System;

using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // representação dos elementos da página
    public abstract class PdfElement : PdfObject
    {
        public PdfPage Page { get { return _page; } }
        public int ElementNumber { get { return _element_number; } }
        public string ElementName { get { return GetElementName(); } }

        private PdfPage _page;
        private int _element_number;

        internal void SetupElement(PdfPage pdf_page, int element_number)
        {
            _page = pdf_page;
            _element_number = element_number;
        }

        protected abstract string GetElementName();
        internal abstract byte[] Contents();
    }
}