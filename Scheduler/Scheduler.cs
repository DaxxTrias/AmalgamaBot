using Discord.WebSocket;
using DougBot.Models;

namespace DougBot.Scheduler;

public class Scheduler
{
    public static async Task Schedule(DiscordSocketClient Client)
    {
        while (true)
            try
            {
                await Task.Delay(1000);
                //Run items 
                var queueItems = await Queue.GetQueuesDue(10);
                foreach (var queue in queueItems)
                {
                    try
                    {
                        //Run queue
                        switch (queue.Type)
                        {
                            case "RemoveRole":
                                await Role.Remove(Client,
                                    ulong.Parse(queue.Keys["guildId"]),
                                    ulong.Parse(queue.Keys["userId"]),
                                    ulong.Parse(queue.Keys["roleId"]));
                                break;
                            case "AddRole":
                                await Role.Add(Client,
                                    ulong.Parse(queue.Keys["guildId"]),
                                    ulong.Parse(queue.Keys["userId"]),
                                    ulong.Parse(queue.Keys["roleId"]));
                                break;
                            case "RemoveReaction":
                                await Reaction.Remove(Client,
                                    ulong.Parse(queue.Keys["guildId"]),
                                    ulong.Parse(queue.Keys["channelId"]),
                                    ulong.Parse(queue.Keys["messageId"]),
                                    queue.Keys["emoteName"]);
                                break;
                            case "SendMessage":
                                await Message.Send(Client,
                                    ulong.Parse(queue.Keys["guildId"]),
                                    ulong.Parse(queue.Keys["channelId"]),
                                    queue.Keys["message"],
                                    queue.Keys["embedBuilders"],
                                    bool.Parse(queue.Keys["ping"]),
                                    queue.Keys["attachments"]);
                                break;
                            case "SendDM":
                                await Message.SendDM(Client,
                                    ulong.Parse(queue.Keys["guildId"]),
                                    ulong.Parse(queue.Keys["userId"]),
                                    ulong.Parse(queue.Keys["SenderId"]),
                                    queue.Keys["embedBuilders"]);
                                break;
                            case "FreshCheck":
                                await Onboarding.FreshmanCheck(Client,
                                    ulong.Parse(queue.Keys["guildId"]),
                                    ulong.Parse(queue.Keys["userId"]));
                                break;
                            case "SetStatus":
                                await Client.SetGameAsync(queue.Keys["status"]);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        queue.DueAt = DateTime.UtcNow.AddYears(10);
                        await queue.Update();
                    }

                    await queue.Remove();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
    }
}