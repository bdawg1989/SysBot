using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static Discord.GatewayIntents;
using static SysBot.Pokemon.DiscordSettings;

namespace SysBot.Pokemon.Discord;

public static class SysCordSettings
{
    public static DiscordManager Manager { get; internal set; } = default!;
    public static DiscordSettings Settings => Manager.Config;
    public static PokeTradeHubConfig HubConfig { get; internal set; } = default!;
}

public sealed class SysCord<T> where T : PKM, new()
{
    public static PokeBotRunner<T> Runner { get; private set; } = default!;

    private readonly CancellationTokenSource _statusUpdateCancellationTokenSource = new CancellationTokenSource();

    private readonly DiscordSocketClient _client;
    private readonly DiscordManager Manager;
    public readonly PokeTradeHub<T> Hub;

    // Keep the CommandService and DI container around for use with commands.
    // These two types require you install the Discord.Net.Commands package.
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;

    // Track loading of Echo/Logging channels, so they aren't loaded multiple times.
    private bool MessageChannelsLoaded { get; set; }

    public SysCord(PokeBotRunner<T> runner)
    {
        Runner = runner;
        Hub = runner.Hub;
        Manager = new DiscordManager(Hub.Config.Discord);

        SysCordSettings.Manager = Manager;
        SysCordSettings.HubConfig = Hub.Config;

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            // How much logging do you want to see?
            LogLevel = LogSeverity.Info,
            GatewayIntents = Guilds | GuildMessages | DirectMessages | GuildMembers | GuildPresences | MessageContent,
            // If you or another service needs to do anything with messages
            // (ex. checking Reactions, checking the content of edited/deleted messages),
            // you must set the MessageCacheSize. You may adjust the number as needed.
            //MessageCacheSize = 50,
        });

        _commands = new CommandService(new CommandServiceConfig
        {
            // Again, log level:
            LogLevel = LogSeverity.Info,

            // This makes commands get run on the task thread pool instead on the websocket read thread.
            // This ensures long-running logic can't block the websocket connection.
            DefaultRunMode = Hub.Config.Discord.AsyncCommands ? RunMode.Async : RunMode.Sync,

            // There's a few more properties you can set,
            // for example, case-insensitive commands.
            CaseSensitiveCommands = false,
        });

        // Subscribe the logging handler to both the client and the CommandService.
        _client.Log += Log;
        _commands.Log += Log;

        // Setup your DI container.
        _services = ConfigureServices();

        _client.PresenceUpdated += Client_PresenceUpdated;

        // Subscribe to the BotStopped event
        runner.BotStopped += async (sender, e) => await HandleBotStop();
    }

    public void SetupEventListeners(DiscordSocketClient client)
    {
        client.Connected += Client_Connected;
        client.Disconnected += Client_Disconnected;
    }

    private async Task Client_Connected()
    {
        LogUtil.LogText("Client_Connected: Bot is connecting...");
        await AnnounceBotStatus("Online", EmbedColorOption.Green);
        LogUtil.LogText("Client_Connected: Connection handling completed.");
    }

    private async Task Client_Disconnected(Exception arg)
    {
        LogUtil.LogText($"Client_Disconnected: Bot is disconnecting... Exception: {arg.Message}");
        await AnnounceBotStatus("Offline", EmbedColorOption.Red);
        LogUtil.LogText("Client_Disconnected: Disconnection handling completed.");
    }

    private async Task UpdateBotStatusAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            bool noQueue = !Hub.Queues.Info.GetCanQueue();
            string customStatus;
            UserStatus statusColor;

            if (noQueue)
            {
                customStatus = "Not accepting trades...";
                statusColor = UserStatus.DoNotDisturb;
            }
            else
            {
                var currentTrade = Hub.Queues.Info.UsersInQueue.FirstOrDefault(x => x.Trade.IsProcessing);
                if (currentTrade != null)
                {
                    var tradeDetail = currentTrade.Trade;
                    if (tradeDetail.Type != PokeTradeType.Random)
                    {
                        var speciesName = GameInfo.GetStrings(1).Species[tradeDetail.TradeData.Species];
                        customStatus = $"🔄 {speciesName} to {tradeDetail.Trainer.TrainerName}";
                        statusColor = UserStatus.Online;
                    }
                    else
                    {
                        customStatus = "⏳ Waiting for trades";
                        statusColor = UserStatus.Idle;
                    }
                }
                else if (Hub.BotSync.Barrier != null && Hub.Ledy.Pool.Files.Count > 0)
                {
                    customStatus = "🔂 Distributing Pokémon";
                    statusColor = UserStatus.Online;
                }
                else
                {
                    customStatus = "Waiting for trades";
                    statusColor = UserStatus.Idle;
                }
            }

            await _client.SetGameAsync(customStatus, type: ActivityType.CustomStatus).ConfigureAwait(false);
            await _client.SetStatusAsync(statusColor).ConfigureAwait(false);
            await Task.Delay(3000, token); // Check and update every 3 seconds
        }
    }

    public async Task HandleBotStop()
    {
        _statusUpdateCancellationTokenSource.Cancel();
        await AnnounceBotStatus("Offline", EmbedColorOption.Red);
        LogUtil.LogText("HandleBotStop: Cleanup tasks completed.");
    }

    private readonly Dictionary<ulong, ulong> _announcementMessageIds = [];

    public async Task AnnounceBotStatus(string status, EmbedColorOption color)
    {
        // Check the BotEmbedStatus setting before proceeding
        if (!SysCordSettings.Settings.BotEmbedStatus)
            return;

        var botName = SysCordSettings.HubConfig.BotName;
        if (string.IsNullOrEmpty(botName))
            botName = "Bot";

        var fullStatusMessage = $"# {botName} is {status}!";

        var thumbnailUrl = status == "Online"
            ? "https://i.imgur.com/gOEp9BV.png"
            : "https://i.imgur.com/krI9XlI.png";

        var embed = new EmbedBuilder()
            .WithTitle($"{botName} Status")
            .WithDescription(fullStatusMessage)
            .WithColor(EmbedColorConverter.ToDiscordColor(color))
            .WithThumbnailUrl(thumbnailUrl)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        // Iterate over whitelisted channels and send the announcement
        foreach (var channelId in SysCordSettings.Manager.WhitelistedChannels.List.Select(channel => channel.ID))
        {
            IMessageChannel? channel = _client.GetChannel(channelId) as IMessageChannel;
            if (channel == null)
            {
                // If not found in cache, try fetching directly
                channel = await _client.Rest.GetChannelAsync(channelId) as IMessageChannel;
                if (channel == null)
                {
                    LogUtil.LogText($"AnnounceBotStatus: Failed to find channel with ID {channelId} even after direct fetch.");
                    continue;
                }
            }

            try
            {
                // Check if there's a previous announcement message in this channel
                if (_announcementMessageIds.TryGetValue(channelId, out ulong messageId))
                {
                    // Try to delete the previous announcement message
                    try
                    {
                        await channel.DeleteMessageAsync(messageId);
                    }
                    catch (Exception ex)
                    {
                        LogUtil.LogText($"AnnounceBotStatus: Exception when deleting previous message in channel {channelId}: {ex.Message}");
                    }
                }

                // Send the new announcement and store the message ID
                var message = await channel.SendMessageAsync(embed: embed);
                _announcementMessageIds[channelId] = message.Id;
                LogUtil.LogText($"AnnounceBotStatus: {fullStatusMessage} announced in channel {channelId}.");
            }
            catch (Exception ex)
            {
                LogUtil.LogText($"AnnounceBotStatus: Exception when sending message to channel {channelId}: {ex.Message}");
            }
        }
    }


    // If any services require the client, or the CommandService, or something else you keep on hand,
    // pass them as parameters into this method as needed.
    // If this method is getting pretty long, you can separate it out into another file using partials.
    private static ServiceProvider ConfigureServices()
    {
        var map = new ServiceCollection();//.AddSingleton(new SomeServiceClass());

        // When all your required services are in the collection, build the container.
        // Tip: There's an overload taking in a 'validateScopes' bool to make sure
        // you haven't made any mistakes in your dependency graph.
        return map.BuildServiceProvider();
    }

    // Example of a logging handler. This can be reused by add-ons
    // that ask for a Func<LogMessage, Task>.

    private static Task Log(LogMessage msg)
    {
        var text = $"[{msg.Severity,8}] {msg.Source}: {msg.Message} {msg.Exception}";
        Console.ForegroundColor = GetTextColor(msg.Severity);
        Console.WriteLine($"{DateTime.Now,-19} {text}");
        Console.ResetColor();

        LogUtil.LogText($"SysCord: {text}");

        return Task.CompletedTask;
    }

    private static ConsoleColor GetTextColor(LogSeverity sv) => sv switch
    {
        LogSeverity.Critical => ConsoleColor.Red,
        LogSeverity.Error => ConsoleColor.Red,

        LogSeverity.Warning => ConsoleColor.Yellow,
        LogSeverity.Info => ConsoleColor.White,

        LogSeverity.Verbose => ConsoleColor.DarkGray,
        LogSeverity.Debug => ConsoleColor.DarkGray,
        _ => Console.ForegroundColor,
    };

    public async Task MainAsync(string apiToken, CancellationToken token)
    {
        // Centralize the logic for commands into a separate method.
        await InitCommands().ConfigureAwait(false);

        // Setup event listeners for the Discord client.
        SetupEventListeners(_client);

        // Login and connect.
        await _client.LoginAsync(TokenType.Bot, apiToken).ConfigureAwait(false);
        await _client.StartAsync().ConfigureAwait(false);

        var app = await _client.GetApplicationInfoAsync().ConfigureAwait(false);
        Manager.Owner = app.Owner.Id;

        try
        {
            // Start the task to update the bot's status
            Task updateBotStatusTask = UpdateBotStatusAsync(_statusUpdateCancellationTokenSource.Token);

            // Wait for a cancellation request
            await Task.Delay(-1, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Handle the cancellation and perform cleanup tasks
            LogUtil.LogText("MainAsync: Bot is disconnecting due to cancellation...");
            await AnnounceBotStatus("Offline", EmbedColorOption.Red);
            LogUtil.LogText("MainAsync: Cleanup tasks completed.");
        }
        finally
        {
            // Disconnect the bot
            await _client.StopAsync();
        }
    }

    public async Task InitCommands()
    {
        var assembly = Assembly.GetExecutingAssembly();

        await _commands.AddModulesAsync(assembly, _services).ConfigureAwait(false);
        var genericTypes = assembly.DefinedTypes.Where(z => z.IsSubclassOf(typeof(ModuleBase<SocketCommandContext>)) && z.IsGenericType);
        foreach (var t in genericTypes)
        {
            var genModule = t.MakeGenericType(typeof(T));
            await _commands.AddModuleAsync(genModule, _services).ConfigureAwait(false);
        }
        var modules = _commands.Modules.ToList();

        var blacklist = Hub.Config.Discord.ModuleBlacklist
            .Replace("Module", "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(z => z.Trim()).ToList();

        foreach (var module in modules)
        {
            var name = module.Name;
            name = name.Replace("Module", "");
            var gen = name.IndexOf('`');
            if (gen != -1)
                name = name[..gen];
            if (blacklist.Any(z => z.Equals(name, StringComparison.OrdinalIgnoreCase)))
                await _commands.RemoveModuleAsync(module).ConfigureAwait(false);
        }

        // Subscribe a handler to see if a message invokes a command.
        _client.Ready += LoadLoggingAndEcho;
        _client.MessageReceived += HandleMessageAsync;
    }

    private async Task HandleMessageAsync(SocketMessage arg)
    {
        // Bail out if it's a System Message.
        if (arg is not SocketUserMessage msg)
            return;

        // Check if the message is from a server and if that server is blacklisted
        if (msg.Channel is SocketGuildChannel guildChannel)
        {
            if (Manager.BlacklistedServers.Contains(guildChannel.Guild.Id))
            {
                await guildChannel.Guild.LeaveAsync();
                return;
            }
        }

        // We don't want the bot to respond to itself or other bots.
        if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot)
            return;

        // Create a number to track where the prefix ends and the command begins
        int pos = 0;
        if (msg.HasStringPrefix(Hub.Config.Discord.CommandPrefix, ref pos))
        {
            bool handled = await TryHandleCommandAsync(msg, pos).ConfigureAwait(false);
            if (handled)
                return;
        }

        await TryHandleMessageAsync(msg).ConfigureAwait(false);
    }

    private async Task TryHandleMessageAsync(SocketMessage msg)
    {
        if (msg.Attachments.Count > 0)
        {
            var mgr = Manager;
            var cfg = mgr.Config;
            if (cfg.ConvertPKMToShowdownSet && (cfg.ConvertPKMReplyAnyChannel || mgr.CanUseCommandChannel(msg.Channel.Id)))
            {
                if (msg is SocketUserMessage userMessage)
                {
                    foreach (var att in msg.Attachments)
                        await msg.Channel.RepostPKMAsShowdownAsync(att, userMessage).ConfigureAwait(false);
                }
            }
        }
        // Replies for thanking or showing love to the bot.
        string thanksText = msg.Content.ToLower();
        string[] thanksWords = {
            "thank",
            "thx",
            "arigato",
            "thank you",
            "thanks",
            "appreciate",
            "much appreciated",
            "i love you",
            "awesome",
            "amazing",
            "the best"
        };

        if (thanksWords.Any(word => thanksText.Contains(word)))
        {
            var channel = msg.Channel;
            await channel.TriggerTypingAsync();
            await Task.Delay(1_500);

            var responses = new List<string>
    {
        "You're welcome! ❤️",
        "No problem at all!",
        "Anytime, glad to help!",
        "It's my pleasure! ❤️",
        "Not a problem! You're welcome!",
        "Always here to help!",
        "Glad I could assist!",
        "Happy to serve!",
        "Of course! You're welcome!",
        "Sure thing!",
        "You got it!",
        "No problemo.",
        "It's been an honor serving you.",
        "I deserve all of the credit.",
        "You're welcome, my friendly yet weak human.",
        "That's what I'm here for!"
    };

            var random = new Random();
            var randomIndex = random.Next(responses.Count);
            var finalResponse = responses[randomIndex];

            await msg.Channel.SendMessageAsync(finalResponse).ConfigureAwait(false);
            return;
        }
    }
    private Task Client_PresenceUpdated(SocketUser user, SocketPresence before, SocketPresence after)
    {
        return Task.CompletedTask;
    }

    private async Task<bool> TryHandleCommandAsync(SocketUserMessage msg, int pos)
        {
            // Create a Command Context.
            var context = new SocketCommandContext(_client, msg);

            // Check Permission
            var mgr = Manager;
            if (!mgr.CanUseCommandUser(msg.Author.Id))
            {
                await msg.Channel.SendMessageAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return true;
            }
            if (!mgr.CanUseCommandChannel(msg.Channel.Id) && msg.Author.Id != mgr.Owner)
            {
                if (Hub.Config.Discord.ReplyCannotUseCommandInChannel)
                    await msg.Channel.SendMessageAsync("You can't use that command here.").ConfigureAwait(false);
                return true;
            }

            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully).
            var guild = msg.Channel is SocketGuildChannel g ? g.Guild.Name : "Unknown Guild";
            await Log(new LogMessage(LogSeverity.Info, "Command", $"Executing command from {guild}#{msg.Channel.Name}:@{msg.Author.Username}. Content: {msg}")).ConfigureAwait(false);
            var result = await _commands.ExecuteAsync(context, pos, _services).ConfigureAwait(false);

            if (result.Error == CommandError.UnknownCommand)
                return false;

            // Uncomment the following lines if you want the bot
            // to send a message if it failed.
            // This does not catch errors from commands with 'RunMode.Async',
            // subscribe a handler for '_commands.CommandExecuted' to see those.
            if (!result.IsSuccess)
                await msg.Channel.SendMessageAsync(result.ErrorReason).ConfigureAwait(false);
            return true;
        }
        private async Task LoadLoggingAndEcho()
        {
            if (MessageChannelsLoaded)
                return;

            // Restore Echoes
            EchoModule.RestoreChannels(_client, Hub.Config.Discord);

            // Restore Logging
            LogModule.RestoreLogging(_client, Hub.Config.Discord);
            TradeStartModule<T>.RestoreTradeStarting(_client);

            // Don't let it load more than once in case of Discord hiccups.
            await Log(new LogMessage(LogSeverity.Info, "LoadLoggingAndEcho()", "Logging and Echo channels loaded!")).ConfigureAwait(false);
            MessageChannelsLoaded = true;
        }
    }
