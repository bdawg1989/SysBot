using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SysBot.Pokemon;

public class TradeSettings : IBotStateSettings, ICountSettings
{
    private const string CountStats = nameof(CountStats);
    private const string HOMELegality = nameof(HOMELegality);
    private const string TradeConfig = nameof(TradeConfig);
    private const string VGCPastesConfig = nameof(VGCPastesConfig);
    private const string AutoCorrectShowdownConfig = nameof(AutoCorrectShowdownConfig);
    private const string Miscellaneous = nameof(Miscellaneous);
    private const string RequestFolders = nameof(RequestFolders);
    private const string EmbedSettings = nameof(EmbedSettings);
    public override string ToString() => "Trade Configuration Settings";

    [Category(TradeConfig), Description("Settings related to Trade Configuration."), DisplayName("Trade Configuration"), Browsable(true)]
    public TradeSettingsCategory TradeConfiguration { get; set; } = new();

    [Category(VGCPastesConfig), Description("Settings related to VGCPastes Configuration."), DisplayName("VGC Pastes Configuration"), Browsable(true)]
    public VGCPastesCategory VGCPastesConfiguration { get; set; } = new();

    [Category(AutoCorrectShowdownConfig), Description("Settings related to Auto Correcting Showdown Sets."), DisplayName("Auto Correct Showdown Settings"), Browsable(true)]
    public AutoCorrectShowdownCategory AutoCorrectConfig { get; set; } = new();

    [Category(EmbedSettings), Description("Settings related to the Trade Embed in Discord."), DisplayName("Trade Embed Settings"), Browsable(true)]
    public TradeEmbedSettingsCategory TradeEmbedSettings { get; set; } = new();

    [Category(HOMELegality), Description("Settings related to HOME Legality."), DisplayName("HOME Legality Settings"), Browsable(true)]
    public HOMELegalitySettingsCategory HomeLegalitySettings { get; set; } = new();

    [Category(RequestFolders), Description("Settings related to Request Folders."), DisplayName("Request Folder Settings"), Browsable(true)]
    public RequestFolderSettingsCategory RequestFolderSettings { get; set; } = new();

    [Category(CountStats), Description("Settings related to Trade Count Statistics."), DisplayName("Trade Count Statistics Settings"), Browsable(true)]
    public CountStatsSettingsCategory CountStatsSettings { get; set; } = new();


    [Category(TradeConfig), TypeConverter(typeof(CategoryConverter<TradeSettingsCategory>))]
    public class TradeSettingsCategory
    {
        public override string ToString() => "Trade Configuration Settings";

        [Category(TradeConfig), Description("Minimum Link Code."), DisplayName("Minimum Trade Link Code")]
        public int MinTradeCode { get; set; } = 0;

        [Category(TradeConfig), Description("Maximum Link Code."), DisplayName("Maximum Trade Link Code")]
        public int MaxTradeCode { get; set; } = 9999_9999;

        [Category(TradeConfig), Description("If set to True, Discord Users trade code will be stored and used repeatedly without changing."), DisplayName("Store and Reuse Trade Codes")]
        public bool StoreTradeCodes { get; set; } = false;

        [Category(TradeConfig), Description("Time to wait for a trade partner in seconds."), DisplayName("Trade Partner Wait Time (seconds)")]
        public int TradeWaitTime { get; set; } = 30;

        [Category(TradeConfig), Description("Max amount of time in seconds pressing A to wait for a trade to process."), DisplayName("Maximum Trade Confirmation Time (seconds)")]
        public int MaxTradeConfirmTime { get; set; } = 25;

        [Category(TradeConfig), Description("Select default species for \"ItemTrade\", if configured."), DisplayName("Default Species for Item Trades")]
        public Species ItemTradeSpecies { get; set; } = Species.None;

        [Category(TradeConfig), Description("Default held item to send if none is specified."), DisplayName("Default Held Item for Trades")]
        public HeldItem DefaultHeldItem { get; set; } = HeldItem.None;

        [Category(TradeConfig), Description("If set to True, each valid Pokemon will come with all suggested Relearnable Moves without the need for a batch command."), DisplayName("Suggest Relearnable Moves by Default")]
        public bool SuggestRelearnMoves { get; set; } = true;

        [Category(TradeConfig), Description("Toggle to allow or disallow batch trades."), DisplayName("Allow Batch Trades")]
        public bool AllowBatchTrades { get; set; } = true;

        [Category(TradeConfig), Description("Maximum Pokémon of a single trade. Batch mode will be closed if this configuration is less than 1"), DisplayName("Maximum Pokémon per Trade")]
        public int MaxPkmsPerTrade { get; set; } = 1;

        [Category(TradeConfig), Description("Dump Trade: Dumping routine will stop after a maximum number of dumps from a single user."), DisplayName("Maximum Dumps per Trade")]
        public int MaxDumpsPerTrade { get; set; } = 20;

        [Category(TradeConfig), Description("Dump Trade: Dumping routine will stop after spending x seconds in trade."), DisplayName("Maximum Dump Trade Time (seconds)")]
        public int MaxDumpTradeTime { get; set; } = 180;

        [Category(TradeConfig), Description("Dump Trade: If enabled, Dumping routine will output legality check information to the user."), DisplayName("Dump Trade Legality Check")]
        public bool DumpTradeLegalityCheck { get; set; } = true;

        [Category(TradeConfig), Description("LGPE Setting.")]
        public int TradeAnimationMaxDelaySeconds = 25;

        public enum HeldItem
        {
            None = 0,
            AbilityPatch = 1606,
            RareCandy = 50,
            AbilityCapsule = 645,
            BottleCap = 795,
            expCandyL = 1127,
            expCandyXL = 1128,
            MasterBall = 1,
            Nugget = 92,
            BigPearl = 89,
            GoldBottleCap = 796,
            ppUp = 51,
            ppMax = 53,
            FreshStartMochi = 2479,
        }
    }

    [Category(nameof(AutoCorrectShowdownConfig)), TypeConverter(typeof(CategoryConverter<AutoCorrectShowdownCategory>))]
    public class AutoCorrectShowdownCategory
    {
        public override string ToString() => "Auto Correct Showdown Settings";

        private bool _autoCorrectEmbedIndicator = true;
        [Category(nameof(AutoCorrectShowdownCategory)), Description("If set to True, we will put an indicator on Trade Embeds showing a trade was Auto-Corrected."), DisplayName("Show Auto-Correct Indicator")]
        public bool AutoCorrectEmbedIndicator
        {
            get => EnableAutoCorrect && _autoCorrectEmbedIndicator;
            set => _autoCorrectEmbedIndicator = value;
        }

        [Category(nameof(AutoCorrectShowdownCategory)), Description("If set to True, each failed showdown set will go through auto correction."), DisplayName("Enable Auto Correct")]
        public bool EnableAutoCorrect { get; set; } = true;

        [Category(nameof(AutoCorrectShowdownCategory)), Description("If set to True, auto correction will correct wrong species and form."), DisplayName("Auto Correct Species and Form")]
        public bool AutoCorrectSpeciesAndForm { get; set; } = true;

        [Category(nameof(AutoCorrectShowdownCategory)), Description("If set to True, auto correction will correct wrong held item."), DisplayName("Auto Correct Held Item")]
        public bool AutoCorrectHeldItem { get; set; } = true;

        [Category(nameof(AutoCorrectShowdownCategory)), Description("If set to True, auto correction will correct wrong nature."), DisplayName("Auto Correct Nature")]
        public bool AutoCorrectNature { get; set; } = true;

        [Category(nameof(AutoCorrectShowdownCategory)), Description("If set to True, auto correction will correct wrong ability."), DisplayName("Auto Correct Ability")]
        public bool AutoCorrectAbility { get; set; } = true;

        [Category(nameof(AutoCorrectShowdownCategory)), Description("If set to True, auto correction will correct wrong level."), DisplayName("Auto Correct Level")]
        public bool AutoCorrectLevel { get; set; } = true;

        private bool _autoCorrectGender = true;
        [Category(nameof(AutoCorrectShowdownCategory)), Description("If set to True, auto correction will correct wrong gender."), DisplayName("Auto Correct Gender")]
        public bool AutoCorrectGender
        {
            get => EnableAutoCorrect && _autoCorrectGender;
            set => _autoCorrectGender = value;
        }



        [Category(nameof(AutoCorrectShowdownCategory)), Description("If set to True, auto correction will correct wrong moves and learnset."), DisplayName("Auto Correct Moves/Learnset")]
        public bool AutoCorrectMovesLearnset { get; set; } = true;

        [Category(nameof(AutoCorrectShowdownCategory)), Description("If set to True, auto correction will correct wrong EVs."), DisplayName("Auto Correct EVs")]
        public bool AutoCorrectEVs { get; set; } = true;

        [Category(nameof(AutoCorrectShowdownCategory)), Description("If set to True, auto correction will correct wrong IVs."), DisplayName("Auto Correct IVs")]
        public bool AutoCorrectIVs { get; set; } = true;
    }

    [Category(EmbedSettings), TypeConverter(typeof(CategoryConverter<TradeEmbedSettingsCategory>))]
    public class TradeEmbedSettingsCategory
    {
        public override string ToString() => "Trade Embed Configuration Settings";


        [Category(EmbedSettings), Description("If true, will show beautiful embeds in Discord. False will show simple, yet stylish text."), DisplayName("Use Embeds")]
        public bool UseEmbeds { get; set; } = true;

        [Category(EmbedSettings), Description("Will show Move Type Icons next to moves in trade embed (Discord only). Requires user to upload the emojis to their server."), DisplayName("Show Move Type Emojis")]
        public bool MoveTypeEmojis { get; set; } = true;

        [Category(EmbedSettings), Description("Will show Gender Icons in trade embed (Discord only)."), DisplayName("Show Gender Icons")]
        public bool GenderEmojis { get; set; } = true;

        [Category(EmbedSettings), Description("Will show Mystery Gift Emoji in trade embed if in Cherish Ball (Discord only)."), DisplayName("Show Mystery Gift Icons")]
        public bool MysteryGiftEmoji { get; set; } = true;

        [Category(EmbedSettings), Description("The emoji information for displaying the Alpha Mark."), DisplayName("Alpha Mark Emoji")]
        public bool AlphaMarkEmoji { get; set; } = true;

        [Category(EmbedSettings), Description("The emoji information for displaying the Mightiest Mark."), DisplayName("Mightiest Mark Emoji")]
        public bool MightiestMarkEmoji { get; set; } = true;

        [Category(EmbedSettings), Description("Will show Scale in trade embed (SV & Discord only). Requires user to upload the emojis to their server."), DisplayName("Show Scale")]
        public bool ShowScale { get; set; } = true;

        [Category(EmbedSettings), Description("Will show Tera Type in trade embed (SV & Discord only)."), DisplayName("Show Tera Type")]
        public bool ShowTeraType { get; set; } = true;

        [Category(EmbedSettings), Description("Will show Tera Type Emojis in the trade embed (SV & Discord only)."), DisplayName("Show Tera Type Emojis")]
        public bool ShowTeraTypeEmoji { get; set; } = true;

        [Category(EmbedSettings), Description("Will show Level in trade embed (Discord only)."), DisplayName("Show Level")]
        public bool ShowLevel { get; set; } = true;

        [Category(EmbedSettings), Description("Will show Ability in trade embed (Discord only)."), DisplayName("Show Ability")]
        public bool ShowAbility { get; set; } = true;

        [Category(EmbedSettings), Description("Will show Nature in trade embed (Discord only)."), DisplayName("Show Nature")]
        public bool ShowNature { get; set; } = true;

        [Category(EmbedSettings), Description("Will show IVs in trade embed (Discord only)."), DisplayName("Show IVs")]
        public bool ShowIVs { get; set; } = true;

        [Category(EmbedSettings), Description("Will show EVs in trade embed (Discord only)."), DisplayName("Show EVs")]
        public bool ShowEVs { get; set; } = true;

        [Category(EmbedSettings), Description("Will show OT in trade embed (Discord only)."), DisplayName("Show OT")]
        public bool ShowOT { get; set; } = true;

        [Category(EmbedSettings), Description("Will show TID in trade embed (Discord only)."), DisplayName("Show TID")]
        public bool ShowTID { get; set; } = true;

        [Category(EmbedSettings), Description("Will show SID in trade embed (Discord only)."), DisplayName("Show SID")]
        public bool ShowSID { get; set; } = true;

        [Category(EmbedSettings), Description("Will show Met Level in trade embed (Discord only)."), DisplayName("Show Met Level")]
        public bool ShowMetLevel { get; set; } = false;

        [Category(EmbedSettings), Description("Will show Met Level in trade embed (Discord only)."), DisplayName("Show Fateful Encounter")]
        public bool ShowFatefulEncounter { get; set; } = false;

        [Category(EmbedSettings), Description("Will show if Pokémon came from an egg in trade embed (Discord only)."), DisplayName("Show WasEgg")]
        public bool ShowWasEgg { get; set; } = false;

        [Category(EmbedSettings), Description("Will show MetDate in trade embed (Discord only)."), DisplayName("Show Met Date")]
        public bool ShowMetDate { get; set; } = false;

        [Category(EmbedSettings), Description("Will show Met Date in trade embed (Discord only)."), DisplayName("Show Language")]
        public bool ShowLanguage { get; set; } = false;
    }


    [Category(VGCPastesConfig), TypeConverter(typeof(CategoryConverter<VGCPastesCategory>))]
    public class VGCPastesCategory
    {
        public override string ToString() => "VGCPastes Configuration Settings";

        [Category(VGCPastesConfig), Description("Allow users to request and generate teams using the VGCPastes Spreadsheet."), DisplayName("Allow VGC Paste Requests")]
        public bool AllowRequests { get; set; } = true;

        [Category(VGCPastesConfig), Description("GID of Spreadsheet tab you would like to pull from. Hint: https://docs.google.com/spreadsheets/d/ID/gid=1837599752"), DisplayName("GID of Spreadsheet Tab")]
        public int GID { get; set; } = 1837599752; // Reg F Tab

    }

    [Category(HOMELegality), TypeConverter(typeof(CategoryConverter<HOMELegalitySettingsCategory>))]
    public class HOMELegalitySettingsCategory
    {
        public override string ToString() => "HOME Legality Settings";

        [Category(HOMELegality), Description("Prevents trading Pokémon that require a HOME Tracker, even if the file has one already."), DisplayName("Disallow Non-Native Pokémon")]
        public bool DisallowNonNatives { get; set; } = false;

        [Category(HOMELegality), Description("Prevents trading Pokémon that already have a HOME Tracker."), DisplayName("Disallow Home Tracked Pokémon")]
        public bool DisallowTracked { get; set; } = false;
    }

    [Category(RequestFolders), TypeConverter(typeof(CategoryConverter<RequestFolderSettingsCategory>))]
    public class RequestFolderSettingsCategory
    {
        public override string ToString() => "Request Folders Settings";

        [Category("RequestFolders"), Description("Path to your Events Folder. Create a new folder called 'events' and copy the path here."), DisplayName("Events Folder Path")]
        public string EventsFolder { get; set; } = string.Empty;

        [Category("RequestFolders"), Description("Path to your BattleReady Folder. Create a new folder called 'battleready' and copy the path here."), DisplayName("Battle-Ready Folder Path")]
        public string BattleReadyPKMFolder { get; set; } = string.Empty;

        [Category("RequestFolders"), Description("Path to your HOME-eady Folder. Create a new folder called 'homeready' and copy the path here."), DisplayName("HOME-Ready Folder Path")]
        public string HOMEReadyPKMFolder { get; set; } = string.Empty;

    }

    [Category(Miscellaneous), Description("Miscellaneous Settings"), DisplayName("Miscellaneous")]
    public bool ScreenOff { get; set; } = false;


    /// <summary>
    /// Gets a random trade code based on the range settings.
    /// </summary>
    public int GetRandomTradeCode() => Util.Rand.Next(TradeConfiguration.MinTradeCode, TradeConfiguration.MaxTradeCode + 1);
    public static List<Pictocodes> GetRandomLGTradeCode(bool randomtrade = false)
    {
        var lgcode = new List<Pictocodes>();
        if (randomtrade)
        {
            for (int i = 0; i <= 2; i++)
            {
                // code.Add((pictocodes)Util.Rand.Next(10));
                lgcode.Add(Pictocodes.Pikachu);

            }
        }
        else
        {
            for (int i = 0; i <= 2; i++)
            {
                lgcode.Add((Pictocodes)Util.Rand.Next(10));
                // code.Add(pictocodes.Pikachu);

            }
        }
        return lgcode;
    }


    [Category(CountStats), TypeConverter(typeof(CategoryConverter<CountStatsSettingsCategory>))]
    public class CountStatsSettingsCategory
    {
        public override string ToString() => "Trade Count Statistics";

        private int _completedSurprise;
        private int _completedDistribution;
        private int _completedTrades;
        private int _completedSeedChecks;
        private int _completedClones;
        private int _completedDumps;
        private int _completedFixOTs;

        [Category(CountStats), Description("Completed Surprise Trades")]
        public int CompletedSurprise
        {
            get => _completedSurprise;
            set => _completedSurprise = value;
        }

        [Category(  ), Description("Completed Link Trades (Distribution)")]
        public int CompletedDistribution
        {
            get => _completedDistribution;
            set => _completedDistribution = value;
        }

        [Category(CountStats), Description("Completed Link Trades (Specific User)")]
        public int CompletedTrades
        {
            get => _completedTrades;
            set => _completedTrades = value;
        }

        [Category(CountStats), Description("Completed FixOT Trades (Specific User)")]
        public int CompletedFixOTs
        {
            get => _completedFixOTs;
            set => _completedFixOTs = value;
        }

        [Browsable(false)]
        [Category(CountStats), Description("Completed Seed Check Trades")]
        public int CompletedSeedChecks
        {
            get => _completedSeedChecks;
            set => _completedSeedChecks = value;
        }

        [Category(CountStats), Description("Completed Clone Trades (Specific User)")]
        public int CompletedClones
        {
            get => _completedClones;
            set => _completedClones = value;
        }

        [Category(CountStats), Description("Completed Dump Trades (Specific User)")]
        public int CompletedDumps
        {
            get => _completedDumps;
            set => _completedDumps = value;
        }

        [Category(CountStats), Description("When enabled, the counts will be emitted when a status check is requested.")]
        public bool EmitCountsOnStatusCheck { get; set; }

        public void AddCompletedTrade() => Interlocked.Increment(ref _completedTrades);
        public void AddCompletedSeedCheck() => Interlocked.Increment(ref _completedSeedChecks);
        public void AddCompletedSurprise() => Interlocked.Increment(ref _completedSurprise);
        public void AddCompletedDistribution() => Interlocked.Increment(ref _completedDistribution);
        public void AddCompletedDumps() => Interlocked.Increment(ref _completedDumps);
        public void AddCompletedClones() => Interlocked.Increment(ref _completedClones);
        public void AddCompletedFixOTs() => Interlocked.Increment(ref _completedFixOTs);

        public IEnumerable<string> GetNonZeroCounts()
        {
            if (!EmitCountsOnStatusCheck)
                yield break;
            if (CompletedSeedChecks != 0)
                yield return $"Seed Check Trades: {CompletedSeedChecks}";
            if (CompletedClones != 0)
                yield return $"Clone Trades: {CompletedClones}";
            if (CompletedDumps != 0)
                yield return $"Dump Trades: {CompletedDumps}";
            if (CompletedTrades != 0)
                yield return $"Link Trades: {CompletedTrades}";
            if (CompletedDistribution != 0)
                yield return $"Distribution Trades: {CompletedDistribution}";
            if (CompletedFixOTs != 0)
                yield return $"FixOT Trades: {CompletedFixOTs}";
            if (CompletedSurprise != 0)
                yield return $"Surprise Trades: {CompletedSurprise}";
        }
    }

    public bool EmitCountsOnStatusCheck
    {
        get => CountStatsSettings.EmitCountsOnStatusCheck;
        set => CountStatsSettings.EmitCountsOnStatusCheck = value;
    }

    public IEnumerable<string> GetNonZeroCounts()
    {
        // Delegating the call to CountStatsSettingsCategory
        return CountStatsSettings.GetNonZeroCounts();
    }

    public class CategoryConverter<T> : TypeConverter
    {
        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes) => TypeDescriptor.GetProperties(typeof(T));

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType != typeof(string) && base.CanConvertTo(context, destinationType);
    }
}
