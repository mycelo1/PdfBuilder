using System;
using System.Collections.Generic;

using PdfBuilder.Helpers;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // montagem do conteúdo da página (strings, linhas, etc)
    public class PdfContents : PdfObject
    {
        public PdfPage Page { get; }
        public IEnumerable<PdfContentItem> ContentItems { get { return _content_items; } }

        private List<PdfContentItem> _content_items = new List<PdfContentItem>();

        public PdfContents(PdfPage pdf_page)
        {
            Page = pdf_page;
        }

        internal void AddContentItem(PdfContentItem content_item)
        {
            _content_items.Add(content_item);
        }

        protected override byte[] InternalData()
        {
            List<byte> contents_stream = new List<byte>();
            foreach (PdfElement element in Page.Elements)
            {
                contents_stream.AddRange(element.Contents() ?? new byte[0]);
            }
            foreach (PdfContentItem item in ContentItems)
            {
                contents_stream.AddRange(item.Contents() ?? new byte[0]);
            }
            List<byte> contents = new List<byte>();
            contents.AddRange(Util.StringToBytes($"<< /Length {contents_stream.Count}\x20"));

            switch (File.Compression)
            {
                case PdfCompression.LZW:
                    contents.AddRange(Util.StringToBytes($"/Filter /LZWDecode\x20"));
                    break;
                case PdfCompression.ZLib:
                    contents.AddRange(Util.StringToBytes($"/Filter /FlateDecode\x20"));
                    break;
            }

            contents.AddRange(Util.StringToBytes(
                $">>\n" +
                $"stream\n"
                ));
            contents.AddRange(contents_stream);
            contents.AddRange(Util.StringToBytes($"endstream\n"));
            return File.Compress(contents.ToArray());
        }
    }
}