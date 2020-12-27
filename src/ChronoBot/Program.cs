using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
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
            try
            {
                new Program().MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Logger(new LogMessage(LogSeverity.Critical, "Main", e.Message, e));
                Console.WriteLine(e);
                Console.WriteLine(e.Message);
            }
        }

        private Program()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
            });
        }

        public static Task Logger(LogMessage message)
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
            string exception = message.Exception == null ? string.Empty : $"\n{message.Exception}";
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {exception}");

            if (!File.Exists(_logFile) || DateTime.Today != _today)
            {
                _today = DateTime.Today;
                FileStream file = File.Create(_logFile);
                file.Close();
            }

            File.AppendAllLines(_logFile, new List<string> { $"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {exception}" });

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
    }

}
