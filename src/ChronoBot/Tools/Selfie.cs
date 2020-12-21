using System.Collections.Generic;
using ChronoBot.Systems;
using Discord;
using Discord.WebSocket;

namespace ChronoBot.Tools
{
    class Selfie
    {
        private DiscordSocketClient _client;
        private readonly ImageSystem _imageSystem;
        private const string Command = Info.COMMAND_PREFIX + "selfie";

        public Selfie(DiscordSocketClient client)
        {
            _client = client;
            _imageSystem = new ImageSystem();
        }

        public void MessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot)
                return;

            TakeSelfie(socketMessage);
        }

        private void TakeSelfie(SocketMessage socketMessage)
        {
            string message = socketMessage.ToString();
            if (message != Command)
                return;

            Info.SendMessageToChannel(socketMessage, ":camera_with_flash:");

            string filePath = _imageSystem.GetFilePathToImagesSelfie + "avatar.png";
            _imageSystem.DownloadImage(socketMessage.Author.GetAvatarUrl(ImageFormat.Png), filePath);
            
            List<ImageSystem.ImageInfo> images = new List<ImageSystem.ImageInfo>();
            images.Add(CreateImageInfo(filePath, 28, 44));
            images.Add(CreateImageInfo(_imageSystem.GetFilePathToImagesSelfie + "filter.png", 24, 41));

            string result = _imageSystem.AddImagesToImage(_imageSystem.GetFilePathToImagesSelfie + "frame.png", images);

            Info.SendFileToChannel(socketMessage, result, "kaway des yu nay");

            _imageSystem.Close(filePath);
        }

        private ImageSystem.ImageInfo CreateImageInfo(string filePath, int w, int h)
        {
            ImageSystem.ImageInfo imageInfo = new ImageSystem.ImageInfo();
            imageInfo.FilePath = filePath;
            imageInfo.W = w;
            imageInfo.H = h;
            return imageInfo;
        }
    }
}
