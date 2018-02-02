using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//compile with: /doc:DiscordSink.xml
namespace DiscordSink
{
    public class DiscordSink : ILogEventSink, IDisposable
    {

        private DiscordSocketClient client;
        string botToken = "";

        IFormatProvider formatProvider;
        IReadOnlyList<Server> servers;

        Queue<LogEvent> queue = new Queue<LogEvent>();

        public DiscordSink(string bot, IReadOnlyList<Server> _servers, IFormatProvider _formatProvider)
        {
            botToken = bot;
            formatProvider = _formatProvider;
            servers = _servers;

            client = new DiscordSocketClient();
            Task.Run(() => SignIn()).Wait();
        }

        public async Task SignIn()
        {
            await client.LoginAsync(TokenType.Bot, botToken);
            await client.StartAsync();
            client.Ready += Client_Ready;
        }

        private Task Client_Ready()
        {
            foreach (Server s in servers)
            {
                s.ProccessWithClient(ref client);
            }
            while (queue.Count() > 0)
                Emit(queue.Dequeue());
            return Task.CompletedTask;
        }

        public void Emit(LogEvent logEvent)
        {
            if (servers.Any(s => !s.IsReady))
            {
                queue.Enqueue(logEvent);
                return;
            }
            switch (logEvent.Level)
            {
                case LogEventLevel.Verbose:
                    foreach (Server server in servers)
                        foreach (ulong channel in server.Verb)
                            SendMessage(server.ServerId ?? 0, channel, logEvent.RenderMessage(formatProvider));
                    break;
                case LogEventLevel.Debug:
                    foreach (Server server in servers)
                        foreach (ulong channel in server.Debug)
                            SendMessage(server.ServerId ?? 0, channel, logEvent.RenderMessage(formatProvider));
                    break;
                case LogEventLevel.Information:
                    foreach (Server server in servers)
                        foreach (ulong channel in server.Info)
                            SendMessage(server.ServerId ?? 0, channel, logEvent.RenderMessage(formatProvider));
                    break;
                case LogEventLevel.Warning:
                    foreach (Server server in servers)
                        foreach (ulong channel in server.Warning)
                            SendMessage(server.ServerId ?? 0, channel, logEvent.RenderMessage(formatProvider));
                    break;
                case LogEventLevel.Error:
                    foreach (Server server in servers)
                        foreach (ulong channel in server.Error)
                            SendMessage(server.ServerId ?? 0, channel, logEvent.RenderMessage(formatProvider));
                    break;
                case LogEventLevel.Fatal:
                    foreach (Server server in servers)
                        foreach (ulong channel in server.Fatal)
                            SendMessage(server.ServerId ?? 0, channel, logEvent.RenderMessage(formatProvider));
                    break;
            }
        }

        private async void SendMessage(ulong server, ulong channel, string message)
        {
            SocketGuild guild = client.GetGuild(server);
            if (guild == null)
            {
                Console.WriteLine($"no server found with name {server}");
                return;
            }
            SocketTextChannel socketChannel = guild.GetTextChannel(channel);
            if (socketChannel == null)
            {
                Console.WriteLine($"no channel found with name {channel}");
                return;
            }
            await socketChannel.SendMessageAsync(message);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }



        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }



    public static class DiscordSinkExtensions
    {
        public static LoggerConfiguration DiscordSink(this LoggerSinkConfiguration logerConfiguration, string botString, IReadOnlyList<Server> servers, IFormatProvider provider = null)
        => logerConfiguration.Sink(new DiscordSink(botString, servers, provider));

        public static LoggerConfiguration DiscordSink(this LoggerSinkConfiguration loggerSinkConfiguration, string botString, Server server, IFormatProvider provider = null)
        => loggerSinkConfiguration.DiscordSink(botString, new Server[] { server }, provider);

        public static LoggerConfiguration DiscordSink(
        this LoggerSinkConfiguration loggerConfiguration,
        string botString,
        string server,
        Dictionary<string, LoggingLevel> channels,
        IFormatProvider provider = null)
            => loggerConfiguration.DiscordSink(botString, new List<Server>(new Server[] { new Server(server, channels) }), provider);

        public static LoggerConfiguration DiscordSink(this LoggerSinkConfiguration loggerSinkConfiguration, string botString, ulong server, IFormatProvider provider = null)
            => loggerSinkConfiguration.DiscordSink(botString, new ulong[] { server }, provider);

        public static LoggerConfiguration DiscordSink(this LoggerSinkConfiguration loggerSinkConfiguration, string botString, IReadOnlyList<ulong> servers, IFormatProvider provider = null)
        {
            List<Server> data = new List<Server>();
            foreach (ulong server in servers)
                data.Add(new Server(server, new Dictionary<string, LoggingLevel>{
                    { "info", LoggingLevel.Information },
                    { "debug", LoggingLevel.Debug },
                    { "error", LoggingLevel.Error },
                    { "fatal", LoggingLevel.Fatal },
                    { "verbose", LoggingLevel.Verbose },
                    { "warn", LoggingLevel.Warning }
                }));
            return loggerSinkConfiguration.DiscordSink(botString, data, provider);
        }

        public static LoggerConfiguration DiscordSink(this LoggerSinkConfiguration loggerConfiguration, string botString, IReadOnlyList<string> servers, IFormatProvider provider = null)
        {
            List<Server> data = new List<Server>();
            foreach (string server in servers)
            {
                data.Add(new Server(server, new Dictionary<string, LoggingLevel>{
                    { "info", LoggingLevel.Information },
                    { "debug", LoggingLevel.Debug },
                    { "error", LoggingLevel.Error },
                    { "fatal", LoggingLevel.Fatal },
                    { "verbose", LoggingLevel.Verbose },
                    { "warn", LoggingLevel.Warning }
                }));
            }
            return loggerConfiguration.DiscordSink(botString, data, provider);
        }


        public static LoggerConfiguration DiscordSink(this LoggerSinkConfiguration loggerConfiguration, string botString, string server, IFormatProvider provider = null)
            => loggerConfiguration.DiscordSink(botString, new string[] { server }, provider);
    }
}
