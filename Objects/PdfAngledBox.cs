using System;

using PdfBuilder.Helpers;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // box com rotação
    public class PdfAngledBox : PdfBox
    {
        public PdfAlign VAlign { get; }
        public PdfRect Rectangle { get; }
        public PdfAngle Angle { get; }

        public PdfAngledBox(PdfRect rect, PdfAngle angle, PdfAlign v_align, PdfBorder borders = null, PdfBoxLayout box_layout = null)
            : base(borders, box_layout)
        {
            Rectangle = rect;
            Angle = angle;
            VAlign = v_align;
        }

        protected override double CalcMaxTextWidth() => Rectangle.Area.Height - Borders.VBorders;

        protected override PdfRect CalcRectangle(double text_width, double text_height)
        {
            if (ParentBox != null)
            {
                switch (TilePosition)
                {
                    case PdfTile.OnTop:
                        return new PdfRect(
                            x: ParentBox.AbsRectangle.UpperLeft.X + Rectangle.UpperLeft.X,
                            y: ParentBox.AbsRectangle.UpperLeft.Y + Rectangle.UpperLeft.Y - Rectangle.Area.Height,
                            width: Rectangle.Area.Width,
                            height: Rectangle.Area.Height);

                    case PdfTile.OnBottom:
                        return new PdfRect(
                            x: ParentBox.AbsRectangle.UpperLeft.X + Rectangle.UpperLeft.X,
                            y: ParentBox.AbsRectangle.LowerRight.Y + Rectangle.UpperLeft.Y,
                            width: Rectangle.Area.Width,
                            height: Rectangle.Area.Height);

                    case PdfTile.OnLeft:
                        return new PdfRect(
                            x: ParentBox.AbsRectangle.UpperLeft.X + Rectangle.UpperLeft.X - Rectangle.Area.Width,
                            y: ParentBox.AbsRectangle.UpperLeft.Y + Rectangle.UpperLeft.Y,
                            width: Rectangle.Area.Width,
                            height: Rectangle.Area.Height);

                    case PdfTile.OnRight:
                        return new PdfRect(
                            x: ParentBox.AbsRectangle.LowerRight.X + Rectangle.UpperLeft.X,
                            y: ParentBox.AbsRectangle.UpperLeft.Y + Rectangle.UpperLeft.Y,
                            width: Rectangle.Area.Width,
                            height: Rectangle.Area.Height);

                    default:
                        throw new InvalidOperationException();
                }
            }
            else
            {
                return Rectangle;
            }
        }

        protected override PdfBoundingBox CalcBoundingBox(PdfRect abs_rectangle)
        {
            double llx;
            double lly;

            switch (Angle)
            {
                case PdfAngle.BottomUp:
                    llx = abs_rectangle.LowerRight.X;
                    lly = Page.Area.Height - abs_rectangle.LowerRight.Y;
                    break;

                case PdfAngle.TopDown:
                    llx = abs_rectangle.UpperLeft.X;
                    lly = Page.Area.Height - abs_rectangle.UpperLeft.Y;
                    break;

                default:
                    throw new InvalidOperationException();
            }

            return new PdfBoundingBox(llx, lly, llx + Rectangle.Area.Height, lly + Rectangle.Area.Width);
        }

        protected override (double a, double b, double c, double d) CalcRotation()
        {
            return Util.Rotate((double)Angle);
        }

        protected override double CalcTextStartX(double text_width) => Borders.Left;

        protected override double CalcTextStartY(double text_height)
        {
            return Util.CalcVAlign(VAlign, Rectangle.Area.Width - Borders.HBorders, text_height) + Borders.Left;
        }

        protected override double CalcTextX(PdfAlign h_align, double line_width)
        {
            return Util.CalcHAlign(h_align, CalcMaxTextWidth(), line_width);
        }
    }
}