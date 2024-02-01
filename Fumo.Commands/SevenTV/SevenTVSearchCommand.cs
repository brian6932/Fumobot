﻿using Fumo.Shared.Exceptions;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Regexes;
using Fumo.ThirdParty.Emotes.SevenTV;
using Fumo.ThirdParty.Exceptions;
using System.Text;
using Fumo.ThirdParty.Emotes.SevenTV.Models;

namespace Fumo.Commands.SevenTV;

public class SevenTVSearchCommand : ChatCommand
{
    private readonly int MaxEmoteOutput = 5;

    private readonly ISevenTVService SevenTV;
    private readonly IUserRepository UserRepository;

    public SevenTVSearchCommand()
    {
        SetName("7tv$|search");
        SetDescription("Search 7TV emotes");

        AddParameter(new(typeof(string), "uploader"));
        AddParameter(new(typeof(bool), "exact"));
    }

    public SevenTVSearchCommand(ISevenTVService sevenTV, IUserRepository userRepository) : this()
    {
        SevenTV = sevenTV;
        UserRepository = userRepository;
    }

    private async ValueTask<CommandResult> GetEmoteFromName(string searchTerm, CancellationToken ct)
    {
        var exact = GetArgument<bool>("exact");

        var emotes = await SevenTV.SearchEmotesByName(searchTerm, exact, ct);

        if (TryGetArgument<string>("uploader", out var uploader))
        {
            await FilterByUploader(emotes.Items, UsernameCleanerRegex.CleanUsername(uploader), ct);
        }

        if (emotes.Items.Count == 0)
        {
            return "No emotes found";
        }

        if (emotes.Items.Count == 1)
        {
            var emote = emotes.Items[0];

            return $"{emote.Name} - https://7tv.app/emotes/{emote.Id}";
        }

        StringBuilder builder = new();

        var emotesToDisplay = emotes.Items.Take(MaxEmoteOutput);

        foreach (var emote in emotesToDisplay)
        {
            builder.Append($"{emote.Name} - https://7tv.app/emotes/{emote.Id}");

            if (emote != emotesToDisplay.Last())
            {
                builder.Append(" | ");
            }
        }

        return builder.ToString();
    }

    private async ValueTask<CommandResult> GetEmoteFromID(string id, CancellationToken ct)
    {
        try
        {
            var emote = await SevenTV.SearchEmoteByID(id, ct);

            if (emote is null)
            {
                return $"No emote with the ID of {id} found";
            }

            return $"{emote.Name} - https://7tv.app/emotes/{emote.Id}";
        }
        catch (GraphQLException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private async ValueTask FilterByUploader(List<SevenTVEmoteByNameItem> emotes, string uploader, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(uploader))
            return;

        var user = await UserRepository.SearchName(uploader, ct);
        var seventvUser = await SevenTV.GetUserInfo(user.TwitchID, ct);

        emotes.RemoveAll(x => x.Owner.Id != seventvUser.Id);
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var searchTerm = Input.ElementAtOrDefault(0) ?? throw new InvalidInputException("Missing a search term");

        var potentialID = ExtractSevenTVIDRegex.Extract(searchTerm);

        if (potentialID is string id)
        {
            return await GetEmoteFromID(id, ct);
        }

        return await GetEmoteFromName(searchTerm, ct);
    }
}
