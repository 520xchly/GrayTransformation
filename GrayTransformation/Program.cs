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
        private static string deskPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public static void Main(string[] args)
        {
            Console.WriteLine(@"使用说明：
1、xxx.exe cmd->gray imagepath   -- 在桌面生成 【 /grayxxx.png(.jpg等)灰度图 】【 /grayReversexxx.png(.jpg等)灰度反转图】【 / b2grayxxx.png(.jpg)灰度二值化图】 【支持gif】
2、xxx.exe cmd->togif imagepath1 imagepath2 imagepath3 --【在桌面组装生成多张图片成为1234.gif，以空格分隔多张图片路径】
3、xxx.exe cmd->quit -- 退出
                ");

            while (true)
            {
                try
                {
                    string strs = Console.ReadLine();
                    List<string> strArray = new List<string>();
                    if (!string.IsNullOrWhiteSpace(strs))
                    {
                        strs.Split(" ").ToList().ForEach(str =>
                        {
                            if (!string.IsNullOrWhiteSpace(str))
                            {
                                strArray.Add(str);
                            }
                        });
                        if (strArray != null && strArray.Count > 0)
                        {
                            int cmdCount = strArray.Count;
                            string operation = strArray[0];

                            if (operation.Equals("gray", StringComparison.CurrentCultureIgnoreCase))
                            {
                                string path = strArray[1];
                                string oldBmpName = Path.GetFileName(path);
                                Bitmap bmp = new Bitmap(path);
                                int width = bmp.Width, height = bmp.Height;
                                if (bmp.PixelFormat == PixelFormat.Format24bppRgb || bmp.PixelFormat == PixelFormat.Format32bppRgb)
                                {
                                    RGBCreateGrayBitMap(bmp, oldBmpName, width, height);
                                }
                                else if (bmp.PixelFormat == PixelFormat.Format8bppIndexed || bmp.PixelFormat == PixelFormat.Format1bppIndexed || bmp.PixelFormat == PixelFormat.Format4bppIndexed)
                                {
                                    IndexCreateGrayBitMap(bmp, oldBmpName, width, height);
                                }
                                else if (bmp.PixelFormat == PixelFormat.Format32bppArgb)
                                {
                                    GifCreateGrayBitMap(bmp, oldBmpName, width, height);
                                }
                                bmp = null;
                                Console.WriteLine("转换完成");
                                continue;
                            }
                            else if (operation.Equals("togif", StringComparison.CurrentCultureIgnoreCase))
                            {
                                strArray.RemoveAt(0);
                                if (strArray.Count != 0)
                                {
                                    ToGitBitmap(strArray);
                                    Console.WriteLine("转换完成");
                                    continue;
                                }
                            }
                            else if (operation.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                            {
                                break;
                            }
                        }
                    }
                    Console.WriteLine("该操作无效");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"错误：{ex.Message}");
                    continue;
                }
            }

            Console.WriteLine("输入回车键退出图片灰度转换程序！");
            Console.ReadLine();
        }



        //由多张bitmapPaht -> 生成gif
        private static void ToGitBitmap(List<string> bitmapPath)
        {
            //http://files.cnblogs.com/bomo/Gif.Components.zip
            AnimatedGifEncoder encoder = new AnimatedGifEncoder();
            encoder.Start(deskPath + "/test.gif");
            encoder.SetDelay(1000);
            encoder.SetRepeat(0);// 0 repeat  -1 norepeat
            bitmapPath.ForEach(file =>
            {
                encoder.AddFrame(Bitmap.FromFile(file));
            });
            encoder.Finish();
            encoder = null;
        }

        //该格式常见于gif
        private static void GifCreateGrayBitMap(Bitmap bitmap, string oldBmpName, int width, int height)
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
        }

        //索引格式生成灰度图
        private static void IndexCreateGrayBitMap(Bitmap bitmap, string oldBmpName, int width, int height)
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
            bitmap.Save(deskPath + $"/gray{oldBmpName}");
            bitmap.Palette = grayReversePalette;
            bitmap.Save(deskPath + $"/grayReverse{oldBmpName}");
            //
            grayAverage = grayAverage / (bitmap.Width * bitmap.Height);
            for (int i = 0; i < 256; i++)
            {
                b2Palette.Entries[i] = B2Gray(Color.FromArgb(i, i, i), grayAverage);
            }
            bitmap.Palette = b2Palette;
            bitmap.Save(deskPath + $"/b2gray{oldBmpName}");
        }

        //rgb格式生成灰度图
        private static void RGBCreateGrayBitMap(Bitmap bmp, string oldBmpName, int width, int height)
        {
            int grayAverage = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color grayPixel = Rgb2GrayYUV(bmp.GetPixel(x, y));
                    grayAverage += grayPixel.R;
                    bmp.SetPixel(x, y, grayPixel);
                }
            }
            bmp.Save(deskPath + $"/gray{oldBmpName}");
            Bitmap b2 = (Bitmap)bmp.Clone();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color grayReversePixel = GrayRevers(bmp.GetPixel(x, y));
                    bmp.SetPixel(x, y, grayReversePixel);
                }
            }
            bmp.Save(deskPath + $"/grayReverse{oldBmpName}");
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
            b2.Save(deskPath + $"/b2gray{oldBmpName}");
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

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
