
# YOU WILL NEED TO REGISTER YOUR APP WITH DISCORD TO GET A BOT TOKEN
You can do that [here](https://discordapp.com/developers/applications/me)

Once you have your token you can proceed with either getting your server and channel ids (you will have to enable developer mode first) or just use the server and channel names (NOT RECOMMENDED, NAMES CAN BE DUPLICATE AND LEAD TO IT LOGGING TO THE WRONG SPOT).

can either create a list of server objects to pass into the sink or (if you are only using one server) you can create a dictionary either ulongs (ids) or strings (names) and LogginLevel's (flagged enum).  By default you only need a server name/id.   This will assume you will have have different channels for each level and they are named as such. If you provide any channels at all it will only use the provided.

## Logging levels
All of the default serilog logging levels are supported with an additional all.
This is done through the `LoggingLevel` enum.  You can combine logging levels by doing `LoggingLevel.one | LoggingLevel.two` (note:All will override everything so there is no need to use all with any other level)