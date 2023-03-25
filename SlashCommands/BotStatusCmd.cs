using System.Diagnostics;
using Discord;
using Discord.Interactions;
using DougBot.Models;

namespace DougBot.SlashCommands;

public class BotStatusCmd : InteractionModuleBase
{
    [SlashCommand("botstatus", "Displays the current status of the bot")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public async Task BotStatus()
    {
        if (Context.Guild != null)
        {
            var process = Process.GetCurrentProcess();
            //Get uptime
            var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
            //Get memory usage; 
            var usedMemory = process.PrivateMemorySize64;
            var usedMemoryInMB = usedMemory / (1024 * 1024);
            //Get threads
            var currentAppThreadsCount = process.Threads.Count;
            var threadList = process.Threads.Cast<ProcessThread>().OrderByDescending(t => t.TotalProcessorTime).Select(t => $"ID: {t.Id}\n" + $"  State: {t.ThreadState}\n" + $"  Time: {t.TotalProcessorTime}\n" + $"  Priority: {t.CurrentPriority}").ToList();
            //Get queue
            var queue = await Queue.GetQueues();
            var queueCount = queue.Count;
            var dueCount = queue.Count(q => q.DueAt < DateTime.UtcNow);
            var embed = new EmbedBuilder()
                .WithTitle("Bot Status")
                .AddField("Uptime", $"{uptime.Days} days {uptime.Hours} hours {uptime.Minutes} minutes {uptime.Seconds} seconds", true)
                .AddField("Memory Usage", $"{usedMemoryInMB} MB", true)
                .AddField("Threads", $"{currentAppThreadsCount}", true)
                .AddField("Pending Jobs", queueCount, true)
                .AddField("Due Jobs", dueCount, true)
                .AddField("Thread List", $"```{string.Join("\n", threadList)}```", false)
                .Build();
            await RespondAsync(embeds: new[] { embed }, ephemeral: true);
        }
    }
}