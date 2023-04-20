using DougBot.Models;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Interfaces;

namespace DougBot.Twitch;

public class Twitch
{
    public static TwitchAPI API { get; private set; }
    public static TwitchClient IRC { get; private set; }
    public static async Task RunClient()
    {
        try
        {
            Console.WriteLine("Twitch Initialized");
            //Load settings
            var settings = (await Guild.GetGuild("567141138021089308")).TwitchSettings;
            //Setup API
            API = new TwitchAPI();
            var monitor = new LiveStreamMonitorService(API);
            monitor.SetChannelsByName(new List<string> { settings.ChannelName });
            monitor.OnStreamOnline += Monitor_OnStreamOnline;
            monitor.OnStreamOffline += Monitor_OnStreamOffline;
            monitor.Start();
            //Setup tokens
            RefreshResponse dougRefresh = null;
            RefreshResponse botRefresh = null;
            //Setup PubSub
            var pubSub = new PubSub().Create();
            pubSub.OnPubSubServiceConnected += (Sender, e) =>
            {
                while (dougRefresh == null)
                {
                    Console.WriteLine("Waiting for tokens");
                    Task.Delay(1000);
                }

                pubSub.SendTopics(dougRefresh.AccessToken);
                Console.WriteLine("PubSub Connected");
            };
            //Setup IRC anonymously
            IRC = new IRC().Create(settings.ChannelName);
            //Refresh token when expired
            while (true)
            {
                try
                {
                    Console.WriteLine("Refreshing Tokens");
                    //Refresh tokens
                    botRefresh =
                        await API.Auth.RefreshAuthTokenAsync(settings.BotRefreshToken, settings.ClientSecret,
                            settings.ClientId);
                    dougRefresh =
                        await API.Auth.RefreshAuthTokenAsync(settings.ChannelRefreshToken, settings.ClientSecret,
                            settings.ClientId);
                    API.Settings.AccessToken = botRefresh.AccessToken;
                    API.Settings.ClientId = settings.ClientId;
                    //Connect IRC
                    var credentials = new ConnectionCredentials(settings.BotName, Twitch.API.Settings.AccessToken, disableUsernameCheck: true);
                    IRC.SetConnectionCredentials(credentials);
                    IRC.Connect();
                    IRC.JoinChannel(settings.ChannelName);
                    //Update PubSub
                    pubSub.Connect();
                    pubSub.ListenToChannelPoints(settings.ChannelId);
                    pubSub.ListenToPredictions(settings.ChannelId);
                    //Get the lowest refresh time
                    var refreshTime = botRefresh.ExpiresIn < dougRefresh.ExpiresIn
                        ? botRefresh.ExpiresIn
                        : dougRefresh.ExpiresIn;
                    Console.WriteLine(
                        $"Refreshed Tokens in {refreshTime} seconds at {DateTime.UtcNow.AddSeconds(refreshTime):HH:mm}");
                    await Task.Delay((refreshTime - 1800) * 1000);
                    //Disconnected
                    pubSub.Disconnect();
                    IRC.Disconnect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss}] Error refreshing tokens: {ex}");
                    await Task.Delay(60000);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static void Monitor_OnStreamOnline(object? sender, OnStreamOnlineArgs Stream)
    {
        Console.WriteLine($"Stream Online: {Stream.Channel}");
        //Automate online ticker, ping, perhaps twitch things like disable emote only mode
    }

    private static void Monitor_OnStreamOffline(object? sender, OnStreamOfflineArgs Stream)
    {
        Console.WriteLine($"Stream Offline: {Stream.Channel}");
        //Automate delete ticker, perhaps twitch things like enable emote only mode for offline chat
    }
}