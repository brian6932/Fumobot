﻿using Fumo.Shared.Enums;
using Fumo.Shared.Models;
using Fumo.Shared.Utils;

namespace Fumo.Commands;

public class PingCommand : ChatCommand
{
    private static DateTime Start;

    public PingCommand()
    {
        if (Start == DateTime.MinValue)
        {
            Start = DateTime.Now;
        }

        SetName("[Pp]ing");
        SetFlags(ChatCommandFlags.Reply);
    }

    public override ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var uptime = DateTime.Now - Start;

        string time = new SecondsFormatter().SecondsFmt(uptime.TotalSeconds, limit: 4);
        return ValueTask.FromResult(new CommandResult
        {
            Message = $"🕴️ Uptime: {time}",
        });
    }
}
