﻿using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Scheduler;
using DougBot.Systems;

namespace DougBot;

public class Program
{
    //Main Variables
    private static InteractionService _Service;
    private static IServiceProvider _ServiceProvider;
    private static DiscordSocketClient _Client;
    private bool _FirstStart = true;


    private static Task Main()
    {
        return new Program().MainAsync();
    }

    private async Task MainAsync()
    {
        //Load Settings
        var settings = Setting.GetSettings();
        //Start discord bot
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
            AlwaysDownloadUsers = true,
            AlwaysResolveStickers = true,
            LogLevel = LogSeverity.Info
        };
        _Client = new DiscordSocketClient(config);
        _Client.Log += Log;
        _Client.Ready += Ready;
        await _Client.LoginAsync(TokenType.Bot, settings.Token);
        await _Client.StartAsync();
        //Block Task
        await Task.Delay(-1);
    }

    private async Task Ready()
    {
        if (_FirstStart)
        {
            _FirstStart = false;
            //Register Plugins
            var scheduler = new Schedule(_Client);
            var handler = new Events(_Client);
            //Register Commands
            _Service = new InteractionService(_Client.Rest);
            _Service.Log += Log;
            await _Service.AddModulesAsync(Assembly.GetEntryAssembly(), _ServiceProvider);
            await _Service.RegisterCommandsGloballyAsync();
            _Client.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(_Client, interaction);
                await _Service.ExecuteCommandAsync(ctx, _ServiceProvider);
            };
            _Service.SlashCommandExecuted += async (command, context, result) =>
            {
                var data = context.Interaction.Data as SocketSlashCommandData;
                var auditFields = new List<EmbedFieldBuilder>
                {
                    new()
                    {
                        Name = "Command",
                        Value = command.Name,
                        IsInline = true
                    },
                    new()
                    {
                        Name = "User",
                        Value = context.User.Mention,
                        IsInline = true
                    },
                    new()
                    {
                        Name = "Channel",
                        Value = (context.Channel as SocketTextChannel).Mention,
                        IsInline = true
                    },
                    data.Options.Count > 0 ? new EmbedFieldBuilder
                        {
                            Name = "Parameters",
                            Value = string.Join("\n", data.Options.Select(x => $"{x.Name}: {x.Value}")),
                            IsInline = true
                        }
                        : new EmbedFieldBuilder
                        {
                            Name = "null",
                            Value = "null",
                            IsInline = true
                        },
                    result.ErrorReason != null ? new EmbedFieldBuilder
                        {
                            Name = "Error",
                            Value = result.ErrorReason,
                            IsInline = true
                        }
                        : new EmbedFieldBuilder
                        {
                            Name = "null",
                            Value = "null",
                            IsInline = true
                        },
                };
                AuditLog.LogEvent(_Client,
                    $"Command Ran",
                    result.IsSuccess, auditFields);

                if (!result.IsSuccess)
                {
                    if (result.ErrorReason.Contains("was not present in the dictionary.") &&
                        command.Name == "timestamp")
                    {
                        await context.Interaction.RespondAsync(
                            "Invalid time format. Please use the format `12:00 GMT ` or `01 Jan 2022 12:00 GMT`",
                            ephemeral: true);
                    }
                    else
                    {
                        if (!context.Interaction.HasResponded)
                            await context.Interaction.RespondAsync($"Error: {result.ErrorReason}", ephemeral: true);
                        else
                            await context.Interaction.ModifyOriginalResponseAsync(m =>
                                m.Content = $"Error: {result.ErrorReason}");
                    }
                }
            };
        }

        //Status
        await _Client.SetStatusAsync(UserStatus.DoNotDisturb);
    }

    private static Task Log(LogMessage msg)
    {
        if (msg.Exception is CommandException cmdException)
        {
            AuditLog.LogEvent(_Client,
                $"{cmdException.Command.Name} failed to execute in {cmdException.Context.Channel} by user {cmdException.Context.User.Username}.",
                false);
            AuditLog.LogEvent(_Client, cmdException.ToString(), false);
        }
        else if (msg.Source == "Command")
        {
            AuditLog.LogEvent(_Client, msg.Message, true);
        }
        else
        {
            Console.WriteLine($"[General/{msg.Severity}] {msg}");
        }

        return Task.CompletedTask;
    }
}