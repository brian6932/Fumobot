﻿using Autofac;
using Fumo.Shared.Models;
using Fumo.Shared.Interfaces;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Models;
using Fumo.Database.Extensions;
using Fumo.Database;
using MediatR;
using Fumo.Shared.Mediator;

namespace Fumo.Application.Bot;

public class IrcHandler
{
    private readonly Serilog.ILogger Logger;
    private readonly ILifetimeScope Scope;
    private readonly AppSettings Settings;
    private readonly CancellationTokenSource CancellationTokenSource;
    private readonly IrcClient IrcClient;
    private readonly IChannelRepository ChannelRepository;
    private readonly MetricsTracker MetricsTracker;

    public IrcHandler(
        Serilog.ILogger logger,
        ILifetimeScope scope,
        AppSettings settings,
        CancellationTokenSource cancellationTokenSource,
        IrcClient ircClient,
        IChannelRepository channelRepository,
        MetricsTracker metricsTracker)
    {
        Logger = logger.ForContext<IrcHandler>();
        Scope = scope;
        Settings = settings;
        CancellationTokenSource = cancellationTokenSource;
        IrcClient = ircClient;
        ChannelRepository = channelRepository;
        MetricsTracker = metricsTracker;

        IrcClient.OnMessage += IrcClient_OnMessage;
    }

    public async ValueTask Start()
    {
        Logger.Information("Connecting to TMI");
        var token = CancellationTokenSource.Token;

        var connected = Settings.DebugTMI switch
        {
            true => await IrcClient.ConnectAsync("ws://localhost:6969/", token),
            false => await IrcClient.ConnectAsync(cancellationToken: token)
        };

        if (!connected)
        {
            Logger.Fatal("Failed to connect to TMI");
            return;
        }

        Logger.Information("Connected to TMI");

        var channels = ChannelRepository.GetAll()
            .Where(x => x.GetSetting(ChannelSettingKey.ConnectedWithEventsub) == false.ToString())
            .Select(x => x.TwitchName);

        foreach (var channel in channels)
        {
            await IrcClient.JoinChannel(channel, token);
        }
    }

    private async ValueTask IrcClient_OnMessage(Privmsg privmsg)
    {
        try
        {
            var token = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token).Token;

            // TODO: Figure out if memory leak and if 'using' would break.
            var messageScope = Scope.BeginLifetimeScope();
            var userRepo = messageScope.Resolve<IUserRepository>();
            var bus = messageScope.Resolve<IMediator>();

            var channel = ChannelRepository.GetByID(privmsg.Channel.Id.ToString());
            if (channel is null) return;

            var user = await userRepo.SearchID(privmsg.Author.Id.ToString(), token);

            MetricsTracker.TotalMessagesRead.WithLabels(channel.TwitchName).Inc();

            if (!user.TwitchName.Equals(privmsg.Author.Name))
            {
                user.TwitchName = privmsg.Author.Name;
                user.UsernameHistory.Add(new(user.TwitchName, DateTime.Now));

                await userRepo.SaveChanges(token);
            }

            var input = privmsg.Content
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            bool isBroadcaster = user.TwitchID == channel.TwitchID;
            bool isMod = privmsg.Author.IsMod || isBroadcaster;

            MessageReceivedCommand message = new ChatMessage(channel, user, input, isBroadcaster, isMod, messageScope, privmsg.Id);

            await bus.Publish(message, token);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to handle message in {Channel}", privmsg.Channel.Name);
        }
    }
}
