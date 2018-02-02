using System;
using Serilog;
using System.Collections.Generic;

namespace DiscordSink.Example
{
    class Program
    {
        static void Main(string[] args)
        {

            String botToken = "<Token Here>";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.DiscordSink(botToken,new Server[] {
                    new Server(/*Server Name/id here*/, new Dictionary</*ulong|string*/, LoggingLevel>{ { /*channel name/id*/, LoggingLevel.Verbose }, { /*channel name/id*/, LoggingLevel.Warning | LoggingLevel.Error } })
                })
                .CreateLogger();
            Log.Logger.Verbose("HELLO");
            do
            {
                string input = Console.ReadLine();
                if (input == ":q")
                    break;
                Log.Logger.Verbose(input);
            } while (true);
        }
    }
}
