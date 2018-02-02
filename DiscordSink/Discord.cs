using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//compile with: /doc:DiscordSink.xml
namespace DiscordSink
{
    public class DiscordSink : ILogEventSink, IDisposable
    {
        private bool IsReady = false;

        private DiscordSocketClient client;
        string botToken = "";

        IFormatProvider formatProvider;
        ConcurrentBag<Server> servers;

        ConcurrentQueue<LogEvent> queue = new ConcurrentQueue<LogEvent>();

        public DiscordSink(string bot, IReadOnlyList<Server> _servers, IFormatProvider _formatProvider)
        {
            botToken = bot;
            formatProvider = _formatProvider;
            servers = new ConcurrentBag<Server>(_servers);

            client = new DiscordSocketClient();
            client.Ready += Client_Ready;
            client.LoggedIn += () =>
            {
                IsReady = true;
                return Task.CompletedTask;
            };
        }

        private async Task SignIn()
        {
            await client.LoginAsync(TokenType.Bot, botToken);
            await client.StartAsync();
            IsReady = true;
        }

        private Task Client_Ready()
        {
            foreach (Server s in servers)
            {
                s.ProccessWithClient(client);
            }
            while (queue.Count() > 0)
            {
                LogEvent evt;
                if (queue.TryDequeue(out evt))
                    Emit(evt);
            }
            return Task.CompletedTask;
        }

        public async void Emit(LogEvent logEvent)
        {
            if (!IsReady)
            {
                await SignIn();
            }

            if (servers.Any(s => !s.IsReady))
            {
                queue.Enqueue(logEvent);
                return;
            }
            IEnumerable<ulong> GetChannels(Server server, LogEventLevel level)
            {
                switch (level)
                {
                    case LogEventLevel.Verbose: return server.Verb;
                    case LogEventLevel.Debug: return server.Debug;
                    case LogEventLevel.Information: return server.Info;
                    case LogEventLevel.Warning: return server.Warning;
                    case LogEventLevel.Error: return server.Error;
                    case LogEventLevel.Fatal: return server.Fatal;
                    default: return new ulong[] { };
                }
            }
            var channels = from server in servers
                           from channelId in GetChannels(server, logEvent.Level)
                           select (server.ServerId ?? 0, channelId);

            foreach (var (serverId, channelId) in channels)
            {
                SendMessage(serverId, channelId, logEvent.RenderMessage(formatProvider));
            }
        }

        private async void SendMessage(ulong server, ulong channel, string message)
        {
            SocketGuild guild = client.GetGuild(server);
            if (guild == null)
            {
                Log.Logger.Error($"no channel found with id {server}");
                return;
            }
            SocketTextChannel socketChannel = guild.GetTextChannel(channel);
            if (socketChannel == null)
            {
                Log.Logger.Error($"no channel found with name {channel}");
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
            => loggerSinkConfiguration.DiscordSink(botString, new List<Server>(new Server[] { server }), provider);

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
