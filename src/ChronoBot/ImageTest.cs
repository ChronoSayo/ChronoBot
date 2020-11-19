using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Discord;
using Discord.WebSocket;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

//Test class; remove later
namespace ChronoBot
{
    class ImageTest
    {
        private DiscordSocketClient _client;
        private int count;

        public ImageTest(DiscordSocketClient client)
        {
            _client = client;
            count = 0;
        }

        public void MessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot)
                return;

            HandleImage(socketMessage);
        }

        private void HandleImage(SocketMessage socketMessage)
        {
            string message = socketMessage.ToString().ToLower();
            if(message.Contains("!image"))
            {
                string[] split = message.Split(' ');
                if (split.Length != 2)
                    return;

                IReadOnlyCollection<SocketUser> users = socketMessage.MentionedUsers;
                if (users == null)
                    return;

                count++;
                string url = _client.GetUser(users.ElementAt(0).Id).GetAvatarUrl(ImageFormat.Jpeg);
                WebClient wc = new WebClient();
                wc.DownloadFile(url, $"Pics/test{count}.jpg");

                using (Image<Rgba32> image = (Image < Rgba32 > )Image.Load($"Pics/test{count}.jpg")) //open the file and detect the file type and decode it
                {
                    for (int i = 0; i < 500; i++)
                    {
                        int x = Info.GetRandom(0, 128);
                        int y = Info.GetRandom(0, 128);
                        image[x, y] = Rgba32.ParseHex("0000FF");
                    }
                    image.Save($"Pics/result{count}.jpg"); // based on the file extension pick an encoder then encode and write the data to disk

                    Info.SendFileToChannel(socketMessage, $"Pics/result{count}.jpg", "");
                    image.Dispose();
                }
                wc.Dispose();
            }
        }
    }
}
