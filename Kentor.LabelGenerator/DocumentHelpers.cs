using Kentor.LabelGenerator.Models;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Kentor.LabelGenerator
{
    public static class DocumentHelpers
    {
        public static PdfDocument CreateDocument(string[][] labelRows, DocumentType documentType, int labelCount = 1, Bitmap[] bitmaps = null, string[] offLabel = null)
        {
            LabelSettings settings = Utilities.GetSettings(documentType);

            // Document settings
            PdfDocument doc = new PdfDocument();
            PdfPage page = AddPage(doc, settings);
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XTextFormatter tf = new XTextFormatter(gfx);
            
            // Variables used to calculate label position
            int currentColumn;
            int currentRow;
            int labelsInCurrentPage = 0;
            double contentPositionLeft;
            double contentPositionTop;

            for (var r=0; r<labelRows.Length; r++)
            {
                var row = labelRows[r];

                Bitmap bitmap = (null != bitmaps && bitmaps.Length > r) ? bitmaps[r] : null;
                var image = GetImage(bitmap);


                for (var dupes = 0; dupes < labelCount; dupes++)
                {
                    if (labelsInCurrentPage == settings.LabelsPerPage)
                    {
                        // Page is full, add new page
                        page = AddPage(doc, settings);
                        gfx = XGraphics.FromPdfPage(page);
                        tf = new XTextFormatter(gfx);
                        labelsInCurrentPage = 0;
                    }

                    currentColumn = CalculateCurrentColumn(labelsInCurrentPage, settings.ColumnsPerPage);
                    currentRow = CalculateCurrentRow(labelsInCurrentPage, settings.ColumnsPerPage, currentColumn);
                    contentPositionLeft = CalculateContentPositionLeft(currentColumn, settings);
                    contentPositionTop = CalculateContentPositionTop(currentRow, settings);

                    XSize contentSize = GetContentSize(settings);
                    XRect labelRectangle = CreateRectangle(contentPositionLeft, contentPositionTop, contentSize);
                    XRect textRectangle;
                    if (null != image)
                    {
                        var imageSize = GetImageSize(image);
                        textRectangle = new XRect(labelRectangle.X + imageSize.Width + 1, labelRectangle.Y, labelRectangle.Width - imageSize.Width - 1, labelRectangle.Height);
                        var imgLoc = (XVector)labelRectangle.TopLeft;// + new XVector(2, 2);
                        gfx.DrawImage(image, imgLoc.X, imgLoc.Y, labelRectangle.Height, labelRectangle.Height);
                    }
                    else
                    {
                        textRectangle = labelRectangle;
                    }

                    //gfx.DrawRectangle(XPens.Black, labelRectangle); // Transparent border
                    //gfx.DrawRectangle(XPens.Orange, textRectangle); // Transparent border

                    XFont font = new XFont(settings.FontFamily, settings.FontSize, XFontStyle.Bold);
                    var labelText = FormatLabelText(gfx, font, textRectangle.Width, row);
                    //tf.DrawString($"({XUnit.FromPoint(textRectangle.Top).Millimeter:f2},{XUnit.FromPoint(textRectangle.Left).Millimeter:f2})", font, XBrushes.Black, textRectangle, XStringFormats.TopLeft);
                    tf.DrawString(labelText, font, XBrushes.Black, textRectangle, XStringFormats.TopLeft);

                    if (null != offLabel)
                    {
                        XFont smallFont = new XFont(settings.FontFamily, settings.FontSize-1, XFontStyle.Regular);
                        XGraphicsState state = gfx.Save();
                        gfx.RotateAtTransform(90, CreatePoint(0, 0));
                        //for (int x = -100; x < 101; x += 10)
                        //{
                        //    for (int y = -100; y < 101; y += 10)
                        //    {
                        //        var rect = CreateRectangle(x, y, new XSize(XUnit.FromMillimeter(5), XUnit.FromMillimeter(5)));
                        //        //gfx.DrawRectangle(XPens.Red, rect);
                        //        gfx.DrawString($"({x},{y})", smallFont, XBrushes.Red, rect, XStringFormats.Center);
                        //    }
                        //}
                        var top = XUnit.FromPoint(textRectangle.Top).Millimeter;
                        var left = -XUnit.FromPoint(textRectangle.Left-28).Millimeter;

                        XRect verticalRectangle = CreateRectangle(top, left, new XSize(XUnit.FromMillimeter(settings.labelBaseHeight), XUnit.FromMillimeter(5)));
                        //var verticalRectangle = CreateRectangle(contentPositionTop, -contentPositionLeft, new XSize(30, 10));
                        //gfx.DrawRectangle(XPens.Purple, verticalRectangle);
                        tf.DrawString(offLabel[r], smallFont, XBrushes.Purple, verticalRectangle, XStringFormats.TopLeft);
                        gfx.Restore(state);
                    }

                    // Increase number of labels printed
                    labelsInCurrentPage++;
                }
            }
            return doc;
        }

        public static PdfPage AddPage(PdfDocument doc, LabelSettings settings)
        {
            PdfPage page = doc.AddPage();
            page.Width = XUnit.FromMillimeter(settings.PageWidth);
            page.Height = XUnit.FromMillimeter(settings.PageHeight);
            return page;
        }

        public static byte[] SaveToArray(PdfDocument document)
        {
            using (var stream = new MemoryStream())
            {
                document.Save(stream, false);
                return stream.ToArray();
            }
        }

        private static XImage GetImage(Bitmap bitmap)
        {
            if (null == bitmap) return null;
            ImageConverter converter = new ImageConverter();
            var bytes = (byte[])converter.ConvertTo(bitmap, typeof(byte[]));
            using (var stream = new MemoryStream(bytes))
            {
                return XBitmapSource.FromStream(stream);
            }
        }

        public static XSize GetContentSize(LabelSettings settings)
        {
            var contentWidth = XUnit.FromMillimeter(settings.labelBaseWidth - settings.LabelPaddingLeft - settings.LabelPaddingRight);
            var contentHeight = XUnit.FromMillimeter(settings.labelBaseHeight - settings.LabelPaddingTop - settings.LabelPaddingBottom);
            var contentSize = new XSize(contentWidth.Point, contentHeight.Point);
            return contentSize;
        }

        public static XSize GetImageSize(XImage image)
        {
            var contentWidth = XUnit.FromPoint(image.PointWidth);
            var contentHeight = XUnit.FromPoint(image.PointHeight);
            var contentSize = new XSize(contentWidth.Point, contentHeight.Point);
            return contentSize;
        }

        public static int CalculateCurrentColumn(int labelsInCurrentPage, int columnsPerPage)
        {
            // Algorithm to calculate which column label should be printed to
            var currentColumn = (labelsInCurrentPage + 1) % columnsPerPage;

            if (currentColumn == 0)
            {
                // Last column in row
                currentColumn = columnsPerPage;
            }
            return currentColumn;
        }

        public static int CalculateCurrentRow(int labelsInCurrentPage, int columnsPerPage, int currentColumn)
        {
            var currentRow = (labelsInCurrentPage + 1) / columnsPerPage;

            if (currentColumn != columnsPerPage)
            {
                // New row
                currentRow = ++currentRow;
            }
            return currentRow;
        }

        public static double CalculateContentPositionLeft(int currentColumn, LabelSettings settings)
        {
            // Set horisontal position of label based on how many labels is allready printed on same row
            var positionX =
                currentColumn == 1 ?
                settings.LabelPositionX - settings.labelBaseWidth + settings.LabelPaddingLeft :
                ((currentColumn - 1) * settings.LabelPositionX) + (settings.LabelPositionX - settings.labelBaseWidth) + settings.LabelMarginLeft + settings.LabelPaddingLeft;
            return positionX;
        }
        public static double CalculateContentPositionTop(double currentRow, LabelSettings settings)
        {
            // Set vertical position of label based on how many labels is allready printed above
            var positionY = ((currentRow - 1) * settings.labelBaseHeight) + settings.LabelMarginTop + settings.LabelPaddingTop;
            //if (currentRow >= 10)
            //    positionY += 4;
            return positionY;
        }

        public static XRect CreateRectangle(double contentLeftPosition, double contentTopPosition, XSize contentSize)
        {
            var pointX = XUnit.FromMillimeter(contentLeftPosition).Point;
            var pointY = XUnit.FromMillimeter(contentTopPosition).Point;
            var rectangle = new XRect(new XPoint(pointX, pointY), contentSize);
            return rectangle;
        }
        public static XPoint CreatePoint(double contentLeftPosition, double contentTopPosition)
        {
            var pointX = XUnit.FromMillimeter(contentLeftPosition).Point;
            var pointY = XUnit.FromMillimeter(contentTopPosition).Point;
            return new XPoint(pointX, pointY);
        }

        public static string FormatLabelText(XGraphics gfx, XFont xfont, double width, string[] rows)
        {
            var result = string.Empty;
            for (int i = 0; i < rows.Count(); i++)
            {
                var line = rows[i];
                while (false == string.IsNullOrWhiteSpace(line))
                {
                    var size = gfx.MeasureString(line, xfont);
                    if (size.Width > width)
                    {

                        for (var j = line.Length - 1; j >= 0; j--)
                        {
                            size = gfx.MeasureString(line.Substring(0, j), xfont);
                            if (size.Width <= width)
                            {
                                result += line.Substring(0, j) + Environment.NewLine;
                                line = line.Substring(j);
                                break;
                            }
                        }
                    }
                    else
                    {
                        result += line + Environment.NewLine;
                        line = string.Empty;
                    }
                }
            }
            return result;
        }

        public static string FormatLabelText(XGraphics gfx, XFont xfont, XSize textSpace, string[] rows, bool hCenter = false, bool vCenter = false)
        {
            var result = string.Empty;
            var spaceSize = gfx.MeasureString(" ", xfont);

            for (int i = 0; i < rows.Count(); i++)
            {
                var line = rows[i];
                if (string.IsNullOrWhiteSpace(line))
                    line = " ";
                while (line != string.Empty)
                {
                    var size = gfx.MeasureString(line, xfont);
                    if (size.Width > textSpace.Width)
                    {

                        for (var j = line.Length - 1; j >= 0; j--)
                        {
                            if (line[j] != ' ')
                                continue;

                            size = gfx.MeasureString(line.Substring(0, j), xfont);
                            if (size.Width <= textSpace.Width)
                            {
                                var extra = textSpace.Width - size.Width;
                                int spaces = hCenter ? (int)(extra / spaceSize.Width) / 2 : 0;

                                result += "".PadLeft(spaces) + line.Substring(0, j) + Environment.NewLine;
                                line = line.Substring(j + 1);
                                break;
                            }
                        }
                    }
                    else
                    {
                        var extra = textSpace.Width - size.Width;
                        int spaces = hCenter ? (int)(extra / spaceSize.Width) / 2 : 0;

                        result += "".PadLeft(spaces) + line + Environment.NewLine;
                        line = string.Empty;
                    }
                }
            }

            if (vCenter)
            {
                var resultHeight = result.Count(c => c == '\n') * spaceSize.Height;
                var padLines = (int)((textSpace.Height - resultHeight) / spaceSize.Height / 2.0);
                if (padLines < 0)
                {
                    if (result[result.Length - 1] != '.')
                        result += "...";
                }
                else
                {
                    result = new string('\n', padLines) + result;
                }
            }

            return result;
        }
    }
}
