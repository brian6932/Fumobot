﻿using Autofac;
using Fumo.BackgroundJobs;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Extensions.AutoFacInstallers;
using Fumo.Interfaces;
using Fumo.Repository;
using Fumo.ThirdParty.ThreeLetterAPI;
using Fumo.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Quartz;
using Serilog;
using System.Runtime.InteropServices;

namespace Fumo;

internal class Program
{
    static async Task Main(string[] args)
    {
        var cwd = Directory.GetCurrentDirectory();
        var configPath = args.Length > 0 ? args[0] : "config.json";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(cwd)
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .Build();

        var container = new ContainerBuilder()
            .InstallGlobalCancellationToken(configuration)
            .InstallConfig(configuration)
            .InstallSerilog(configuration)
            .InstallDatabase(configuration)
            .InstallSingletons(configuration)
            .InstallScoped(configuration)
            .InstallQuartz(configuration)
            .Build();

        Log.Logger.Information("Starting up");

        using (var scope = container.BeginLifetimeScope())
        {
            var commandRepo = scope.Resolve<CommandRepository>();
            commandRepo.LoadAssemblyCommands();

            // The simplest way of handling the bot's channel/user is just initializing it here.
            var config = scope.Resolve<IConfiguration>();
            var tlp = scope.Resolve<IThreeLetterAPI>();
            var db = scope.Resolve<DatabaseContext>();
            var ctoken = scope.Resolve<CancellationTokenSource>().Token;

            Log.Information("Checking for Pending migrations");
            await db.Database.MigrateAsync(ctoken);

            var scheduler = scope.Resolve<IScheduler>();

            var botChannel = await db.Channels
                .Where(x => x.UserTwitchID.Equals(config["Twitch:UserID"]))
                .SingleOrDefaultAsync();

            if (botChannel is null)
            {
                var response = await tlp.SendAsync<BasicUserResponse>(new BasicUserInstruction(id: config["Twitch:UserID"]), ctoken);

                UserDTO user = new()
                {
                    TwitchID = response.User.ID,
                    TwitchName = response.User.Login,
                    Permissions = new() { "default", "bot" }
                };

                ChannelDTO channel = new()
                {
                    TwitchID = response.User.ID,
                    TwitchName = response.User.Login,
                    UserTwitchID = response.User.ID,
                };

                // add to database
                db.Channels.Add(channel);
                db.Users.Add(user);

                await db.SaveChangesAsync();
            }

            Log.Information("Registering Quartz jobs");
            await JobRegister.RegisterJobs(scheduler, ctoken);

            // Start up some singletons
            _ = scope.Resolve<ICommandHandler>();
            _ = scope.Resolve<ICooldownHandler>();
            _ = scope.Resolve<IMessageSenderHandler>();

            await scheduler.Start(ctoken);

            await scope.Resolve<IApplication>().StartAsync();
        }

        var token = container.Resolve<CancellationTokenSource>();

        while (!token.IsCancellationRequested)
        {
            // Idk, Console.ReadLine doesn't work as a systemctl service
            await Task.Delay(100);
        }

        await container.Resolve<IScheduler>().Shutdown();
        await container.DisposeAsync();
    }
}
