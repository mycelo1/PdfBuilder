using System;

using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // imagem JPEG
    public class PdfJpegImage : PdfImage
    {
        public PdfJpegImage(byte[] jpeg_data, PdfArea jpeg_area, PdfRect image_position, int bits_per_color = 8)
            : base(jpeg_data, jpeg_area, image_position)
        {
            BitsPerColor = bits_per_color;
        }

        internal override void Initialize(PdfFile pdf_file, int number = -1, int version = 0)
        {
            base.Initialize(pdf_file, number, version);
            ColorSpace = "DeviceRGB";
            Filter = "DCTDecode";
        }
    }
}