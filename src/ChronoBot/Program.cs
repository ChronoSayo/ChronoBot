using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoBot
{
    class Program
    {
        private readonly DiscordSocketClient _client;
        private static DateTime _today;
        private static string _logFile;

        // Program entry point
        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private Program()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
            });
        }

        private static Task Logger(LogMessage message)
        {
            var cc = Console.ForegroundColor;
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.ForegroundColor = cc;
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message}");
            LogFile($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message}");
            return Task.CompletedTask;
        }

        private async Task MainAsync()
        {
            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location);
            if (!Directory.Exists(Path.Combine(exePath ?? string.Empty, "Log")))
                Directory.CreateDirectory((Path.Combine(exePath ?? string.Empty, "Log")));
            _logFile = Path.Combine(exePath ?? string.Empty, "Log",
                "ChronoBotLog-" + DateTime.Today.ToString("yyyy-dd-M--HH-mm-ss") + ".txt");
            _today = DateTime.Today;

            _client.Log += Logger;
            
            await _client.LoginAsync(TokenType.Bot, File.ReadAllText("Memory Card/DiscordToken.txt"));
            await _client.StartAsync();

            ChronoBot cb = new ChronoBot(_client);
            
            await Task.Delay(-1);
        }

        public static void LogFile(string message)
        {
            if (!File.Exists(_logFile) || DateTime.Today != _today)
            {
                _today = DateTime.Today;
                FileStream file = File.Create(_logFile);
                file.Close();
            }

            File.AppendAllLines(_logFile, new List<string> { DateTime.Now.ToString(CultureInfo.InvariantCulture), message });
        }
    }

}
