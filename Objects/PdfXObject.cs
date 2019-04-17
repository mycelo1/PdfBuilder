using System;

using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // objeto XObject (elemento gráfico)
    public abstract class PdfXObject : PdfElement
    {
        public PdfRect Rectange { get; set; }
    }
}