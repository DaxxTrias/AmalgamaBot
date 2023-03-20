using Discord;
using Discord.Interactions;
using DougBot.Models;

namespace DougBot.SlashCommands;

public class SetSlowModeCMD : InteractionModuleBase
{
    [SlashCommand("setslowmode", "Set the slow mode of a channel")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public async Task SetStatus([Summary(description: "Channel to update (This Channel)")] ITextChannel channel = null,
        [Summary("Slow mode in seconds (5)")] [MaxValue(21600)] int seconds = 5)
    {
        if(channel == null)
            channel = (ITextChannel)Context.Channel;
        await channel.ModifyAsync(x => x.SlowModeInterval = seconds);
        await RespondAsync($"Set slow mode to {seconds} seconds in {channel.Mention}", ephemeral: true);
    }
}