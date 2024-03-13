using Discord;
using Discord.Commands;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private const string detail = "I am a free and open-source Discord bot powered by PKHeX & AutoLegalityMod, upgraded through time, in pieces, by a collaboration of multiple developers of all skill levels with bright ideas and a hobby.";
        private const string repo = "[Original Source](https://github.com/kwsch/SysBot.NET)";

        [Command("info")]
        [Alias("about", "whoami", "owner", "bot")]
        public async Task InfoAsync()
        {
            var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = detail,
            };

            builder.AddField("Info",
                $"- {Format.Bold("Owner")}: {app.Owner} ({app.Owner.Id})\n" +
                $"- {Format.Bold("Original Source")}: {("[SysBot.Net](https://github.com/kwsch/SysBot.NET)")}\n" +
                $"- {Format.Bold("Bot Fork")}: {("[ZE FusionBot](https://discord.com/channels/709788621896417370/1159521117854257242)")}\n" +
                $"- {Format.Bold("Fork Base")}: {("[MergeBot](https://github.com/bdawg1989/MergeBot)")}\n" +
                $"- {Format.Bold("Library")}: Discord.Net ({DiscordConfig.Version})\n" +
                $"- {Format.Bold("Uptime")}: {GetUptime()}\n" +
                $"- {Format.Bold("Runtime")}: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture}\n" +
                $"- {Format.Bold("Buildtime")}: {GetVersionInfo("SysBot.Base", false)}\n" +
                $"- {Format.Bold("Core & ALM Version")}: {GetVersionInfo("PKHeX.Core")}\n" +
                $"- {Format.Bold("Operating System")}: {("Windows X-Lite Optimum 11 Professional 2H22")}\n" +
                $"- {Format.Bold("Contributers")}: {("Secludedly(Sec), GenPKM.com(Gengar), DeVry, SantaCrab, Caburus(Kai), kwsch(Kurt), koiffeinated(Koi)")}\n"
            );

            builder.AddField("Stats",
                $"- {Format.Bold("Heap Size")}: {GetHeapSize()}MiB\n" +
                $"- {Format.Bold("Guilds")}: {Context.Client.Guilds.Count}\n" +
                $"- {Format.Bold("Channels")}: {Context.Client.Guilds.Sum(g => g.Channels.Count)}\n" +
                $"- {Format.Bold("Users")}: {Context.Client.Guilds.Sum(g => g.MemberCount)}\n"
            );

            await ReplyAsync("Curious? Here's some info about me!", embed: builder.Build()).ConfigureAwait(false);
        }

        private static string GetUptime() => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
        private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.CurrentCulture);

        private static string GetVersionInfo(string assemblyName, bool inclVersion = true)
        {
            const string _default = "Unknown";
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assembly = Array.Find(assemblies, x => x.GetName().Name == assemblyName);

            var attribute = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute is null)
                return _default;

            var info = attribute.InformationalVersion;
            var split = info.Split('+');
            if (split.Length >= 2)
            {
                var version = split[0];
                var revision = split[1];
                if (DateTime.TryParseExact(revision, "yyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var buildTime))
                    return (inclVersion ? $"{version} " : "") + $@"{buildTime:yy-MM-dd\.hh\:mm}";
                return inclVersion ? version : _default;
            }
            return _default;
        }

        private string GetOperatingSystemInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows " + Environment.OSVersion.Version.ToString();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "Linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "MacOS";
            }
            else
            {
                return "Unknown";
            }
        }
    }
}
