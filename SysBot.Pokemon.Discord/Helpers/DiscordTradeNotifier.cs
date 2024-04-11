using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using PKHeX.Drawing.PokeSprite;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using Color = Discord.Color;

namespace SysBot.Pokemon.Discord;

public class DiscordTradeNotifier<T> : IPokeTradeNotifier<T>
    where T : PKM, new()
{
    private T Data { get; }
    private PokeTradeTrainerInfo Info { get; }
    private int Code { get; }
    private List<Pictocodes> LGCode { get; }
    private SocketUser Trader { get; }
    private int BatchTradeNumber { get; }
    private int TotalBatchTrades { get; }
    private bool IsMysteryEgg { get; }

    public DiscordTradeNotifier(T data, PokeTradeTrainerInfo info, int code, SocketUser trader, int batchTradeNumber, int totalBatchTrades, bool isMysteryEgg, List<Pictocodes> lgcode)
    {
        Data = data;
        Info = info;
        Code = code;
        Trader = trader;
        BatchTradeNumber = batchTradeNumber;
        TotalBatchTrades = totalBatchTrades;
        IsMysteryEgg = isMysteryEgg;
        LGCode = lgcode;
    }

    public Action<PokeRoutineExecutor<T>>? OnFinish { private get; set; }
    public readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;

    public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        if (Data is PK9)
        {
            var batchInfo = TotalBatchTrades > 1 ? $" (Trade {BatchTradeNumber} of {TotalBatchTrades})" : "";
            var receive = Data.Species == 0 ? string.Empty : $" {Data.Nickname}";
            var message = $"**Pokémon:** {receive}{batchInfo}\n**Trade Code:** {Code:0000 0000}\n*Your trade will begin in a moment*";

            if (TotalBatchTrades > 1 && BatchTradeNumber == 1)
            {
                message += "\n*Please stay in the trade until all batch trades are completed*";
            }

            string imageUrl = "https://i.imgur.com/rixGxsH.gif";
            var embed = new EmbedBuilder()
                .WithTitle("Initializing Bot...")
                .WithDescription(message)
                .WithColor(Color.Green)
                .WithThumbnailUrl(imageUrl)
                .Build();

            Trader.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }
        else if (Data is PB7)
        {
            var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
            var (thefile, lgcodeembed) = CreateLGLinkCodeSpriteEmbed(LGCode);
            Trader.SendFileAsync(thefile, $"**Pokémon:** {receive}\n**Trade Code:**", embed: lgcodeembed).ConfigureAwait(false);
        }
        else
        {
            var batchInfo = TotalBatchTrades > 1 ? $" (Trade {BatchTradeNumber} of {TotalBatchTrades})" : "";
            var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
            var message = $"**Pokémon:** {receive}{batchInfo}\n**Trade Code:** {Code:0000 0000}";
            Trader.SendMessageAsync(message).ConfigureAwait(false);
        }
    }

    public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        var batchInfo = TotalBatchTrades > 1 ? $" for batch trade (Trade {BatchTradeNumber} of {TotalBatchTrades})" : "";
        var name = Info.TrainerName;
        var trainer = string.IsNullOrEmpty(name) ? string.Empty : $" {name}";
        string message;

        if (Data is PB7 && LGCode != null && LGCode.Count != 0)
        {
            message = $"**Waiting For:** {trainer}{batchInfo}\n**My IGN:** {routine.InGameName}";
        }
        else
        {
            if (TotalBatchTrades > 1 && BatchTradeNumber > 1)
            {
                var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
                message = $"**Pokémon:** {receive} (Trade {BatchTradeNumber} of {TotalBatchTrades}) \n**Select the Pokémon you wish to trade!**";
            }
            else
            {
                message = $"**Waiting For:** {trainer}{batchInfo}\n**My IGN:** {routine.InGameName}\n**Trade Code:** {Code:0000 0000\n*Please insert the Trade Code now.*}";
            }
        }

        string imageUrl = "https://i.imgur.com/WBJ05TJ.gif";
        var embed = new EmbedBuilder()
            .WithTitle("Now Searching...")
            .WithDescription(message)
            .WithColor(Color.DarkGreen)
            .WithThumbnailUrl(imageUrl)
            .Build();

        Trader.SendMessageAsync(embed: embed).ConfigureAwait(false);
    }


    public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
    {
        OnFinish?.Invoke(routine);
        string imageUrl = "https://i.imgur.com/KnpoU86.gif";
        var embed = new EmbedBuilder()
            .WithTitle("Uh-Oh...")
            .WithDescription($"*Sorry, but there was an error*\n**Reason:** {msg}")
            .WithColor(Color.DarkRed)
            .WithThumbnailUrl(imageUrl)
            .Build();

        Trader.SendMessageAsync(embed: embed).ConfigureAwait(false);
    }

    public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
    {
        OnFinish?.Invoke(routine);
        var tradedToUser = Data.Species;
        string imageUrl = "https://i.imgur.com/9x9wj0t.gif";
        var message = tradedToUser != 0 ? (IsMysteryEgg ? "*Enjoy your Mystery Egg!*" : $"*Enjoy your {(Species)tradedToUser}!*") : "*Trade finished!*";


        var embed = new EmbedBuilder()
            .WithTitle("Trade Finished!")
            .WithDescription(message)
            .WithColor(Color.DarkPurple)
            .WithThumbnailUrl(imageUrl)
            .Build();

        Trader.SendMessageAsync(embed: embed).ConfigureAwait(false);
        if (result.Species != 0 && Hub.Config.Discord.ReturnPKMs)
            Trader.SendPKMAsync(result, "Here's what you traded me!").ConfigureAwait(false);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
    {
        var embed = new EmbedBuilder()
            .WithDescription(message)
            .WithColor(Color.Blue)
            .Build();

        Trader.SendMessageAsync(embed: embed).ConfigureAwait(false);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
    {
        if (message.ExtraInfo is SeedSearchResult r)
        {
            SendNotificationZ3(r);
            return;
        }

        var msg = message.Summary;
        if (message.Details.Count > 0)
            msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));

        var embed = new EmbedBuilder()
            .WithDescription(msg)
            .WithColor(Color.Blue)
            .Build();

        Trader.SendMessageAsync(embed: embed).ConfigureAwait(false);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
    {
        if (result.Species != 0 && (Hub.Config.Discord.ReturnPKMs || info.Type == PokeTradeType.Dump))
            Trader.SendPKMAsync(result, message).ConfigureAwait(false);
    }

    private void SendNotificationZ3(SeedSearchResult r)
    {
        var lines = r.ToString();

        var embed = new EmbedBuilder()
            .WithTitle($"Seed: {r.Seed:X16}")
            .WithDescription(lines)
            .WithColor(Color.LighterGrey)
            .Build();

        Trader.SendMessageAsync(embed: embed).ConfigureAwait(false);
    }


    public static (string, Embed) CreateLGLinkCodeSpriteEmbed(List<Pictocodes> lgcode)
    {
        int codecount = 0;
        List<System.Drawing.Image> spritearray = new();
        foreach (Pictocodes cd in lgcode)
        {
            var showdown = new ShowdownSet(cd.ToString());
            var sav = SaveUtil.GetBlankSAV(EntityContext.Gen7b, "pip");
            PKM pk = sav.GetLegalFromSet(showdown).Created;
            System.Drawing.Image png = pk.Sprite();
            var destRect = new Rectangle(-40, -65, 137, 130);
            var destImage = new Bitmap(137, 130);

            destImage.SetResolution(png.HorizontalResolution, png.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.DrawImage(png, destRect, 0, 0, png.Width, png.Height, GraphicsUnit.Pixel);

            }
            png = destImage;
            spritearray.Add(png);
            codecount++;
        }
        int outputImageWidth = spritearray[0].Width + 20;

        int outputImageHeight = spritearray[0].Height - 65;

        Bitmap outputImage = new Bitmap(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(outputImage))
        {
            graphics.DrawImage(spritearray[0], new Rectangle(0, 0, spritearray[0].Width, spritearray[0].Height),
                new Rectangle(new Point(), spritearray[0].Size), GraphicsUnit.Pixel);
            graphics.DrawImage(spritearray[1], new Rectangle(50, 0, spritearray[1].Width, spritearray[1].Height),
                new Rectangle(new Point(), spritearray[1].Size), GraphicsUnit.Pixel);
            graphics.DrawImage(spritearray[2], new Rectangle(100, 0, spritearray[2].Width, spritearray[2].Height),
                new Rectangle(new Point(), spritearray[2].Size), GraphicsUnit.Pixel);
        }
        System.Drawing.Image finalembedpic = outputImage;
        var filename = $"{System.IO.Directory.GetCurrentDirectory()}//finalcode.png";
        finalembedpic.Save(filename);
        filename = System.IO.Path.GetFileName($"{System.IO.Directory.GetCurrentDirectory()}//finalcode.png");
        Embed returnembed = new EmbedBuilder().WithTitle($"{lgcode[0]}, {lgcode[1]}, {lgcode[2]}").WithImageUrl($"attachment://{filename}").Build();
        return (filename, returnembed);
    }
}
