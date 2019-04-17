using System;

namespace PdfBuilder.Types
{
    public enum PdfCompression
    {
        None,
        LZW,
        ZLib
    }

    public enum PdfAlign
    {
        Near,
        Middle,
        Far,
        Justify
    }

    public enum PdfGrow
    {
        None = 0b0000,
        Up = 0b0001,
        Down = 0b0010,
        Left = 0b0100,
        Right = 0b1000,
        UpLeft = 0b0101,
        DownLeft = 0b0110,
        UpRight = 0b1001,
        DownRight = 0b1010
    }

    public enum PdfBoxStyle
    {
        Stroke = 0b01,
        Fill = 0b10,
        StrokeFill = 0b11
    }

    public enum PdfAngle
    {
        BottomUp = 90,
        TopDown = -90
    }

    public enum PdfTile
    {
        OnTop,
        OnBottom,
        OnLeft,
        OnRight
    }

    public class PdfXY
    {
        public double X { get; }
        public double Y { get; }
        public PdfXY(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class PdfArea
    {
        public double Width { get; }
        public double Height { get; }
        public PdfArea(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }

    public class PdfBorder
    {
        public int Top { get; }
        public int Bottom { get; }
        public int Left { get; }
        public int Right { get; }
        public int HBorders { get; }
        public int VBorders { get; }
        public PdfBorder(int top, int bottom, int left, int right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
            HBorders = left + right;
            VBorders = top + bottom;
        }
        public PdfBorder(int h_border, int v_border)
            : this(v_border, v_border, h_border, h_border) { }
    }

    public class PdfRect
    {
        public PdfXY UpperLeft { get; }
        public PdfXY LowerRight { get { return new PdfXY(UpperLeft.X + Area.Width, UpperLeft.Y + Area.Height); } }
        public PdfArea Area { get; }
        public PdfRect(PdfXY upper_left, PdfArea area)
        {
            UpperLeft = upper_left;
            Area = area;
        }
        public PdfRect(double x, double y, double width, double height)
        {
            UpperLeft = new PdfXY(x, y);
            Area = new PdfArea(width, height);
        }
        public static PdfRect Coords(double ul_x, double ul_y, double br_x, double br_y)
        {
            return new PdfRect(ul_x, ul_y, br_x - ul_x, br_y - ul_y);
        }
    }

    public class PdfBoundingBox
    {
        public double LLx { get; }
        public double LLy { get; }
        public double URx { get; }
        public double URy { get; }
        public double Width { get { return URx - LLx; } }
        public double Height { get { return URy - LLy; } }
        public PdfBoundingBox(double llx, double lly, double urx, double ury)
        {
            LLx = llx;
            LLy = lly;
            URx = urx;
            URy = ury;
        }
        public PdfBoundingBox(PdfRect rect, double page_height)
        {
            LLx = rect.UpperLeft.X;
            LLy = page_height - (rect.UpperLeft.Y + rect.Area.Height);
            URx = rect.UpperLeft.X + rect.Area.Width;
            URy = page_height - rect.UpperLeft.Y;
        }
        public PdfBoundingBox(PdfRect rect, double page_height, PdfXY upper_tile)
        {
            LLx = rect.UpperLeft.X + upper_tile.X;
            LLy = page_height - (rect.UpperLeft.Y + upper_tile.Y + rect.Area.Height);
            URx = rect.UpperLeft.X + upper_tile.X + rect.Area.Width;
            URy = page_height - (rect.UpperLeft.Y + upper_tile.Y);
        }
    }

    public class PdfColor
    {
        public double Red { get; }
        public double Green { get; }
        public double Blue { get; }
        public PdfColor(double red, double green, double blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }
        public PdfColor(double gray_shade)
        {
            Red = gray_shade;
            Green = gray_shade;
            Blue = gray_shade;
        }
    }

    public class PdfLineDash
    {
        public double StrokeLength { get; }
        public double SpaceLength { get; }
        public PdfLineDash(double stroke_length, double space_length)
        {
            StrokeLength = stroke_length;
            SpaceLength = space_length;
        }
        public PdfLineDash(double dash_length)
        {
            StrokeLength = dash_length;
            SpaceLength = dash_length;
        }
    }

    public class PdfCornerCurve
    {
        public double Width { get; }
        public double Heigth { get; }
        public PdfCornerCurve(double width, double height)
        {
            Width = width;
            Heigth = height;
        }
        public PdfCornerCurve(double length)
        {
            Width = length;
            Heigth = length;
        }
    }

    public class PdfBoxLayout
    {
        public PdfColor StrokeColor { get; }
        public PdfColor FillColor { get; }
        public double Thickness { get; }
        public bool DrawTop { get; }
        public bool DrawBottom { get; }
        public bool DrawLeft { get; }
        public bool DrawRight { get; }
        public PdfBoxStyle Style { get; }
        public PdfLineDash LineDash { get; }
        public PdfCornerCurve CornerCurve { get; }

        public PdfBoxLayout(double thickness = 0, PdfColor stroke_color = null, PdfColor fill_color = null, bool? draw_top = null, bool? draw_bottom = null, bool? draw_left = null, bool? draw_right = null, PdfLineDash line_dash = null, PdfCornerCurve corner_curve = null)
        {
            Style = fill_color == null ? PdfBoxStyle.Stroke : (stroke_color == null ? PdfBoxStyle.Fill : PdfBoxStyle.StrokeFill);
            StrokeColor = stroke_color;
            FillColor = fill_color;
            Thickness = thickness;
            DrawTop = draw_top ?? (stroke_color != null);
            DrawBottom = draw_bottom ?? (stroke_color != null);
            DrawLeft = draw_left ?? (stroke_color != null);
            DrawRight = draw_right ?? (stroke_color != null);
            LineDash = line_dash;
            CornerCurve = corner_curve;
        }
    }
}