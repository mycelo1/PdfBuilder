using System;

namespace PdfBuilder.Objects
{
    public interface IPdfAddBox
    {
        PdfBox AddBox(PdfBox pdf_box);
    }
}