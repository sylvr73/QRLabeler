using QRCodeEncoderLibrary;
using QRCodeDecoderLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using QRCoder;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;
using OpenCvSharp;
using PdfSharp.Pdf.Content.Objects;

namespace QRLabeler.Utilities
{
    public static class QRHelpers
    {
        public static Bitmap GetQRCode(string value)
        {
            var encoder = new QREncoder
            {
                ErrorCorrection = QRCodeEncoderLibrary.ErrorCorrection.H,
                ModuleSize = 2,
                QuietZone = 8
            };
            encoder.Encode(value);
            return encoder.CreateQRCodeBitmap();
        }


        public static string ReadQRCode(Bitmap image)
        {
            var decoder = new QRDecoder();

            var data = decoder.ImageDecoder(image);
            var turns = 1;

            while ((null == data || data.Length == 0) && turns < 4)
            {
                //Console.WriteLine("turns=" + turns);
                image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                data = decoder.ImageDecoder(image);
                turns++;
            }

            if (null == data || data.Length == 0)
            {
                //Console.WriteLine("code not found");
                return string.Empty;
            }

            if (data.Length > 1)
            {
                Console.WriteLine($"Found {data.Length} codes on page");
                return string.Empty;
            }

            var utfDecoder = Encoding.UTF8.GetDecoder();
            var count = utfDecoder.GetCharCount(data[0], 0, data[0].Length);
            var charArray = new char[count];
            utfDecoder.GetChars(data[0], 0, data[0].Length, charArray, 0);
            return new string(charArray);
        }

        public static Bitmap GetQRCode2(string value)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(3);
            return qrCodeImage;
        }

        public static Bitmap ExtractQRCodeAI(Bitmap inputBitmap)
        {
            var reader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    ReturnCodabarStartEnd = false
                }
            };

            var result = reader.Decode(inputBitmap);
            if (result == null)
            {
                throw new Exception("No QR code found in the provided image.");
            }

            var points = result.ResultPoints;
            if (points == null || points.Length < 3)
            {
                throw new Exception("Invalid QR code detection.");
            }

            // Get the bounding box from detected points
            float minX = Math.Min(points[0].X, Math.Min(points[1].X, points[2].X));
            float minY = Math.Min(points[0].Y, Math.Min(points[1].Y, points[2].Y));
            float maxX = Math.Max(points[0].X, Math.Max(points[1].X, points[2].X));
            float maxY = Math.Max(points[0].Y, Math.Max(points[1].Y, points[2].Y));

            // Compute width and height
            int width = (int)(maxX - minX);
            int height = (int)(maxY - minY);

            // Extract QR Code region
            Rectangle cropRect = new Rectangle((int)minX, (int)minY, width, height);
            Bitmap croppedBitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(croppedBitmap))
            {
                g.DrawImage(inputBitmap, new Rectangle(0, 0, width, height), cropRect, GraphicsUnit.Pixel);
            }

            return croppedBitmap;
        }

        public static Bitmap AdjustContrast(Bitmap image, float contrast)
        {
            Bitmap newImage = new Bitmap(image.Width, image.Height);
            float adjustment = (100.0f + contrast) / 100.0f;
            adjustment *= adjustment;

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color oldColor = image.GetPixel(x, y);
                    float red = oldColor.R / 255.0f;
                    float green = oldColor.G / 255.0f;
                    float blue = oldColor.B / 255.0f;

                    red = (((red - 0.5f) * adjustment) + 0.5f) * 255.0f;
                    green = (((green - 0.5f) * adjustment) + 0.5f) * 255.0f;
                    blue = (((blue - 0.5f) * adjustment) + 0.5f) * 255.0f;

                    int r = (int)((red < 0) ? 0 : ((red > 255) ? 255 : red));
                    int g = (int)((green < 0) ? 0 : ((green > 255) ? 255 : green));
                    int b = (int)((blue < 0) ? 0 : ((blue > 255) ? 255 : blue));

                    Color newColor = Color.FromArgb(r, g, b);
                    newImage.SetPixel(x, y, newColor);
                }
            }

            return newImage;
        }
        public static Bitmap ToBlackAndWhite(Bitmap image)
        {
            Bitmap newImage = new Bitmap(image.Width, image.Height);
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color oldColor = image.GetPixel(x, y);
                    var sum = oldColor.R + oldColor.G + oldColor.B;
                    var newColor = sum < 600 ? Color.Black : Color.White;
                    newImage.SetPixel(x, y, newColor);
                }
            }

            return newImage;
        }


        public static Bitmap ToBlackAndWhiteOptimized(Bitmap image)
        {
            Bitmap newImage = new Bitmap(image.Width, image.Height);

            BitmapData data = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            BitmapData newData = newImage.LockBits(
                new Rectangle(0, 0, newImage.Width, newImage.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            int bytesPerPixel = Image.GetPixelFormatSize(PixelFormat.Format24bppRgb) / 8;
            int byteCount = data.Stride * image.Height;
            byte[] pixelBuffer = new byte[byteCount];
            byte[] resultBuffer = new byte[byteCount];

            Marshal.Copy(data.Scan0, pixelBuffer, 0, byteCount);
            image.UnlockBits(data);

            for (int y = 1; y < image.Height - 1; y++)
            {
                for (int x = 1; x < image.Width - 1; x++)
                {
                    int position = y * data.Stride + x * bytesPerPixel;
                    int sum = pixelBuffer[position] + pixelBuffer[position + 1] + pixelBuffer[position + 2];

                    byte color = sum < 600 ? (byte)0 : (byte)255;
                    resultBuffer[position] = color;
                    resultBuffer[position + 1] = color;
                    resultBuffer[position + 2] = color;
                }
            }

            FillSurroundedWhite(resultBuffer, image.Width, image.Height, bytesPerPixel, data.Stride);

            Marshal.Copy(resultBuffer, 0, newData.Scan0, byteCount);
            newImage.UnlockBits(newData);

            return newImage;
        }

        private static void FillSurroundedWhite(byte[] buffer, int width, int height, int bpp, int stride)
        {
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int pos = y * stride + x * bpp;
                    if (buffer[pos] == 255)
                    {
                        bool surrounded =
                            buffer[pos - bpp] == 0 && buffer[pos + bpp] == 0 && // Left and right
                            buffer[pos - stride] == 0 && buffer[pos + stride] == 0; // Top and bottom

                        if (surrounded)
                        {
                            buffer[pos] = 0;
                            buffer[pos + 1] = 0;
                            buffer[pos + 2] = 0;
                        }
                    }
                }
            }
        }

        public static Bitmap ReduceNoiseAndSmooth(Bitmap inputBitmap)
        {
            // Convert Bitmap to OpenCV Mat
            Mat image = OpenCvSharp.Extensions.BitmapConverter.ToMat(inputBitmap);

            // Ensure the image is grayscale (binary)
            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGRA2GRAY);

            // Apply Gaussian Blur (smoothens noise while preserving structure)
            Mat blurred = new Mat();
            Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);

            // Apply Otsu’s Thresholding (binarizes the image again after smoothing)
            Mat binary = new Mat();
            Cv2.Threshold(blurred, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // Apply Morphological Operations (Erosion to remove noise, then Dilation to restore shapes)
            Mat morphKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));

            Mat eroded = new Mat();
            Cv2.Erode(binary, eroded, morphKernel, iterations: 1);

            Mat cleaned = new Mat();
            Cv2.Dilate(eroded, cleaned, morphKernel, iterations: 1);

            // Convert back to Bitmap
            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(cleaned);
        }

        public static string ReadBarcode(Bitmap bitmap)
        {
            var barcodeReader = new BarcodeReader();
            var zresult = barcodeReader.Decode(bitmap);
            if (null != zresult && false == string.IsNullOrEmpty(zresult.Text))
                return zresult.Text;
            return null;
        }
    }
}
