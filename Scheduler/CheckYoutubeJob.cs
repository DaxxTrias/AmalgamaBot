using System.Linq;
using System.Text.Json;
using Discord;
using AmalgamaBot.Models;
using Quartz;
using YoutubeExplode;
using YoutubeExplode.Common;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace AmalgamaBot.Scheduler;

public class CheckYoutubeJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var youtube = new YoutubeClient();
            var dbGuilds = await Guild.GetGuilds();

            foreach (var dbGuild in dbGuilds.Where(g => g?.YoutubeSettings?.Any() ?? false))
            {
                foreach (var dbYoutube in dbGuild.YoutubeSettings.Where(y => !string.IsNullOrWhiteSpace(y?.Id)))
                {
                    try
                    {
                        var ytChannel = await youtube.Channels.GetAsync(dbYoutube.Id);
                        var video = (await youtube.Channels.GetUploadsAsync(dbYoutube.Id)).FirstOrDefault();

                        if (video?.Id.ToString() == dbYoutube.LastVideoId) continue;

                        var mentionRole = "";
                        if (dbYoutube.Id == "UCzL0SBEypNk4slpzSbxo01g" && !video.Title.Contains("VOD"))
                            mentionRole = "<@&812501073289805884>";
                        else if (video?.Duration?.TotalMinutes > 2) mentionRole = $"<@&{dbYoutube.MentionRole}>";

                        var embed = new EmbedBuilder()
                            .WithAuthor(ytChannel.Title, ytChannel.Thumbnails?[0].Url, ytChannel.Url)
                            .WithTitle(video?.Title)
                            .WithImageUrl(video?.Thumbnails?.OrderByDescending(t => t.Resolution.Area)?.FirstOrDefault()?.Url)
                            .WithUrl(video?.Url);
                        var embedJson = JsonSerializer.Serialize(new List<EmbedBuilder> { embed },
                            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });

                        var sendMessageJob = JobBuilder.Create<SendMessageJob>()
                            .WithIdentity($"sendMessageJob-{Guid.NewGuid()}", dbGuild.Id)
                            .UsingJobData("guildId", dbGuild.Id)
                            .UsingJobData("channelId", dbYoutube.PostChannel)
                            .UsingJobData("message", mentionRole)
                            .UsingJobData("embedBuilders", embedJson)
                            .UsingJobData("ping", "true")
                            .UsingJobData("attachments", null)
                            .Build();
                        var sendMessageTrigger = TriggerBuilder.Create()
                            .WithIdentity($"sendMessageTrigger-{Guid.NewGuid()}", dbGuild.Id)
                            .StartNow()
                            .Build();
                        await Quartz.MemorySchedulerInstance.ScheduleJob(sendMessageJob, sendMessageTrigger);
                        dbYoutube.LastVideoId = video?.Id.ToString();
                        await dbGuild.Update();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }
}