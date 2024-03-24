﻿using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.ThirdParty.Exceptions;
using Quartz;
using Serilog;
using StackExchange.Redis;
using System.Text.Json;

namespace Fumo.BackgroundJobs.SevenTV;

internal class FetchRolesJob : IJob
{
    public readonly IDatabase Redis;
    public readonly ISevenTVService SevenTV;
    public readonly ILogger Logger;

    public FetchRolesJob(IDatabase redis, ISevenTVService sevenTV, ILogger logger)
    {
        Redis = redis;
        SevenTV = sevenTV;
        Logger = logger.ForContext<FetchRolesJob>();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var roles = await SevenTV.GetGlobalRoles(context.CancellationToken);

            var json = JsonSerializer.Serialize(roles);

            await Redis.StringSetAsync("seventv:roles", json);
        }
        catch (GraphQLException ex) when (ex.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to get roles from 7TV");
        }
    }
}
