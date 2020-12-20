using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;

namespace ChronoBot
{
    class ImageSystem
    {

        public struct ImageInfo
        {
            public string filePath;
            public int w, h;
        }

        public ImageSystem()
        {

        }

        public string AddImagesToImage(string baseImage, List<ImageInfo> imageInfos)
        {
            string result = string.Empty;
            if (imageInfos.Count > 0)
            {
                for (int i = 0; i < imageInfos.Count; i++)
                {
                    ImageInfo ii = imageInfos[i];

                    string bg = baseImage;
                    if (i > 0)
                        bg = result;

                    result = AddImageToImage(bg, ii.filePath, ii.w, ii.h);
                }
            }
            return result;
        }

        public string AddImageToImage(string baseImage, string filePath, int width, int height)
        {
            Image<Rgba32> background = (Image<Rgba32>) Image.Load(baseImage);
            Image<Rgba32> image = null;
            string result = GetFilePathToImagesSelfie + "selfie.png";
            while (image == null)
            {
                try
                {
                    image = (Image<Rgba32>)Image.Load(filePath);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed. " + e + "\nTrying again in 2 seconds.");
                    Thread.Sleep(2000);
                }
            }
            int x = width;
            int y = height;
            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    if (image[i, j].A == 0)
                        continue;
                    background[x + i, y + j] = image[i, j];
                }
            }

            background.Save(result);

            background.Dispose();
            image.Dispose();

            Console.WriteLine("SELFIE SUCCESSFUL");

            return result;
        }

        public void DownloadImage(string url, string fileName)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFileAsync(new Uri(url), fileName);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Failed. " + e);
            }
        }

        public void Close(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
                Thread.Sleep(2000);
                Close(filePath);
            }
        }

        public string GetFilePathToImages { get { return "Images/"; } }
        public string GetFilePathToImagesSelfie { get { return GetFilePathToImages + "/Selfie/"; } }
    }
}
