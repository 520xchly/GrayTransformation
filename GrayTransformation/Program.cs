using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gif.Components;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GrayTransformation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("123");
            Console.WriteLine(args != null ? String.Join(",", args) : "");
            string deskPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string imagePath = deskPath + "/2234.png";
            Bitmap bitmap = new Bitmap(imagePath);
            int width = bitmap.Width, height = bitmap.Height;
            if (bitmap.PixelFormat == PixelFormat.Format24bppRgb || bitmap.PixelFormat == PixelFormat.Format32bppRgb)
            {
                int grayAverage = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color grayPixel = Rgb2GrayYUV(bitmap.GetPixel(x, y));
                        grayAverage += grayPixel.R;
                        bitmap.SetPixel(x, y, grayPixel);
                    }
                }
                bitmap.Save(deskPath + "/gray1234.png");
                Bitmap b2 = (Bitmap)bitmap.Clone();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color grayReversePixel = GrayRevers(bitmap.GetPixel(x, y));
                        bitmap.SetPixel(x, y, grayReversePixel);
                    }
                }
                bitmap.Save(deskPath + "/grayReverse1234.png");
                //
                grayAverage = grayAverage / (width * height);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color grayPixel = B2Gray(b2.GetPixel(x, y), grayAverage);
                        b2.SetPixel(x, y, grayPixel);
                    }
                }
                b2.Save(deskPath + "/b2gray.png");
            }
            else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed || bitmap.PixelFormat == PixelFormat.Format1bppIndexed || bitmap.PixelFormat == PixelFormat.Format4bppIndexed)
            {
                ColorPalette grayPalette = bitmap.Palette;
                ColorPalette grayReversePalette = bitmap.Palette;
                ColorPalette b2Palette = bitmap.Palette;
                int grayAverage = 0;
                for (int i = 0; i < 256; i++)
                {
                    grayAverage += i;
                    grayPalette.Entries[i] = Color.FromArgb(i, i, i);
                    grayReversePalette.Entries[i] = Color.FromArgb(255 - i, 255 - i, 255 - i);
                }
                bitmap.Palette = grayPalette;
                bitmap.Save(deskPath + "/gray1234.png");
                bitmap.Palette = grayReversePalette;
                bitmap.Save(deskPath + "/grayReverse1234.png");
                //
                grayAverage = grayAverage / (bitmap.Width * bitmap.Height);
                for (int i = 0; i < 256; i++)
                {
                    b2Palette.Entries[i] = B2Gray(Color.FromArgb(i, i, i), grayAverage);
                }
                bitmap.Palette = b2Palette;
                bitmap.Save(deskPath + "/b2gray.png");
            }
            else if (bitmap.PixelFormat == PixelFormat.Format32bppArgb)
            {
                if (ImageAnimator.CanAnimate(bitmap))
                {
                    FrameDimension frameDimension = new FrameDimension(bitmap.FrameDimensionsList[0]);
                    int fdCount = bitmap.GetFrameCount(frameDimension);
                    for (int i = 0; i < fdCount; i += 5)
                    {
                        bitmap.SelectActiveFrame(frameDimension, i);
                        bitmap.Save(deskPath + $"/giftest/gif{i}.png", ImageFormat.Png);
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                Color grayPixel = Rgb2GrayYUV(bitmap.GetPixel(x, y));
                                bitmap.SetPixel(x, y, grayPixel);
                            }
                        }
                        bitmap.Save(deskPath + $"/giftest/grayGif{i}.png", ImageFormat.Png);
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                Color grayReversePixel = GrayRevers(bitmap.GetPixel(x, y));
                                bitmap.SetPixel(x, y, grayReversePixel);
                            }
                        }
                        bitmap.Save(deskPath + $"/giftest/ReverseGrayGif{i}.png", ImageFormat.Png);
                    }
                }
                //http://files.cnblogs.com/bomo/Gif.Components.zip
                //List<string> pngfiles = new List<string>();
                //for (int n = 0; n < 160; n += 5)
                //{
                //    string filePath = deskPath + $"/giftest/grayGif{n}.png";
                //    pngfiles.Add(filePath);
                //}
                //AnimatedGifEncoder encoder = new AnimatedGifEncoder();
                //encoder.Start(deskPath + "/giftest/生成gif.gif");
                //encoder.SetDelay(1000);
                //encoder.SetRepeat(0);// 0 repeat  -1 norepeat
                //pngfiles.ForEach(file =>
                //{
                //    encoder.AddFrame(Bitmap.FromFile(file));
                //});
                //encoder.Finish();
            }

            bitmap = null;
            //CreateWebHostBuilder(args).Build().Run();
        }

        private static Color Rgb2GrayYUV(Color rgbPixel)
        {
            int grayValue = (int)(rgbPixel.R * 0.3 + rgbPixel.G * 0.59 + rgbPixel.B * 0.11);
            return Color.FromArgb(grayValue, grayValue, grayValue);
        }

        private static Color GrayRevers(Color grayPixel)
        {
            return Color.FromArgb(255 - grayPixel.R, 255 - grayPixel.G, 255 - grayPixel.B);
        }

        private static Color B2Gray(Color grayPixel, int grayAverage)
        {
            return grayPixel.R > grayAverage ? Color.FromArgb(255, 255, 255) : Color.FromArgb(0, 0, 0);
        }

        private static void Test()
        {
            Bitmap rgb24Bitmap = new Bitmap(2, 2, PixelFormat.Format24bppRgb);
            rgb24Bitmap.SetPixel(0, 0, Color.Red);
            rgb24Bitmap.SetPixel(1, 0, Color.White);
            rgb24Bitmap.SetPixel(0, 1, Color.Blue);
            rgb24Bitmap.SetPixel(1, 1, Color.Orange);
            rgb24Bitmap.Save("/测试坐标.png");
            rgb24Bitmap = null;
        }

        private static void DoNothing() { }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
