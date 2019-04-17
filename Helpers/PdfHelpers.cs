using System;
using System.Globalization;
using System.Text;

using PdfBuilder.Types;

namespace PdfBuilder.Helpers
{
    internal static class Util
    {
        public static string FormatDouble(double number, int digits = 3)
        {
            return Math.Round(number, digits).ToString(CultureInfo.InvariantCulture);
        }

        public static int RoundDouble(double number)
        {
            return (int)Math.Round(number);
        }

        internal static byte[] StringToBytes(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        public static double CalcHAlign(PdfAlign align, double range, double content)
        {
            switch (align)
            {
                case PdfAlign.Middle:
                    return RoundDouble((range - content) / 2f);
                case PdfAlign.Far:
                    return range - content;
                default:
                    return 0;
            }
        }

        public static double CalcVAlign(PdfAlign align, double range, double content)
        {
            switch (align)
            {
                case PdfAlign.Middle:
                    return RoundDouble((range - content) / 2f);
                case PdfAlign.Near:
                    return range - content;
                default:
                    return 0;
            }
        }

        public static (double, double, double, double) Rotate(double degrees)
        {
            double radians = Math.PI * degrees / 180f;
            return (Math.Cos(radians), Math.Sin(radians), -Math.Sin(radians), Math.Cos(radians));
        }
    }
}