using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

/// <summary>
/// Discord command module for generating and trading mystery eggs
/// </summary>
/// <typeparam name="T">Type of Pokémon (PK8, PB8, PK9, etc.)</typeparam>
public class MysteryEggModule<T>(IServiceProvider? services = null) : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
    private static readonly Random Random = new();

    /// <summary>
    /// Generates and trades a mystery egg to the user
    /// </summary>
    [Command("mysteryegg")]
    [Alias("me")]
    [Summary("Trades an egg generated from a random Pokémon.")]
    public async Task TradeMysteryEggAsync()
    {
        var userID = Context.User.Id;
        if (Info.IsUserInQueue(userID))
        {
            await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
            return;
        }
        var code = Info.GetRandomTradeCode(userID);

        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessMysteryEggTradeAsync(code).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(MysteryEggModule<T>));
                await ReplyAsync("An error occurred while processing the request.").ConfigureAwait(false);
            }
        });
    }

    /// <summary>
    /// Generates a legal mystery egg with perfect IVs and shiny
    /// </summary>
    /// <param name="maxAttempts">Maximum number of generation attempts</param>
    /// <returns>Legal egg Pokémon or null if generation failed</returns>
    public static T? GenerateLegalMysteryEgg(int maxAttempts = 10)
    {
        var gameVersion = GetGameVersion();

        if (gameVersion == GameVersion.PLA)
            return null;

        var breedableSpecies = GetBreedableSpecies(gameVersion);

        if (breedableSpecies.Count == 0)
            return null;

        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();

        var criteria = EncounterCriteria.Unrestricted with
        {
            IV_HP = 31,
            IV_ATK = 31,
            IV_DEF = 31,
            IV_SPA = 31,
            IV_SPD = 31,
            IV_SPE = 31,
            Shiny = Shiny.Always,
            Ability = AbilityPermission.OnlyHidden,
        };

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var speciesId = breedableSpecies[Random.Next(breedableSpecies.Count)];

            IEncounterEgg? encounter = gameVersion switch
            {
                GameVersion.SWSH => new EncounterEgg8((ushort)speciesId, 0, gameVersion),
                GameVersion.BDSP => new EncounterEgg8b((ushort)speciesId, 0, gameVersion),
                GameVersion.SV => new EncounterEgg9((ushort)speciesId, 0, gameVersion),
                _ => null
            };

            if (encounter is null)
                continue;

            var basePk = encounter.ConvertToPKM(sav, criteria);
            if (basePk is not T validPk)
                continue;

            ApplyEggProperties(validPk);
            validPk.MaximizeFriendship();
            validPk.RefreshChecksum();

            var la = new LegalityAnalysis(validPk);
            if (la.Valid)
                return validPk;
        }

        return null;
    }

    private async Task ProcessMysteryEggTradeAsync(int code)
    {
        var mysteryEgg = GenerateLegalMysteryEgg(10);
        if (mysteryEgg is null)
        {
            await ReplyAsync("Failed to generate a legal mystery egg. Please try again later.").ConfigureAwait(false);
            return;
        }

        var sig = Context.User.GetFavor();
        await AddTradeToQueueAsync(code, Context.User.Username, mysteryEgg, sig, Context.User, isMysteryEgg: true).ConfigureAwait(false);

        if (Context.Message is IUserMessage userMessage)
            await DeleteMessageAfterDelay(userMessage, 2000).ConfigureAwait(false);
    }

    private static async Task DeleteMessageAfterDelay(IUserMessage message, int delayMilliseconds)
    {
        await Task.Delay(delayMilliseconds).ConfigureAwait(false);
        await message.DeleteAsync().ConfigureAwait(false);
    }

    private static void ApplyEggProperties(PKM pk)
    {
        pk.IsNicknamed = true;
        pk.Nickname = pk.Language switch
        {
            1 => "タマゴ",
            3 => "Œuf",
            4 => "Uovo",
            5 => "Ei",
            7 => "Huevo",
            8 => "알",
            9 or 10 => "蛋",
            _ => "Egg",
        };

        pk.IsEgg = true;

        pk.EggLocation = pk switch
        {
            PB8 => 60010,
            PK9 => 30023,
            _ => 60002,
        };

        pk.MetLocation = pk switch
        {
            PB8 => 65535,
            PK9 => 0,
            _ => 30002,
        };

        pk.MetDate = DateOnly.FromDateTime(DateTime.Now);
        pk.EggMetDate = pk.MetDate;
        pk.HeldItem = 0;
        pk.CurrentLevel = 1;
        pk.EXP = 0;
        pk.MetLevel = 1;
        pk.CurrentHandler = 0;
        pk.OriginalTrainerFriendship = 1;
        pk.HandlingTrainerName = "";
        ClearHandlingTrainerMemory(pk);
        pk.HandlingTrainerFriendship = 0;
        pk.ClearMemories();
        pk.StatNature = pk.Nature;
        pk.SetEVs([0, 0, 0, 0, 0, 0]);

        switch (pk)
        {
            case PK8 pk8:
                pk8.HandlingTrainerLanguage = 0;
                pk8.HandlingTrainerGender = 0;
                pk8.HandlingTrainerMemory = 0;
                pk8.HandlingTrainerMemoryFeeling = 0;
                pk8.HandlingTrainerMemoryIntensity = 0;
                pk8.DynamaxLevel = pk8.GetSuggestedDynamaxLevel(pk8, 0);
                break;

            case PB8 pb8:
                pb8.HandlingTrainerLanguage = 0;
                pb8.HandlingTrainerGender = 0;
                pb8.HandlingTrainerMemory = 0;
                pb8.HandlingTrainerMemoryFeeling = 0;
                pb8.HandlingTrainerMemoryIntensity = 0;
                pb8.DynamaxLevel = pb8.GetSuggestedDynamaxLevel(pb8, 0);
                ClearNicknameTrash(pk);
                break;

            case PK9 pk9:
                pk9.HandlingTrainerLanguage = 0;
                pk9.HandlingTrainerGender = 0;
                pk9.HandlingTrainerMemory = 0;
                pk9.HandlingTrainerMemoryFeeling = 0;
                pk9.HandlingTrainerMemoryIntensity = 0;
                pk9.ObedienceLevel = 1;
                pk9.Version = 0;
                pk9.BattleVersion = 0;
                pk9.TeraTypeOverride = (MoveType)19;
                break;
        }

        pk.Move1_PPUps = pk.Move2_PPUps = pk.Move3_PPUps = pk.Move4_PPUps = 0;
        pk.SetMaximumPPCurrent(pk.Moves);
        pk.SetSuggestedHyperTrainingData();
    }

    private static void ClearHandlingTrainerMemory(PKM pk)
    {
        if (pk is IMemoryOT memory)
            memory.ClearMemoriesOT();
    }

    private static void ClearNicknameTrash(PKM pokemon)
    {
        switch (pokemon)
        {
            case PK9 pk9:
                ClearTrash(pk9.NicknameTrash, pk9.Nickname);
                break;
            case PA8 pa8:
                ClearTrash(pa8.NicknameTrash, pa8.Nickname);
                break;
            case PB8 pb8:
                ClearTrash(pb8.NicknameTrash, pb8.Nickname);
                break;
            case PB7 pb7:
                ClearTrash(pb7.NicknameTrash, pb7.Nickname);
                break;
            case PK8 pk8:
                ClearTrash(pk8.NicknameTrash, pk8.Nickname);
                break;
        }
    }

    private static void ClearTrash(Span<byte> trash, string name)
    {
        trash.Clear();
        int maxLength = trash.Length / 2;
        int actualLength = Math.Min(name.Length, maxLength);
        for (int i = 0; i < actualLength; i++)
        {
            char value = name[i];
            trash[i * 2] = (byte)value;
            trash[(i * 2) + 1] = (byte)(value >> 8);
        }
        if (actualLength < maxLength)
        {
            trash[actualLength * 2] = 0x00;
            trash[(actualLength * 2) + 1] = 0x00;
        }
    }

    /// <summary>
    /// Gets the game version based on the PKM type
    /// </summary>
    /// <returns>GameVersion enum representing the current game</returns>
    /// <exception cref="ArgumentException">Thrown when PKM type is not supported</exception>
    public static GameVersion GetGameVersion() => typeof(T) switch
    {
        var t when t == typeof(PK8) => GameVersion.SWSH,
        var t when t == typeof(PB8) => GameVersion.BDSP,
        var t when t == typeof(PA8) => GameVersion.PLA,
        var t when t == typeof(PK9) => GameVersion.SV,
        _ => throw new ArgumentException("Unsupported game version.")
    };

    /// <summary>
    /// Gets list of breedable species for the specified game version
    /// </summary>
    /// <param name="gameVersion">Game version to check</param>
    /// <returns>List of breedable species IDs</returns>
    public static List<ushort> GetBreedableSpecies(GameVersion gameVersion)
    {
        var breedableSpecies = new List<ushort>();
        var personalTable = GetPersonalTable(gameVersion);
        ushort maxSpecies = GetMaxSpeciesID(personalTable);

        for (ushort speciesId = 1; speciesId <= maxSpecies; speciesId++)
        {
            if (!IsSpeciesInGame(personalTable, speciesId))
                continue;

            var pi = GetFormEntry(personalTable, speciesId, 0);

            if (IsBreedable(pi) && pi.EvoStage == 1)
                breedableSpecies.Add(speciesId);
        }

        return breedableSpecies;
    }

    private static bool IsSpeciesInGame(object personalTable, ushort species) => personalTable switch
    {
        PersonalTable9SV pt => pt.IsSpeciesInGame(species),
        PersonalTable8SWSH pt => pt.IsSpeciesInGame(species),
        PersonalTable8BDSP pt => pt.IsSpeciesInGame(species),
        _ => false
    };

    private static ushort GetMaxSpeciesID(object personalTable) => personalTable switch
    {
        PersonalTable9SV pt => pt.MaxSpeciesID,
        PersonalTable8SWSH pt => pt.MaxSpeciesID,
        PersonalTable8BDSP pt => pt.MaxSpeciesID,
        _ => throw new ArgumentException("Unsupported personal table type.")
    };

    private static bool IsBreedable(PersonalInfo pi)
    {
        if (pi.EggGroup1 == 15 || pi.EggGroup2 == 15)
            return false;

        if (pi.EggGroup1 == 0 && pi.EggGroup2 == 0)
            return false;

        return true;
    }

    private static PersonalInfo GetFormEntry(object personalTable, ushort species, byte form) => personalTable switch
    {
        PersonalTable9SV pt => pt.GetFormEntry(species, form),
        PersonalTable8SWSH pt => pt.GetFormEntry(species, form),
        PersonalTable8BDSP pt => pt.GetFormEntry(species, form),
        _ => throw new ArgumentException("Unsupported personal table type.")
    };

    private static object GetPersonalTable(GameVersion gameVersion) => gameVersion switch
    {
        GameVersion.SV => PersonalTable.SV,
        GameVersion.SWSH => PersonalTable.SWSH,
        GameVersion.BDSP => PersonalTable.BDSP,
        _ => throw new ArgumentException("Unsupported game version.")
    };

    private async Task AddTradeToQueueAsync(
       int code,
       string trainerName,
       T pk,
       RequestSignificance sig,
       SocketUser usr,
       bool isBatchTrade = false,
       int batchTradeNumber = 1,
       int totalBatchTrades = 1,
       bool isHiddenTrade = false,
       bool isMysteryEgg = false,
       List<Pictocodes>? lgcode = null,
       PokeTradeType tradeType = PokeTradeType.Specific,
       bool ignoreAutoOT = false)
    {
        lgcode ??= GenerateRandomPictocodes(3);

        var la = new LegalityAnalysis(pk);
        if (!la.Valid)
        {
            string responseMessage = "An unexpected error occurred. Please try again.";
            var reply = await ReplyAsync(responseMessage).ConfigureAwait(false);
            await Task.Delay(6000).ConfigureAwait(false);
            await reply.DeleteAsync().ConfigureAwait(false);
            return;
        }

        await QueueHelper<T>.AddToQueueAsync(
            Context,
            code,
            trainerName,
            sig,
            pk,
            PokeRoutineType.LinkTrade,
            tradeType,
            usr,
            isBatchTrade,
            batchTradeNumber,
            totalBatchTrades,
            isHiddenTrade,
            isMysteryEgg,
            lgcode: lgcode,
            ignoreAutoOT: ignoreAutoOT).ConfigureAwait(false);
    }

    private static List<Pictocodes> GenerateRandomPictocodes(int count)
    {
        List<Pictocodes> randomPictocodes = [];
        Array pictocodeValues = Enum.GetValues<Pictocodes>();

        for (int i = 0; i < count; i++)
        {
            Pictocodes randomPictocode = (Pictocodes)pictocodeValues.GetValue(Random.Next(pictocodeValues.Length))!;
            randomPictocodes.Add(randomPictocode);
        }

        return randomPictocodes;
    }
}
