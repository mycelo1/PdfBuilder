using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PdfBuilder.Helpers;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // box com ajuste automático de tamanho
    public class PdfAutoBox : PdfBox
    {
        public PdfXY UpperLeft { get; }
        public PdfGrow GrowDirection { get; }
        public PdfAlign VAlign { get; }
        public PdfArea Area { get; }
        public PdfArea MaxArea { get; }

        private static int grow_lr = 0b1100;
        private static int grow_tb = 0b0011;

        public PdfAutoBox(PdfXY upper_left, PdfGrow grow_direction, PdfAlign v_align, PdfArea area, PdfArea max_area = null, PdfBorder borders = null, PdfBoxLayout box_layout = null)
            : base(borders, box_layout)
        {
            UpperLeft = upper_left;
            GrowDirection = grow_direction;
            VAlign = v_align;
            Area = area;
            MaxArea = max_area ?? new PdfArea(max_box_size, max_box_size);
        }

        public PdfAutoBox(PdfXY upper_left, PdfGrow grow_direction, PdfArea area = null, PdfArea max_area = null, PdfBorder borders = null, PdfBoxLayout box_layout = null)
            : this(upper_left, grow_direction, PdfAlign.Far, area ?? new PdfArea(0, 0), max_area, borders, box_layout) { }

        public PdfAutoBox(PdfRect rectangle, PdfAlign v_align, PdfBorder borders = null, PdfBoxLayout box_layout = null)
            : this(rectangle.UpperLeft, PdfGrow.None, v_align, rectangle.Area, null, borders, box_layout) { }

        public PdfAutoBox(PdfRect rectangle, PdfBoxLayout box_layout = null)
            : this(rectangle.UpperLeft, PdfGrow.None, PdfAlign.Far, rectangle.Area, null, null, box_layout) { }

        protected override double CalcMaxTextWidth()
        {
            if (((int)GrowDirection & grow_lr) > 0)
            {
                return MaxArea.Width - Borders.HBorders;
            }
            else
            {
                return Area.Width - Borders.HBorders;
            }
        }

        protected override PdfRect CalcRectangle(double text_width, double text_height)
        {
            double width =
                ((int)GrowDirection & grow_lr) > 0
                    ? Math.Max(Math.Min(MaxArea.Width, text_width + Borders.HBorders), Area.Width)
                    : Area.Width
                    ;

            double height =
                ((int)GrowDirection & grow_tb) > 0
                    ? Math.Max(Math.Min(MaxArea.Height, text_height + Borders.VBorders), Area.Height)
                    : Area.Height
                    ;

            double x = 0;
            double y = 0;

            if (ParentBox != null)
            {
                switch (TilePosition)
                {
                    case PdfTile.OnTop:
                        x = ParentBox.AbsRectangle.UpperLeft.X;
                        y = ParentBox.AbsRectangle.UpperLeft.Y;
                        break;

                    case PdfTile.OnBottom:
                        x = ParentBox.AbsRectangle.UpperLeft.X;
                        y = ParentBox.AbsRectangle.LowerRight.Y;
                        break;

                    case PdfTile.OnLeft:
                        x = ParentBox.AbsRectangle.UpperLeft.X;
                        y = ParentBox.AbsRectangle.UpperLeft.Y;
                        break;

                    case PdfTile.OnRight:
                        x = ParentBox.AbsRectangle.LowerRight.X;
                        y = ParentBox.AbsRectangle.UpperLeft.Y;
                        break;
                }
            }

            x +=
                (GrowDirection & PdfGrow.Left) > 0
                    ? UpperLeft.X - width
                    : UpperLeft.X
                    ;

            y +=
                (GrowDirection & PdfGrow.Up) > 0
                    ? UpperLeft.Y - height
                    : UpperLeft.Y
                    ;

            return new PdfRect(x, y, width, height);
        }

        protected override PdfBoundingBox CalcBoundingBox(PdfRect abs_rectangle)
        {
            return new PdfBoundingBox(abs_rectangle, Page.Area.Height);
        }

        protected override (double a, double b, double c, double d) CalcRotation() => (1, 0, 0, 1);

        protected override double CalcTextStartX(double text_width) => Borders.Left;

        protected override double CalcTextStartY(double text_height)
        {
            if ((((int)GrowDirection & grow_tb) == 0) || (text_height < (AbsRectangle.Area.Height - Borders.VBorders)))
            {
                return Util.CalcVAlign(VAlign, AbsRectangle.Area.Height - Borders.VBorders, text_height) + Borders.Bottom;
            }
            {
                return Borders.Bottom;
            }
        }

        protected override double CalcTextX(PdfAlign h_align, double line_width)
        {
            return Util.CalcHAlign(h_align, AbsRectangle.Area.Width - Borders.HBorders, line_width);
        }
    }
}
