using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Text;

namespace ChronoBot
{
    class Program
    {
        private readonly DiscordSocketClient _client;
        private Timer _clearLog;

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

        private void ClearLog()
        {
            _clearLog = new Timer(60 * 60 * 24 * 1000); //Once a day.
            _clearLog.Enabled = true;
            _clearLog.AutoReset = true;
            _clearLog.Elapsed += _clearLog_Elapsed;
        }

        private void _clearLog_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.Clear();
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
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message}");
            Console.ForegroundColor = cc;
            return Task.CompletedTask;
        }

        private async Task MainAsync()
        {
            _client.Log += Logger;
            
            await _client.LoginAsync(TokenType.Bot, "");
            await _client.StartAsync();

            ChronoBot cb = new ChronoBot(_client);
            ClearLog();
            
            await Task.Delay(-1);
        }
    }

}
