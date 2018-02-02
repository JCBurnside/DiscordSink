using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Events;

//compile with: /doc:DiscordServer.xml
namespace DiscordSink
{
    public class Server
    {   
        internal string ServerName { get; private set; }
        internal ulong? ServerId { get; private set; }
        internal Dictionary<ulong,LoggingLevel> Levels = new Dictionary<ulong, LoggingLevel>();
        internal Dictionary<string, LoggingLevel> UnProcessedData = new Dictionary<string, LoggingLevel>();
        internal bool IsReady { get; private set; } = false;

        #region Named INITS
        ///<summary>
        ///Constructs a new server object
        ///</summary>
        ///<param name="name">name of server (case sensitive)</param>
        ///<param name="levels">A dictionary of loging levels(string) and the channels(List of string(case sensitive)) to log to at that level</param>
        public Server(string name, Dictionary<string, LoggingLevel> levels)
        {
            ServerName = name;
            UnProcessedData = levels;
        }

        /// <summary>
        /// Constructs a new server object
        /// </summary>
        /// <param name="name">name of server (case sensitive)</param>
        /// <param name="channels">a List of strings of the channel names to log to (case sensitive and all levels)</param>
        public Server(string name, List<string> channels) : this(name, channels.Distinct().ToDictionary(x=>x,_=>LoggingLevel.All)) { }
        #endregion

        #region ServerName INITS with id'ed channels
        /// <summary>
        /// Creates new server instance where channel ID's are taken;
        /// </summary>
        /// <param name="name">Server Name</param>
        /// <param name="channels">Channel ID's (auto put in the all level) </param>
        public Server(string name, List<ulong> channels) : this(name, channels.Distinct().ToDictionary(x=>x,_=>LoggingLevel.All)) { }
        /// <summary>
        /// Creates new server instance where a dictionary of Levels and ID's are Taken;
        /// </summary>
        /// <param name="name"></param>
        /// <param name="_Levels"></param>
        public Server(string name, Dictionary<ulong,LoggingLevel> _Levels)
        {
            ServerName = name;
            Levels = _Levels;
        }
        #endregion

        #region ID'ED SERVER INITs WITH NAMED CHANNELS
        /// <summary>
        /// Creates instance of server with id of server and Dictionary 
        /// of logging levels and lists of channels
        /// </summary>
        /// <param name="id">Id of server</param>
        /// <param name="data">Dictionary of logging levels and channel names</param>
        public Server(ulong id, Dictionary<string, LoggingLevel> data)
        {
            ServerId = id;
            UnProcessedData = data;
        }

        /// <summary>
        /// Creates instance of server with id of server and
        /// List of channel names.
        /// </summary>
        /// <param name="id">id of server</param>
        /// <param name="list">list of channel names</param>
        public Server(ulong id, List<string> list) : this(id,list.Distinct().ToDictionary(x=>x,_=>LoggingLevel.All)) { }
        #endregion

        #region IDED INITS
        /// <summary>
        /// Creates new server instance with channels for each level
        /// </summary>
        /// <param name="id">ulong of server id</param>
        /// <param name="_Levels">Dictionary of channels and levels</param>
        public Server(ulong id, Dictionary<ulong, LoggingLevel> _Levels)
        {
            ServerId = id;
            Levels = _Levels;
        }

        /// <summary>
        /// Creates new server instance with channels auto placed in all
        /// </summary>
        /// <param name="id">ulong of server id</param>
        /// <param name="channels">id's of channels to log everything</param>
        public Server(ulong id, List<ulong> channels) : this(id, channels.Distinct().ToDictionary(x=>x,_=>LoggingLevel.All)) { }
        #endregion

        #region SHORTCUTS FOR LEVELS
        internal IEnumerable<ulong> Info
        {
            get
            {
                return new List<ulong>(from c in Levels where c.Value.HasFlag(LoggingLevel.Information) select c.Key);
            }
        }

        internal IEnumerable<ulong> Debug
        {
            get
            {
                return new List<ulong>(from c in Levels where c.Value.HasFlag(LoggingLevel.Debug) select c.Key);
            }
        }

        internal IEnumerable<ulong> Warning
        {
            get
            {
                return new List<ulong>(from c in Levels where c.Value.HasFlag(LoggingLevel.Warning) select c.Key);
            }
        }

        internal IEnumerable<ulong> Error
        {
            get
            {
                return new List<ulong>(from c in Levels where c.Value.HasFlag(LoggingLevel.Error) select c.Key);
            }
        }

        internal IEnumerable<ulong> Fatal
        {
            get
            {
                return new List<ulong>(from c in Levels where c.Value.HasFlag(LoggingLevel.Fatal) select c.Key);
            }
        }

        internal IEnumerable<ulong> Verb
        {
            get
            {
                return (from c in Levels where c.Value.HasFlag(LoggingLevel.Verbose) select c.Key);
            }
        }
        #endregion


        internal void ProccessWithClient(DiscordSocketClient client)
        {
            SocketGuild server = null;
            if (!ServerId.HasValue && !String.IsNullOrWhiteSpace(ServerName))
            {
                server = (from g in client.Guilds where g.Name == ServerName select g).FirstOrDefault();
                ServerId = server.Id;
            }
            else
            {
                server = client.GetGuild(ServerId ?? 0);
            }
            if (server == null)
            {
                return;
            }
            foreach (KeyValuePair<string, LoggingLevel> pair in UnProcessedData)
            {
                ulong channelId = (from c in server.Channels where c.Name == pair.Key select c.Id).FirstOrDefault();
                if (channelId == 0)
                    continue;
                if (Levels.ContainsKey(channelId))
                    Levels[channelId] |= pair.Value;
                else
                    Levels.Add(channelId, pair.Value);    
            }
            IsReady = true;
        }
    }
    [Flags]
    public enum LoggingLevel
    { 
        Verbose = 1<<1,
        Debug = 1<<2,
        Information = 1<<3,
        Warning=1<<4,
        Error = 1<<5,
        Fatal = 1<<6,
        All = 1<<7
    }
}
