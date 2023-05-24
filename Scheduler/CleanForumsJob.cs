using Discord;
using Discord.WebSocket;
using AmalgamaBot.Models;
using AmalgamaBot.Systems;
using Quartz;

namespace AmalgamaBot.Scheduler;

public class CleanForumsJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var dbGuilds = await Guild.GetGuilds();

        // Null check for dbGuilds
        if (dbGuilds == null || !dbGuilds.Any())
        {
            // Consider logging the issue here
            return;
        }

        var client = Program._Client;

        // Null check for client
        if (client == null)
        {
            // Consider logging the issue here
            return;
        }

        foreach (var dbGuild in dbGuilds)
        {
            var guild = client.Guilds.FirstOrDefault(g => g.Id.ToString() == dbGuild.Id);

            // Null check for guild
            if (guild == null)
            {
                // Consider logging the issue here
                continue;
            }

            var forums = guild.Channels.Where(c => c.GetType().Name == "SocketForumChannel");

            foreach (var socketGuildChannel in forums)
            {
                var forum = (SocketForumChannel)socketGuildChannel;

                // Null check for forum
                if (forum == null)
                {
                    // Consider logging the issue here
                    continue;
                }

                var threads = await forum.GetActiveThreadsAsync();
                var forumThreads = threads.Where(t => t.ParentChannelId == forum.Id);

                foreach (var thread in forumThreads)
                {
                    // Null check for thread
                    if (thread == null)
                    {
                        // Consider logging the issue here
                        continue;
                    }

                    var message = await thread.GetMessagesAsync(1).FlattenAsync();

                    // Null check for message
                    if (message == null)
                    {
                        // Consider logging the issue here
                        continue;
                    }

                    //if the thread has no messages or the last message is older than 2 days, archive the thread
                    if ((message.Any() && message.First().Timestamp.UtcDateTime < DateTime.UtcNow.AddDays(-2)) ||
                        (!message.Any() && thread.CreatedAt.UtcDateTime < DateTime.UtcNow.AddDays(-2)))
                    {
                        await thread.ModifyAsync(t => t.Archived = true);
                    }
                }
            }
        }
    }

}