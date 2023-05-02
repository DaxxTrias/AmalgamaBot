using System.Text.Json;
using Discord;
using Discord.WebSocket;
using AmalgamaBot.Models;
using Fernandezja.ColorHashSharp;
using Quartz;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace AmalgamaBot.Scheduler;

public class SendDmJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.JobDetail.JobDataMap;
        var client = Program._Client;
        var guildId = Convert.ToUInt64(dataMap.GetString("guildId"));
        var userId = Convert.ToUInt64(dataMap.GetString("userId"));
        var senderId = Convert.ToUInt64(dataMap.GetString("senderId"));
        var embedBuilders = dataMap.GetString("embedBuilders");

        // Check for nulls and return if any are null
        if (guildId == 0 || userId == 0 || senderId == 0 || embedBuilders == null)
            return;

        var dbGuild = await Guild.GetGuild(guildId.ToString());
        if (dbGuild == null)
        {
            // Handle the case when the dbGuild is not found
            return;
        }

        var guild = client.Guilds.FirstOrDefault(g => g.Id == guildId);
        if (guild == null)
        {
            // Handle the case when the guild is not found
            return;
        }

        var channel = guild.Channels.FirstOrDefault(c => c.Id.ToString() == dbGuild.DmReceiptChannel) as SocketTextChannel;
        if (channel == null)
        {
            // Handle the case when the channel is not found or is not a SocketTextChannel
            return;
        }

        var user = await client.GetUserAsync(userId);
        var sender = await client.GetUserAsync(senderId);

        if (user == null || sender == null)
        {
            // Handle the case when the user or sender is not found
            return;
        }

        var embedBuildersList = JsonSerializer.Deserialize<List<EmbedBuilder>>(embedBuilders,
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });

        var embeds = embedBuildersList.Select(embed => embed.Build()).ToList();
        string status;
        var color = (Color)embeds[0].Color;
        var colorHash = new ColorHash();

        try
        {
            await user.SendMessageAsync(embeds: embeds.ToArray());
            status = "Message Delivered";
            color = (Color)colorHash.BuildToColor(userId.ToString());
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Cannot send messages to this user"))
                status = "User has blocked DMs";
            else
                status = "Error: " + ex.Message;
            color = Color.Red;
        }

        embeds = embedBuildersList.Select(embed =>
            embed.WithTitle(status)
                .WithColor(color)
                .WithAuthor($"DM to {user.Username}#{user.Discriminator} ({user.Id}) from {sender.Username}",
                    sender.GetAvatarUrl())
                .Build()).ToList();
        await channel.SendMessageAsync(embeds: embeds.ToArray());
    }
}

