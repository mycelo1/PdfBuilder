using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PdfBuilder.Helpers;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // box em torno de outros boxes
    public class PdfOuterBox : PdfBox
    {
        public IEnumerable<PdfBox> InnerBoxes { get { return _inner_boxes; } }

        private List<PdfBox> _inner_boxes = new List<PdfBox>();
        private PdfXY UpperLeft;
        private PdfXY LowerRight;

        public PdfOuterBox(PdfBorder borders = null, PdfBoxLayout box_layout = null)
            : base(borders, box_layout) { }

        public PdfOuterBox IncludeBox(PdfBox box)
        {
            _inner_boxes.Add(box);
            return this;
        }

        public override void Refresh()
        {
            double ulx = max_box_size;
            double uly = max_box_size;
            double lrx = 0;
            double lry = 0;

            foreach (PdfBox box in InnerBoxes)
            {
                box.Refresh();
                if (box.AbsRectangle.UpperLeft.X < ulx) ulx = box.AbsRectangle.UpperLeft.X;
                if (box.AbsRectangle.UpperLeft.Y < uly) uly = box.AbsRectangle.UpperLeft.Y;
                if (box.AbsRectangle.LowerRight.X > lrx) lrx = box.AbsRectangle.LowerRight.X;
                if (box.AbsRectangle.LowerRight.Y > lry) lry = box.AbsRectangle.LowerRight.Y;
            }

            UpperLeft = new PdfXY(ulx - Borders.Left, uly - Borders.Top);
            LowerRight = new PdfXY(lrx + Borders.Right, lry + Borders.Bottom);
            base.Refresh();
        }

        protected override double CalcMaxTextWidth()
        {
            return (LowerRight.X - UpperLeft.X) - Borders.HBorders;
        }

        protected override PdfRect CalcRectangle(double text_width, double text_height)
        {
            return new PdfRect(UpperLeft.X, UpperLeft.Y, LowerRight.X - UpperLeft.X, LowerRight.Y - UpperLeft.Y);
        }

        protected override PdfBoundingBox CalcBoundingBox(PdfRect abs_rectangle)
        {
            return new PdfBoundingBox(abs_rectangle, Page.Area.Height);
        }

        protected override (double a, double b, double c, double d) CalcRotation() => (1, 0, 0, 1);

        protected override double CalcTextStartX(double text_width) => Borders.Left;
        protected override double CalcTextStartY(double text_height) => Borders.Bottom;

        protected override double CalcTextX(PdfAlign h_align, double line_width)
        {
            return Util.CalcHAlign(h_align, AbsRectangle.Area.Width - Borders.HBorders, line_width);
        }
    }
}
