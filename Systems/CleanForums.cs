using Discord;
using Discord.WebSocket;
using DougBot.Models;

namespace DougBot.Systems;

public static class CleanForums
{
    public static async Task Clean(DiscordSocketClient client)
    {
        Console.WriteLine("CleanForums Initialized");
        while (true)
        {
            await Task.Delay(3600000);
            try
            {
                await using var db = new Database.DougBotContext();
                var dbGuilds = db.Guilds.ToList();
                foreach (var dbGuild in dbGuilds)
                {
                    //Get forums from client
                    var guild = client.Guilds.FirstOrDefault(g => g.Id.ToString() == dbGuild.Id);
                    var forums = guild.Channels.Where(c => c.GetType().Name == "SocketForumChannel");
                    //Loop all the forums in the guild
                    foreach (SocketForumChannel forum in forums)
                    {
                        //Get threads in the forum
                        var threads = await forum.GetActiveThreadsAsync();
                        var forumThreads = threads.Where(t => t.ParentChannelId == forum.Id);
                        //Loop threads
                        foreach (var thread in forumThreads)
                        {
                            //Check if the most recent message is older than 2 days and close if so
                            var message = await thread.GetMessagesAsync(1).FlattenAsync();
                            if (message.First().Timestamp.UtcDateTime < DateTime.UtcNow.AddDays(-2))
                                await thread.ModifyAsync(t => t.Archived = true);
                            else if (!message.Any() && thread.CreatedAt.UtcDateTime < DateTime.UtcNow.AddDays(-2))
                                await thread.ModifyAsync(t => t.Archived = true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}