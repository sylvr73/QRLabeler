using CommandLine;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using Kentor.LabelGenerator;
using Kentor.LabelGenerator.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;
using QRLabeler.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ZXing;

namespace QRLabeler
{
    class Program
    {
        static void Main(string[] args)
        {
            var enterToEnd = true;

            try
            {

                if (!GhostscriptVersionInfo.IsGhostscriptInstalled)
                {
                    Console.WriteLine("Ghostscript not installed on this machine. This application requires Ghostscript library in order to run!");
                    return;
                }

                Parser.Default.ParseArguments<CommandLineOptions>(args)
                       .WithParsed(o =>
                       {
                           enterToEnd = o.EnterToEnd;

                           if (o.Help && File.Exists("help.txt"))
                           {
                               Console.WriteLine("Version Wortapalooza 1.1, Sept 17, 2022");
                               using (var reader = new StreamReader("help.txt"))
                               {
                                   var txt = reader.ReadToEnd();
                                   Console.WriteLine(txt);
                               }
                               return;
                           }
                           if (o.JudgingLabels)
                           {
                               Console.WriteLine("Creating judging labels");
                               CreateQRLabels(o);
                           }
                           else if (o.BottleLabels)
                           {
                               Console.WriteLine("Creating bottle labels");
                               CreateBottleLabels(o);
                           }
                           else if (o.Combine)
                           {
                               Console.WriteLine("Combining files");
                               CombineFiles2(o);
                           }
                           else if (o.Darken)
                           {
                               Darken(o.OutputFile);
                           }
                           else if (false == string.IsNullOrWhiteSpace(o.ScoreSheetDirectory))
                           {
                               Console.WriteLine("Processing QR Codes");
                               ScanAndRename(o);
                           }
                           else 
                           {
                               throw new ArgumentException("Please specify whether you want to create judging labels, bottle labels or scan a scoresheets");
                           }
                       });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }
            finally
            {
                if (enterToEnd)
                {
                    Console.WriteLine("enter to end");
                    Console.ReadLine();
                }
            }
        }

        private static void CreateQRLabels(CommandLineOptions options)
        {
            try
            {
                var entries = FileHelpers.ReadExportFile(options.DataExportCSV);
                var labelText = new List<string[]>();
                var bitmaps = new List<Bitmap>();

                entries.Sort((a, b) =>
                {
                    var r = a.TableNumber.CompareTo(b.TableNumber);
                    if (0 != r)
                        return r;
                    if (int.TryParse(a.Category, out int ca) && int.TryParse(b.Category, out int cb))
                    {
                        r = ca.CompareTo(cb);
                        if (0 != r)
                            return r;
                    }
                    else
                    {
                        r = a.Category.CompareTo(b.Category);
                        if (0 != r)
                            return r;
                    }
                    return a.Subcategory.CompareTo(b.Subcategory);
                });

                foreach (var entry in entries)
                {
                    Console.WriteLine($"{entry.TableNumber}-{entry.Category}-{entry.Subcategory}");

                    if (string.IsNullOrWhiteSpace(entry.EntryNumber))
                    {
                        Console.WriteLine("Empty entry number!");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(entry.TableName))
                        Console.WriteLine($"Entry {entry.EntryNumber} TableName is blank.");
                    if (string.IsNullOrWhiteSpace(entry.Style))
                        Console.WriteLine($"Entry {entry.EntryNumber} Style is blank.");
                    if (string.IsNullOrWhiteSpace(entry.Category))
                        Console.WriteLine($"Entry {entry.EntryNumber} Category is blank.");
                    if (string.IsNullOrWhiteSpace(entry.Subcategory))
                        Console.WriteLine($"Entry {entry.EntryNumber} Subcategory is blank.");
                    
                    entry.QRCode = QRHelpers.GetQRCode2(entry.JudgingNumber); 

                    if (false == int.TryParse(entry.JudgingNumber, out int judgingNumber))
                    {
                        Console.WriteLine($"Could not parse judging number '{entry.JudgingNumber}' for entry {entry.EntryNumber}");
                        judgingNumber = 0;
                    }

                    var rows = new List<string>()
                    {
                        $"{entry.TableName}",
                        $"Entry: {judgingNumber:000000}",
                        $"{entry.Style} ({entry.Category}{entry.Subcategory})",
                    };
                    if (false == string.IsNullOrWhiteSpace(entry.RequiredInfo)) rows.Add(entry.RequiredInfo);
                    if (false == string.IsNullOrWhiteSpace(entry.Strength)) rows.Add($"Str: {entry.Strength}");
                    if (false == string.IsNullOrWhiteSpace(entry.Sweetness)) rows.Add($"Sweet: {entry.Sweetness}");
                    if (false == string.IsNullOrWhiteSpace(entry.Carbonation)) rows.Add($"Carb: {entry.Carbonation}");

                    labelText.Add(rows.ToArray());
                    bitmaps.Add(entry.QRCode);
                }

                var pdf = DocumentHelpers.CreateDocument(
                    labelText.ToArray(), 
                    DocumentType.LabelSettings_Avery_5960, 
                    options.LabelsPerEntry, 
                    bitmaps.ToArray());

                var outputDir = false == string.IsNullOrWhiteSpace(options.OutputDirectory) ?
                    options.OutputDirectory : ".";
                
                pdf.Save($"{outputDir}\\{options.OutputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }
        }

        private static void CreateBottleLabels(CommandLineOptions options)
        {
            try
            {
                var entries = FileHelpers.ReadExportFile(options.DataExportCSV);
                var labelText = new List<string[]>();
                var entryNumbers = new List<string>();
                foreach (var entry in entries)
                {
                    if (entry.TableNumber == 0)
                        Console.WriteLine($"Skipping entry {entry.EntryNumber}, it doesn't have a table");
                }
                entries.RemoveAll(e => e.TableNumber == 0);

                entries.Sort((a, b) => a.EntryNumber.CompareTo(b.EntryNumber));
                //entries.Sort((a,b) =>
                //{
                //    var r = a.TableNumber.CompareTo(b.TableNumber);
                //    if (0 != r) return r;
                //    return a.JudgingNumberInt.CompareTo(b.JudgingNumberInt);
                //});


                foreach (var entry in entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.EntryNumber))
                    {
                        Console.WriteLine("empty entry number!");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(entry.Category))
                        Console.WriteLine($"Entry {entry.EntryNumber} Category is blank.");
                    if (string.IsNullOrWhiteSpace(entry.Subcategory))
                        Console.WriteLine($"Entry {entry.EntryNumber} Subcategory is blank.");
                    if (string.IsNullOrWhiteSpace(entry.JudgingNumber))
                        Console.WriteLine($"Entry {entry.EntryNumber} JudgingNumber is blank.");
                    if (string.IsNullOrWhiteSpace(entry.TableName))
                        Console.WriteLine($"Entry {entry.EntryNumber} TableName is blank.");

                    if (false == int.TryParse(entry.JudgingNumber, out int judgingNumber))
                    {
                        Console.WriteLine($"Could not parse judging number '{entry.JudgingNumber}' for entry {entry.EntryNumber}");
                        judgingNumber = 0;
                    }

                    var rows = new string[] { $"({entry.Category}{entry.Subcategory}){judgingNumber:0000000}[{entry.TableNumber}]" };
                    entryNumbers.Add(entry.EntryNumber);
                    labelText.Add(rows);
                }
                var pdf = DocumentHelpers.CreateDocument(
                    labelText.ToArray(), 
                    DocumentType.LabelSettings_Avery_8167, 
                    options.LabelsPerEntry, 
                    offLabel: entryNumbers.ToArray());

                var outputDir = false == string.IsNullOrWhiteSpace(options.OutputDirectory) ?
                    options.OutputDirectory : ".";

                pdf.Save($"{outputDir}\\{options.OutputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }
        }

        private static void ScanAndRename(CommandLineOptions options)
        {
            try
            {
                if (false == Directory.Exists(options.ScoreSheetDirectory))
                {
                    Console.WriteLine($"Directory '{options.ScoreSheetDirectory}' not found.");
                    return;
                }

                //var entries = FileHelpers.ReadExportFile(options.DataExportCSV);

                var pdfFiles = Directory.GetFiles(options.ScoreSheetDirectory, "*.pdf");
                foreach (var fname in pdfFiles)
                {
                    Console.WriteLine($"Processing file {fname}");
                    var map = BuildPageMap(fname, options);
                    var doc = PdfReader.Open(fname, PdfDocumentOpenMode.Import);
                    
                    BreakupAndSave(map, doc, options);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }
        }

        private static Dictionary<int, string> BuildPageMap(string fname, CommandLineOptions options)
        {
            int desired_dpi = 300;
            var result = new Dictionary<int, string>();

            //using (var rasterizer = new GhostscriptRasterizer())
            //using (var pdf = UglyToad.PdfPig.PdfDocument.Open(fname))
            //{
            //    foreach (var page in pdf.GetPages())
            //    {
            //        var images = page.GetImages();
            //        foreach (var image in images)
            //        {
            //            using (var bitmap = new Bitmap(new MemoryStream(image.RawBytes.ToArray())))
            //            {
            //                bitmap.Save($"{fname}-{page.Number}.bmp", ImageFormat.Bmp);
            //                var darkerBitmap = QRHelpers.ToBlackAndWhite(bitmap);
            //                darkerBitmap.Save($"{fname}-dark-{page.Number}.bmp", ImageFormat.Bmp);

            //                var reader = new BarcodeReader();
            //                var result3 = reader.Decode(darkerBitmap);
            //                var number = QRHelpers.ReadQRCode(darkerBitmap);
            //                if (result3 != null)
            //                {
            //                    Console.WriteLine($"QR Code Text: {result3.Text}");
            //                    number = result3.Text;
            //                }

            //                if (false == string.IsNullOrEmpty(number))
            //                {
            //                    result.Add(page.Number, number)            //                }
            //            }
            //        }
            //    }
            //}

            using (var rasterizer = new GhostscriptRasterizer())
            {
                rasterizer.Open(fname);
                for (var pageNumber = 1; pageNumber <= rasterizer.PageCount; pageNumber++)
                {
                    var bitmap = (Bitmap)rasterizer.GetPage(desired_dpi, pageNumber);
                    var number = QRHelpers.ReadQRCode(bitmap);
                    var lame = false;

                    // need to clean this up 
                    
                    if (string.IsNullOrEmpty(number))
                    {
                        var bitmapCleaner = QRHelpers.ReduceNoiseAndSmooth(bitmap);
                        number = QRHelpers.ReadQRCode(bitmapCleaner);

                        if (string.IsNullOrEmpty(number))
                        {
                            number = QRHelpers.ReadBarcode(bitmapCleaner);
                            if (false == string.IsNullOrEmpty(number))
                            {
                                Console.WriteLine($"5) Found number on page {pageNumber}: {number}");
                                lame = true;
                            }
                        }
                        else
                        {
                            lame = true;

                            Console.WriteLine($"4) Found number on page {pageNumber}: {number}");
                        }
                    }


                    if (string.IsNullOrWhiteSpace(number))
                    {
                        var darkerBitmap = QRHelpers.ToBlackAndWhiteOptimized(bitmap);
                        number = QRHelpers.ReadQRCode(darkerBitmap);

                        //bitmap.Save($"{fname}-{pageNumber}.bmp", ImageFormat.Bmp);
                        //darkerBitmap.Save($"{fname}-dark-{pageNumber}.bmp", ImageFormat.Bmp);

                        if (string.IsNullOrWhiteSpace(number))
                        {
                            number = QRHelpers.ReadBarcode(darkerBitmap);

                            if (String.IsNullOrWhiteSpace(number))
                            {
                                var justFileName = Path.GetFileName(fname);
                                Console.WriteLine($"Could not identify number in file {fname} page {pageNumber}.");
                                number = $"unknown-{justFileName}-page-{pageNumber}";
                                //bitmap.Save($"{fname}-{pageNumber}.bmp", ImageFormat.Bmp);
                                //darkerBitmap.Save($"{fname}-dark-{pageNumber}.bmp", ImageFormat.Bmp);
                            }
                            else
                            {
                                Console.WriteLine($"3) Found number on page {pageNumber}: {number}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"2) Found number on page {pageNumber}: {number}");
                        }
                    }
                    else
                    {
                        if (!lame)
                        Console.WriteLine($"1) Found number on page {pageNumber}: {number}");
                    }
                        result.Add(pageNumber, number);
                }
            }

            return result;
        }

        private static void Darken(string fname)
        {
            int desired_dpi = 300;
            var newDoc = new PdfDocument();
            using (var rasterizer = new GhostscriptRasterizer())
            {
                rasterizer.Open(fname);
                for (var pageNumber = 1; pageNumber <= rasterizer.PageCount; pageNumber++)
                {
                    var bitmap = (Bitmap)rasterizer.GetPage(desired_dpi, pageNumber);
                    var newBitmap = QRHelpers.ToBlackAndWhiteOptimized(bitmap);
                    var page = newDoc.AddPage();
                    var gfx = XGraphics.FromPdfPage(page);
                    gfx.DrawImage(newBitmap, 0, 0, page.Width, page.Height);
                }
            }
            newDoc.Close();
            newDoc.Save($"dark-{fname}");
        }

        private static void BreakupAndSave(Dictionary<int, string> map, PdfDocument doc, CommandLineOptions options)
        {
            try
            {
                var children = new Dictionary<string, PdfDocument>();
                for (var page = 1; page <= doc.Pages.Count; page++)
                {
                    var number = map[page];
                    if (false == children.TryGetValue(number, out PdfDocument childDoc))
                    {
                        childDoc = new PdfDocument();
                        children[number] = childDoc;
                    }
                    childDoc.AddPage(doc.Pages[page - 1]);
                }

                foreach (var judgingNumber in children.Keys)
                {
                    var childDoc = children[judgingNumber];
                    //string numberToUse = entryNumber;

                    //if (int.TryParse(entryNumber, out _) && false == string.IsNullOrEmpty(options.NumberType) && options.NumberType[0] == 'j')
                    //{
                    //    var data = FileHelpers.ReadExportFile(options.DataExportCSV);
                    //    var entry = data.Find(e => e.EntryNumber == entryNumber);
                    //    if (null != entry)
                    //    {
                    //        if (false == string.IsNullOrWhiteSpace(entry.JudgingNumber))
                    //            numberToUse = entry.JudgingNumber;
                    //        else
                    //            throw new Exception($"Entry {entry.EntryNumber} does not have a judging number.");
                    //    }
                    //    else
                    //    {
                    //        throw new Exception($"Could not find entry number {entryNumber} in data export file.");
                    //    }
                    //}

                    string name;
                    if (false == int.TryParse(judgingNumber, out int n))
                        name = $"{judgingNumber}.pdf";
                    else
                        name = $"{n:000000}.pdf";

                    var destinationFile = Path.Combine(options.OutputDirectory ?? ".\\", name);
                    if (File.Exists(destinationFile))
                    {
                        destinationFile = Path.Combine(options.OutputDirectory ?? ".\\", DateTime.Now.Ticks + name);
                    }
                    childDoc.Save(destinationFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }
        }

        private static void CombineFiles(CommandLineOptions options)
        {
            try
            {
                var mergedDir = Path.Combine(options.OutputDirectory, "merged");
                if (false == Directory.Exists(mergedDir))
                    Directory.CreateDirectory(mergedDir);

                var entries = FileHelpers.ReadExportFile(options.DataExportCSV);

                foreach (var entry in entries)
                {
                    int judgingNo = int.Parse(entry.JudgingNumber);
                    int entryNo = int.Parse(entry.EntryNumber);

                    var jFileName = Path.Combine(options.OutputDirectory, $"{judgingNo:000000}.pdf");
                    var eFileName = Path.Combine(options.OutputDirectory, $"{entryNo:000000}.pdf");
                    var newFile = Path.Combine(options.OutputDirectory, $"{entryNo} - {judgingNo}.pdf");
                    if (File.Exists(jFileName) && File.Exists(eFileName))
                    {
                        Console.WriteLine($"combining {entryNo} and {judgingNo}");
                        var edoc = PdfReader.Open(eFileName, PdfDocumentOpenMode.Import);
                        var jdoc = PdfReader.Open(jFileName, PdfDocumentOpenMode.Import);
                        var newDoc = new PdfDocument();
                        foreach (var page in edoc.Pages)
                            newDoc.AddPage(page);
                        foreach (var page in jdoc.Pages)
                            newDoc.AddPage(page);
                        newDoc.Save(Path.Combine(mergedDir, $"{entryNo:000000}.pdf"));
                    }
                    else
                    {
                        //if (File.Exists(jFileName))
                        //    File.Copy(jFileName, Path.Combine(mergedDir, $"{judgingNo:000000}.pdf"));
                        if (File.Exists(eFileName))
                            File.Copy(eFileName, Path.Combine(mergedDir, $"{judgingNo:000000}.pdf"));

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }

        }
        private static void CombineFiles2(CommandLineOptions options)
        {
            try
            {
                var mergedDir = Path.Combine(options.OutputDirectory, "merged");
                if (false == Directory.Exists(mergedDir))
                    Directory.CreateDirectory(mergedDir);

                var entries = FileHelpers.ReadExportFile(options.DataExportCSV);

                foreach (var entry in entries)
                {
                    int judgingNo = int.Parse(entry.JudgingNumber);
                    int entryNo = int.Parse(entry.EntryNumber);

                    var eFileName = Path.Combine(options.OutputDirectory, $"{entryNo:000000}.pdf");
                    var entryFiles = Directory.GetFiles(options.OutputDirectory, $"{entryNo:000000}*.pdf");
                    var judgeFiles = Directory.GetFiles(options.OutputDirectory, $"{judgingNo:000000}*.pdf");

                    var newFile = Path.Combine(options.OutputDirectory, $"{entryNo} - {judgingNo}.pdf");
                    if(judgeFiles.Length > 0 || entryFiles.Length > 1)
                    {
                        var newDoc = new PdfDocument();
                        if (File.Exists(eFileName))
                        {
                            var edoc = PdfReader.Open(eFileName, PdfDocumentOpenMode.Import);
                            foreach (var page in edoc.Pages)
                                newDoc.AddPage(page);
                        }
                        foreach (var judgeFileName in judgeFiles)
                        {
                            Console.WriteLine($"combining {judgeFileName} into {eFileName} (j)");
                            var jdoc = PdfReader.Open(judgeFileName, PdfDocumentOpenMode.Import);
                            foreach (var page in jdoc.Pages)
                                newDoc.AddPage(page);
                        }
                        foreach (var eFileName2 in entryFiles)
                        {
                            if (eFileName2 == eFileName)
                                continue;

                            Console.WriteLine($"combining {eFileName2} into {eFileName} (e)");
                            var jdoc = PdfReader.Open(eFileName2, PdfDocumentOpenMode.Import);
                            foreach (var page in jdoc.Pages)
                                newDoc.AddPage(page);
                        }
                        newDoc.Save(Path.Combine(mergedDir, $"{judgingNo:000000}.pdf"));
                    }
                    else
                    {
                        if (File.Exists(eFileName))
                            File.Copy(eFileName, Path.Combine(mergedDir, $"{judgingNo:000000}.pdf"));

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }

        }
    }
}
