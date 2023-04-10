using Azure;
using Azure.AI.OpenAI;
using Discord;
using Discord.WebSocket;
using DougBot.Models;

namespace DougBot.Systems;

public static class ForumAi
{

    public static async Task Monitor()
    {
        var client = Program._Client;
        client.MessageReceived += ForumAiHandler;
    }

    private static Task ForumAiHandler(SocketMessage arg)
    {
        _ = Task.Run(async () =>
        {
            if(!arg.Author.IsBot && arg.Channel.GetType() == typeof(SocketThreadChannel))
            {
                var threadMessage = arg as SocketUserMessage;
                var threadChannel = threadMessage.Channel as SocketThreadChannel;
                var forumChannel = threadChannel.ParentChannel;
                var threadGuild = threadChannel.Guild;
                var dbGuild = await Guild.GetGuild(threadGuild.Id.ToString());
                if(forumChannel.Id.ToString() == dbGuild.OpenAiChatForum)
                {
                    var embed = new EmbedBuilder()
                        .WithColor(Color.Blue)
                        .WithDescription("Hmm, let me think about that...")
                        .WithFooter("Powered by OpenAI GPT-4");
                    var responseEmbed = await threadChannel.SendMessageAsync(embeds: new []{embed.Build()});
                    var messages = await threadChannel.GetMessagesAsync(200).FlattenAsync();
                    messages = messages.OrderBy(m => m.CreatedAt);
                    //Setup OpenAI
                    var client = new OpenAIClient(new Uri(dbGuild.OpenAiURL), new AzureKeyCredential(dbGuild.OpenAiToken));
                    try
                    {
                        var chatCompletionsOptions = new ChatCompletionsOptions
                        {
                            MaxTokens = 2000,
                            Temperature = 0.5f,
                            PresencePenalty = 0.5f,
                            FrequencyPenalty = 0.5f
                        };
                        //Add messages to chat
                        chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, 
                            "You are an AI assistant in a discord server, you must not use more than 4000 characters in a single message."));
                        foreach (var message in messages)
                        {
                            chatCompletionsOptions.Messages.Add(message.Author.IsBot
                                ? new ChatMessage(ChatRole.Assistant, message.Embeds.FirstOrDefault()?.Description)
                                : new ChatMessage(ChatRole.User, message.Content));
                        }
                        //Get response
                        var response = await client.GetChatCompletionsStreamingAsync("WahSpeech", chatCompletionsOptions);
                        using var streamingChatCompletions = response.Value;
                        //setup embed and variable to update embed every 1 second
                        var nextSend = DateTime.Now.AddSeconds(1);
                        embed.WithDescription("");
                        embed.WithColor(Color.LightOrange);
                        //Stream response
                        await foreach (var choice in streamingChatCompletions.GetChoicesStreaming())
                        {
                            await foreach (var message in choice.GetMessageStreaming())
                            {
                                embed.WithDescription(embed.Description + message.Content);
                                //If the timer has passed, update embed
                                if (DateTime.Now <= nextSend) continue;
                                await responseEmbed.ModifyAsync(m => m.Embeds = new []{embed.Build()});
                                nextSend = DateTime.Now.AddSeconds(1);
                            }
                        }
                        embed.WithColor(Color.Green);
                    }
                    catch (Exception e)
                    {
                        var response = "Failed to analyse chat: " + e.Message;
                        if (e.Message.Contains("content management policy."))
                            response = "Failed to analyse chat: Content is not allowed by Azure's content management policy.";
                        embed.WithColor(Color.Red);
                        embed.WithDescription(response);
                    }
                    await responseEmbed.ModifyAsync(m => m.Embeds = new []{embed.Build()});
                }
            }
        });
        return Task.CompletedTask;
    }
}