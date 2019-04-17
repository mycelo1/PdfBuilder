using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PdfBuilder.Fonts;
using PdfBuilder.Helpers;
using PdfBuilder.Types;

namespace PdfBuilder.Objects
{
    // box (retângulo e/ou texto)
    public abstract class PdfBox : PdfContentItem, IPdfAddBox
    {
        protected const int max_box_size = 0xffff;

        public PdfBoxLayout BoxLayout { get; }
        public PdfBorder Borders { get; }
        public PdfRect AbsRectangle { get { return _abs_rectangle; } }
        public PdfBox ParentBox { get { return _parent_box; } }
        public PdfTile TilePosition { get { return _tile_position; } }
        public IEnumerable<PdfTextLine> TextLines { get { return _text_lines; } }

        protected PdfRect _abs_rectangle;
        protected PdfBox _parent_box;
        protected PdfTile _tile_position;

        private PdfTextOut[] _lines_out;
        private double _text_width;
        private double _text_height;

        private List<PdfTextLine> _text_lines = new List<PdfTextLine>();

        public PdfBox(PdfBorder borders, PdfBoxLayout box_layout)
        {
            BoxLayout = box_layout;
            Borders = borders ?? new PdfBorder(0, 0);
        }

        // acrescentar linha de texto ao box
        public PdfBox AddLine(PdfTextLine text_line)
        {
            _text_lines.Add(text_line);
            text_line.Initialize(Page);
            _abs_rectangle = null;
            return this;
        }

        // acrescentar coleção de linhas de texto ao box
        public PdfBox AddLines(IEnumerable<PdfTextLine> text_lines)
        {
            foreach (var text_line in text_lines)
            {
                AddLine(text_line);
            }
            return this;
        }

        public PdfBox AddBox(PdfBox pdf_box)
        {
            return AttachBox(pdf_box, PdfTile.OnBottom);
        }

        public PdfBox AttachBox(PdfBox pdf_box, PdfTile tile_position = PdfTile.OnBottom)
        {
            pdf_box.SetParentBox(this, tile_position);
            return (PdfBox)AddContentItem(pdf_box);
        }

        public PdfBox IncludeIn(PdfOuterBox outer_box)
        {
            outer_box.IncludeBox(this);
            return this;
        }

        public virtual void Refresh()
        {
            if (_abs_rectangle == null)
            {
                // quebrar linhas e calcular posição de cada linha de texto
                if (TextLines.Count() > 0)
                {
                    (_lines_out, _text_width, _text_height) = PdfFontMetrics.TextCoordinates(
                        width: CalcMaxTextWidth(),
                        lines: TextLines.Select(x => new PdfTextIn(
                            metrics: x.Font.FontMetrics,
                            font_name: x.Font.ElementName,
                            codepage: x.Font.CodePage,
                            text_scale: x.TextScale,
                            text: x.TextLine,
                            h_align: x.HAlign,
                            color: x.Color,
                            kerning: x.Font.Kerning)));
                }
                else
                {
                    _lines_out = null;
                    _text_width = 0;
                    _text_height = 0;
                }
                // calcular dimensões do box
                _abs_rectangle = CalcRectangle(_text_width, _text_height);
            }
        }

        internal PdfContentItem AddContentItem(PdfContentItem content_item)
        {
            Page.AddContentItem(content_item);
            return content_item;
        }

        protected void SetParentBox(PdfBox parent_box, PdfTile tile_position)
        {
            _parent_box = parent_box;
            _tile_position = tile_position;
        }

        internal override byte[] Contents()
        {
            Refresh();
            List<byte> contents = new List<byte>();
            PdfBoundingBox bounding_box = CalcBoundingBox(_abs_rectangle);
            var rotation = CalcRotation();

            // aplicar matriz de transformação
            if ((BoxLayout != null) || ((_lines_out?.Length ?? 0) > 0))
            {
                byte[] ctm_bytes = Util.StringToBytes(
                    $"q\n" +
                    $"{Util.FormatDouble(rotation.a)}\x20" +
                    $"{Util.FormatDouble(rotation.b)}\x20" +
                    $"{Util.FormatDouble(rotation.c)}\x20" +
                    $"{Util.FormatDouble(rotation.d)}\x20" +
                    $"{Util.FormatDouble(bounding_box.LLx)}\x20" +
                    $"{Util.FormatDouble(bounding_box.LLy)}\x20" +
                    $"cm\n");
                contents.AddRange(ctm_bytes);
            }

            // desenhar retânguilo
            if (BoxLayout != null)
            {
                contents.AddRange(DrawBox(BoxLayout, bounding_box.Width, bounding_box.Height));
            }

            // escrever linhas de texto
            if ((_lines_out?.Length ?? 0) > 0)
            {
                // início do bloco de texto
                contents.AddRange(Util.StringToBytes($"BT\n"));
                double pos_x = -CalcTextStartX(_text_width);
                double pos_y = -CalcTextStartY(_text_height);

                // plotar linhas de texto de baixo para cima
                foreach (var line_out in _lines_out)
                {
                    byte[] pdf_string = Encoding.GetEncoding(line_out.CodePage).GetBytes(line_out.Text);
                    double line_x = CalcTextX(line_out.HAlign, line_out.Width);

                    if (line_out.Color != null)
                    {
                        contents.AddRange(Util.StringToBytes(
                            $"{Util.FormatDouble(line_out.Color.Red)} {Util.FormatDouble(line_out.Color.Green)} {Util.FormatDouble(line_out.Color.Blue)} rg\n"
                            ));
                    }

                    contents.AddRange(Util.StringToBytes(
                        $"{line_out.FontName} {line_out.TextScale} Tf\n" +
                        $"{Util.FormatDouble(line_out.WordSpacing)} Tw\n" +
                        $"{Util.FormatDouble(line_x - pos_x)} {Util.FormatDouble(line_out.Y - pos_y)} Td\n" +
                        $"("));
                    foreach (byte text_byte in pdf_string)
                    {
                        if ((text_byte == 0x28) || (text_byte == 0x29) || (text_byte == 0x5c))
                        {
                            contents.Add((byte)'\\');
                        }
                        contents.Add(text_byte);
                    }
                    contents.AddRange(Util.StringToBytes($") Tj\n"));
                    pos_x = line_x;
                    pos_y = line_out.Y;
                }

                // finalizar texto
                contents.AddRange(Util.StringToBytes($"ET\n"));
            }

            // restaurar estado gráfico
            if ((BoxLayout != null) || ((_lines_out?.Length ?? 0) > 0))
            {
                contents.AddRange(Util.StringToBytes($"Q\n"));
            }

            return contents.ToArray();
        }

        // desenhar retângulo em torno do elemento
        protected static byte[] DrawBox(PdfBoxLayout box_layout, double width, double height)
        {
            List<byte> contents = new List<byte>();

            contents.AddRange(Util.StringToBytes($"q\n"));

            if ((box_layout.Style & PdfBoxStyle.Stroke) > 0)
            {
                // configurar largura e cor da linha
                contents.AddRange(Util.StringToBytes($"{Util.FormatDouble(box_layout.Thickness)} w\n"));
                contents.AddRange(Util.StringToBytes($"{Util.FormatDouble(box_layout.StrokeColor.Red)} {Util.FormatDouble(box_layout.StrokeColor.Green)} {Util.FormatDouble(box_layout.StrokeColor.Blue)} RG\n"));
            }

            if ((box_layout.Style & PdfBoxStyle.Fill) > 0)
            {
                // configurar cor do preenchimento
                contents.AddRange(Util.StringToBytes($"{Util.FormatDouble(box_layout.FillColor.Red)} {Util.FormatDouble(box_layout.FillColor.Green)} {Util.FormatDouble(box_layout.FillColor.Blue)} rg\n"));
            }

            if (box_layout.LineDash != null)
            {
                // configurar linha tracejada
                contents.AddRange(Util.StringToBytes($"[{Util.FormatDouble(box_layout.LineDash.StrokeLength)} {Util.FormatDouble(box_layout.LineDash.SpaceLength)}] 0 d\n"));
            }

            if (((box_layout.Style & PdfBoxStyle.Fill) > 0) || ((box_layout.Style == PdfBoxStyle.Stroke) && box_layout.DrawTop && box_layout.DrawBottom && box_layout.DrawLeft && box_layout.DrawRight))
            {
                if (box_layout.CornerCurve == null)
                {
                    // plotar contorno sem curvas
                    contents.AddRange(Util.StringToBytes($"{0} {0} {Util.FormatDouble(width)} {Util.FormatDouble(height)} re\n"));
                }
                else
                {
                    // plotar contorno com curvas
                    double curve_width = box_layout.CornerCurve.Width;
                    double curve_height = box_layout.CornerCurve.Heigth;

                    // desenhar curva superior esquerda
                    contents.AddRange(Util.StringToBytes(
                        $"{Util.FormatDouble(0)} {Util.FormatDouble(height - curve_height)} m\n"));

                    contents.AddRange(Util.StringToBytes(
                        $"{Util.FormatDouble(0)} {Util.FormatDouble(height)}\x20" +
                        $"{Util.FormatDouble(0)} {Util.FormatDouble(height)}\x20" +
                        $"{Util.FormatDouble(0 + curve_width)} {Util.FormatDouble(height)} c\n"));

                    // desenhar linha de cima
                    contents.AddRange(Util.StringToBytes(
                        $"{Util.FormatDouble(width - curve_width)} {Util.FormatDouble(height)} l\n"));

                    // desenhar curva superior direita
                    contents.AddRange(Util.StringToBytes(
                        $"{Util.FormatDouble(width)} {Util.FormatDouble(height)}\x20" +
                        $"{Util.FormatDouble(width)} {Util.FormatDouble(height)}\x20" +
                        $"{Util.FormatDouble(width)} {Util.FormatDouble(height - curve_height)} c\n"));

                    // desenhar linha direita
                    contents.AddRange(Util.StringToBytes(
                        $"{Util.FormatDouble(width)} {Util.FormatDouble(0 + curve_height)} l\n"));

                    // desenhar curva inferior direita
                    contents.AddRange(Util.StringToBytes(
                        $"{Util.FormatDouble(width)} {Util.FormatDouble(0)}\x20" +
                        $"{Util.FormatDouble(width)} {Util.FormatDouble(0)}\x20" +
                        $"{Util.FormatDouble(width - curve_width)} {Util.FormatDouble(0)} c\n"));

                    // desenhar linha de baixo
                    contents.AddRange(Util.StringToBytes(
                        $"{Util.FormatDouble(0 + curve_width)} {Util.FormatDouble(0)} l\n"));

                    // desenhar curva inferior esquerda
                    contents.AddRange(Util.StringToBytes(
                        $"{Util.FormatDouble(0)} {Util.FormatDouble(0)}\x20" +
                        $"{Util.FormatDouble(0)} {Util.FormatDouble(0)}\x20" +
                        $"{Util.FormatDouble(0)} {Util.FormatDouble(0 + curve_height)} c\n"));

                    // desenhar linha esquerda
                    contents.AddRange(Util.StringToBytes("h\x20"));
                }

                switch (box_layout.Style)
                {
                    // pintar contorno
                    case PdfBoxStyle.Stroke:
                        contents.AddRange(Util.StringToBytes($"S\n"));
                        break;

                    // pintar preenchimento
                    case PdfBoxStyle.Fill:
                        contents.AddRange(Util.StringToBytes($"f\n"));
                        break;

                    // pintar contorno e preenchimento
                    case PdfBoxStyle.StrokeFill:
                        contents.AddRange(Util.StringToBytes($"B\n"));
                        break;
                }
            }
            else if (box_layout.Style == PdfBoxStyle.Stroke)
            {
                if (box_layout.DrawTop)
                {
                    // desenhar linha de cima
                    contents.AddRange(Util.StringToBytes(
                        $"{0} {Util.FormatDouble(height)} m\n" +
                        $"{Util.FormatDouble(width)} {Util.FormatDouble(height)} l S\n"));
                }

                if (box_layout.DrawBottom)
                {
                    // desenhar linha de baixo
                    contents.AddRange(Util.StringToBytes(
                        $"{0} {0} m\n" +
                        $"{Util.FormatDouble(width)} {0} l S\n"));
                }

                if (box_layout.DrawLeft)
                {
                    // desenhar linha da esquerda
                    contents.AddRange(Util.StringToBytes(
                        $"{0} {0} m\n" +
                        $"{0} {Util.FormatDouble(height)} l S\n"));
                }

                if (box_layout.DrawRight)
                {
                    // desenhar linha da direita
                    contents.AddRange(Util.StringToBytes(
                        $"{Util.FormatDouble(width)} {0} m\n" +
                        $"{Util.FormatDouble(width)} {Util.FormatDouble(height)} l S\n"));
                }
            }

            contents.AddRange(Util.StringToBytes($"Q\n"));
            return contents.ToArray();
        }

        protected abstract double CalcMaxTextWidth();
        protected abstract PdfRect CalcRectangle(double text_width, double text_height);
        protected abstract PdfBoundingBox CalcBoundingBox(PdfRect abs_rectangle);
        protected abstract (double a, double b, double c, double d) CalcRotation();
        protected abstract double CalcTextStartX(double text_width);
        protected abstract double CalcTextStartY(double text_height);
        protected abstract double CalcTextX(PdfAlign h_align, double line_width);
    }

}