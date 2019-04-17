using System;
using System.Collections.Generic;
using System.Text;

using PdfBuilder.Helpers;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
        // objeto com os recursos usados na página
    public class PdfPage : PdfObject, IPdfAddBox
    {
        public PdfArea Area { get; }
        public PdfContents Contents { get { return _contents; } }
        public PdfPageIndex Parent { get { return _parent; } }

        internal IEnumerable<PdfElement> Elements { get { return _elements; } }

        private PdfContents _contents;
        private PdfPageIndex _parent;
        private readonly List<PdfElement> _elements = new List<PdfElement>();
        private int _element_count = 0;

        public PdfPage(PdfArea area)
        {
            Area = area;
        }

        // acrescentar font à página
        public PdfFont AddFont(PdfFont font)
        {
            return (PdfFont)AddElement(font);
        }

        // acrescentar imagem á página
        public PdfImage AddImage(PdfImage pdf_image)
        {
            return (PdfImage)AddElement(pdf_image);
        }

        // acrescentar box á página
        public PdfBox AddBox(PdfBox pdf_box)
        {
            return (PdfBox)AddContentItem(pdf_box);
        }

        internal PdfElement AddElement(PdfElement element)
        {
            _elements.Add(element);
            element.SetupElement(this, ++_element_count);
            File.Add(element);
            return element;
        }

        internal PdfContentItem AddContentItem(PdfContentItem content_item)
        {
            content_item.Initialize(this);
            Contents.AddContentItem(content_item);
            return content_item;
        }

        internal override void Initialize(PdfFile pdf_file, int number = -1, int version = 0)
        {
            base.Initialize(pdf_file, number, version);
            pdf_file.Add(_contents = new PdfContents(this));
            pdf_file.PdfCatalog.Index.AddPage(this);
        }

        internal void SetParent(PdfPageIndex parent)
        {
            _parent = parent;
        }

        protected override byte[] InternalData()
        {
            StringBuilder resources = new StringBuilder();
            if (_elements.Count > 0)
            {
                StringBuilder rec_x_objects = new StringBuilder();
                StringBuilder rec_fonts = new StringBuilder();
                foreach (PdfElement element in _elements)
                {
                    if (element is PdfXObject)
                    {
                        PdfXObject x_object = element as PdfXObject;
                        rec_x_objects.Append($"{x_object.ElementName} {x_object.ObjectNumber} {x_object.ObjectVersion} R\x20");
                    }
                    else if (element is PdfFont)
                    {
                        PdfFont font = element as PdfFont;
                        rec_fonts.Append($"{font.ElementName} {font.ObjectNumber} {font.ObjectVersion} R\x20");
                    }
                }
                if ((rec_x_objects.Length > 0) || (rec_fonts.Length > 0))
                {
                    resources.Append("/Resources << ");
                    if (rec_x_objects.Length > 0)
                    {
                        resources.Append($"/XObject << {rec_x_objects.ToString()}>> ");
                    }
                    if (rec_fonts.Length > 0)
                    {
                        resources.Append($"/Font << {rec_fonts.ToString()}>> ");
                    }
                    resources.Append(">> ");
                }
            }
            return Util.StringToBytes(
                $"<< /Contents {Contents.ObjectNumber} 0 R /Parent {Parent.ObjectNumber} {Parent.ObjectVersion} R " +
                $"/MediaBox [ 0 0 {Area.Width} {Area.Height} ] " +
                resources.ToString() +
                $"/Type /Page >>\n");
        }
    }
}