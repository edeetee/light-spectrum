using ColorMine.ColorSpaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

namespace LightController
{
    class ScreenGrab
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);




        public static IRgb getAverageColor(int width, int height, int xSamples, int ySamples)
        {
            Bitmap screenPixel = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, width, height, hSrcDC, 0, 0, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }
            
            double totalL = 0;
            double totalS = 0;
            double totalSin = 0;
            double totalCos = 0;

            for (int x =  width / xSamples / 2; x < width; x += width / xSamples)
            {
                for (int y = height / ySamples / 2; y < height; y += height / ySamples)
                {
                    Color pixel = screenPixel.GetPixel(x, y);
                    Hsl newHsv = new Rgb()
                    {
                        R = pixel.R,
                        G = pixel.G,
                        B = pixel.B
                    }.To<Hsl>();
                    double rad = newHsv.H.ToRadians();
                    totalSin += Math.Sin(rad)*newHsv.S;
                    totalCos += Math.Cos(rad)*newHsv.S;

                    totalL += newHsv.L;
                    totalS += newHsv.S;
                }
            }

            int count = xSamples * ySamples;

            double H = Math.Atan2(totalSin / totalS, totalCos / totalS).ToDegrees();
            double Hlength = Math.Sqrt(Math.Pow(totalSin / totalS, 2) + Math.Pow(totalCos / totalS, 2));

            Hsl returnVal = new Hsl()
            {
                H = H,
                S = Math.Min((totalS / count), 100),
                L = Math.Min((totalL / count), 100)
            };
            IRgb returnRgb = returnVal.ToRgb();
            return returnRgb;
        }

        public static IRgb getAverageColumns(int width, int height, int xSamples, int ySamples)
        {
            Bitmap screenPixel = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, width, height, hSrcDC, 0, 0, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            double totalV = 0;
            double totalS = 0;

            double totalSin = 0;
            double totalCos = 0;
            for (int x = xSamples / width / 2; x < width; x += width / xSamples)
            {
                for (int y = ySamples / height / 2; y < height; y += height / ySamples)
                {
                    Color pixel = screenPixel.GetPixel(x, y);
                    Hsv newHsv = new Rgb()
                    {
                        R = pixel.R,
                        G = pixel.G,
                        B = pixel.B
                    }.To<Hsv>();
                    double rad = newHsv.H.ToRadians();
                    totalSin += Math.Sin(rad);
                    totalCos += Math.Cos(rad);

                    totalV += newHsv.V;
                    totalS += newHsv.S;
                }
            }

            int count = xSamples * ySamples;

            double H = Math.Atan2(totalSin / count, totalCos / count).ToDegrees();
            double Hlength = Math.Sqrt(Math.Pow(totalSin / count, 2) + Math.Pow(totalCos / count, 2));

            Hsv returnVal = new Hsv()
            {
                H = H,
                S = Math.Min((totalS / count), 100),
                V = Math.Min((totalV / count) * 10 * Math.Min(0.1, Hlength), 100)
            };
            IRgb returnRgb = returnVal.ToRgb();
            return returnRgb;
        }
    }
}
