using System.Text.RegularExpressions;
using FluentResults;
using Microsoft.Extensions.Logging;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.Clients;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.Services;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Services;

public sealed class SetlistFmService : ISetlistFmService
{
    private static readonly Regex SetlistIdRegex =
        new(@"-([a-f0-9]+)\.html$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ISetlistFmClient _client;
    private readonly ILogger<SetlistFmService> _logger;

    public SetlistFmService(ISetlistFmClient client, ILogger<SetlistFmService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Result<SetlistDto>> GetSetlistAsync(string setlistFmUrl, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(setlistFmUrl, UriKind.Absolute, out var uri)
            || !uri.Host.Equals("www.setlist.fm", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Fail("URL must be an absolute URI with host www.setlist.fm");
        }

        var match = SetlistIdRegex.Match(uri.AbsolutePath);
        if (!match.Success)
        {
            _logger.LogWarning("Could not extract setlist id from URL {Url}", setlistFmUrl);
            return Result.Fail("Could not extract setlist id from URL");
        }

        var setlistId = match.Groups[1].Value;
        _logger.LogInformation("Extracted setlist id {SetlistId} from URL", setlistId);

        return await _client.GetSetlistByIdAsync(setlistId, ct);
    }
}
