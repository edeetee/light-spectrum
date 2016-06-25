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

            double totalHue = 0;
            double totalLightness = 0;
            double totalS = 0;
            double totalHueCount = 0;
            //Rgb total = new Rgb()
            //{
            //    R = 0,
            //    G = 0,
            //    B = 0
            //};

            for (int x = xSamples / width / 2; x < width; x += width / xSamples)
            {
                for (int y = ySamples / height / 2; y < height; y += height / ySamples)
                {
                    Color pixel = screenPixel.GetPixel(x, y);
                    Hsl newHsv = new Rgb()
                    {
                        R = pixel.R,
                        G = pixel.G,
                        B = pixel.B
                    }.To<Hsl>();
                    totalLightness += newHsv.L;
                    totalS += newHsv.S;
                    double hueCount = (1-Math.Abs((newHsv.L)/100 * 2 - 1)) * (newHsv.S/100);
                    totalHue += newHsv.H * (0.5 + 0.5 * hueCount) + (360 - newHsv.H) * (1.0 - 0.5 * hueCount);
                    totalHueCount += 1;
                    //total.R += pixel.R;
                    //total.G += pixel.G;
                    //total.B += pixel.B;
                }
            }

            int count = xSamples * ySamples;
            Hsl returnVal = new Hsl()
            {
                H = (totalHue / totalHueCount)%360,
                S = (totalS / count)%1,
                L = (totalLightness / count)%1
            };
            IRgb returnRgb = returnVal.ToRgb();
            return returnRgb;
            //total.R /= count;
            //total.G /= count;
            //total.B /= count;
            //return total;
        }

        public static IRgb getAverageColorSlow(int width, int height, int xSamples, int ySamples)
        {
            double totalHue = 0;
            double totalLightness = 0;

            IntPtr hdc = GetDC(IntPtr.Zero);
            for(int x = 0; x < xSamples; x += xSamples / width)
            {
                for(int y = 0; y < ySamples; y += ySamples / height)
                {
                    uint pixel = GetPixel(hdc, x, y);
                    Hsl newHsv = new Rgb()
                    {
                        R = ((int)(pixel & 0x000000FF)) / 255.0,
                        G = ((int)(pixel & 0x0000FF00) >> 8) / 255.0,
                        B = ((int)(pixel & 0x00FF0000) >> 16) / 255.0
                    }.To<Hsl>();
                    totalHue += newHsv.H;
                    totalLightness += newHsv.L;
                }
            }
            ReleaseDC(IntPtr.Zero, hdc);

            int count = width * height;
            return new Hsl()
            {
                H = totalHue / count,
                L = totalLightness / count
            }.ToRgb();
        }
    }
}
