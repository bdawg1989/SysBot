using PKHeX.Core;
using SysBot.Pokemon.Discord;
using SysBot.Pokemon.WinForms;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Pokemon.Bilibili;

namespace SysBot.Pokemon;

/// <summary>
/// Bot Environment implementation with Integrations added.
/// </summary>
public class PokeBotRunnerImpl<T> : PokeBotRunner<T> where T : PKM, new()
{
    public PokeBotRunnerImpl(PokeTradeHub<T> hub, BotFactory<T> fac) : base(hub, fac) { }
    public PokeBotRunnerImpl(PokeTradeHubConfig config, BotFactory<T> fac) : base(config, fac) { }

    private BilibiliLiveBot<T>? Bilibili;

    protected override void AddIntegrations()
    {
        AddDiscordBot(Hub.Config.Discord.Token);
        AddBilibiliBot(Hub.Config.Bilibili);
    }

    private void AddDiscordBot(string apiToken)
    {
        if (string.IsNullOrWhiteSpace(apiToken))
            return;
        var bot = new SysCord<T>(this);
        Task.Run(() => bot.MainAsync(apiToken, CancellationToken.None));
    }

    private void AddBilibiliBot(BilibiliSettings config)
    {
        if (string.IsNullOrWhiteSpace(config.LogUrl) || config.RoomId == 0) return;
        if (Bilibili != null) return;
        Bilibili = new BilibiliLiveBot<T>(config, Hub);
    }
}
