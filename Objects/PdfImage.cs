using System;
using System.Collections.Generic;

using PdfBuilder.Helpers;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // imagem genérica
    public abstract class PdfImage : PdfXObject
    {
        public int BitsPerColor { get; set; }
        public string ColorSpace { get; set; }
        public string Filter { get; set; }
        public byte[] ImageData { get; }
        public PdfArea ImageArea { get; }

        public PdfImage(byte[] image_data, PdfArea image_size, PdfRect image_position)
        {
            ImageData = image_data;
            ImageArea = image_size;
            Rectange = image_position;
        }

        protected override byte[] InternalData()
        {
            List<byte> data = new List<byte>();
            data.AddRange(Util.StringToBytes(
                $"<< /BitsPerComponent {BitsPerColor} /ColorSpace /{ColorSpace} /Filter /{Filter} /Subtype /Image " +
                $"/Type /XObject /Height {ImageArea.Height} /Width {ImageArea.Width} /Length {ImageData.Length + 1} >>\n" +
                $"stream\n"
                ));
            data.AddRange(ImageData);
            data.AddRange(Util.StringToBytes($"\n"));
            data.AddRange(Util.StringToBytes($"endstream\n"));
            return data.ToArray();
        }

        protected override string GetElementName() => $"/I{ElementNumber}";

        internal override byte[] Contents()
        {
            PdfBoundingBox bounding_box = new PdfBoundingBox(Rectange, Page.Area.Height);
            return Util.StringToBytes(
                $"q\n" +
                $"{bounding_box.Width} 0 0 {bounding_box.Height} {bounding_box.LLx} {bounding_box.LLy} cm\n" +
                $"{ElementName} Do\n" +
                $"Q\n");
        }
    }
}