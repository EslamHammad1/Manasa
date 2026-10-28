namespace Manasa.Utilities;

public static class VideoUrlPolicy
{
    private static readonly HashSet<string> YouTubeHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com",
        "www.youtube.com",
        "youtu.be"
    };

    private static readonly HashSet<string> VimeoHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "vimeo.com",
        "www.vimeo.com",
        "player.vimeo.com"
    };

    public static bool TryGetEmbedUrl(string? value, out string embedUrl)
    {
        embedUrl = string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !uri.IsDefaultPort
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (YouTubeHosts.Contains(uri.Host) && TryGetYouTubeId(uri, segments, out var youtubeId))
        {
            embedUrl = $"https://www.youtube-nocookie.com/embed/{youtubeId}";
            return true;
        }

        if (VimeoHosts.Contains(uri.Host) && TryGetVimeoId(uri.Host, segments, out var vimeoId))
        {
            embedUrl = $"https://player.vimeo.com/video/{vimeoId}";
            return true;
        }

        return false;
    }

    private static bool TryGetYouTubeId(Uri uri, string[] segments, out string id)
    {
        id = string.Empty;
        string? candidate = null;

        if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase) && segments.Length == 1)
        {
            candidate = segments[0];
        }
        else if (segments.Length == 2 && segments[0] is "embed" or "shorts")
        {
            candidate = segments[1];
        }
        else if (segments.Length == 1 && segments[0] == "watch")
        {
            foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2 && Uri.UnescapeDataString(parts[0]) == "v")
                {
                    candidate = Uri.UnescapeDataString(parts[1]);
                    break;
                }
            }
        }

        if (candidate is null || candidate.Length != 11
            || candidate.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-')))
        {
            return false;
        }

        id = candidate;
        return true;
    }

    private static bool TryGetVimeoId(string host, string[] segments, out string id)
    {
        id = string.Empty;
        string? candidate = null;

        if (host.Equals("player.vimeo.com", StringComparison.OrdinalIgnoreCase)
            && segments.Length == 2 && segments[0] == "video")
        {
            candidate = segments[1];
        }
        else if (!host.Equals("player.vimeo.com", StringComparison.OrdinalIgnoreCase) && segments.Length == 1)
        {
            candidate = segments[0];
        }

        if (candidate is null || candidate.Length is < 1 or > 12 || candidate.Any(character => !char.IsAsciiDigit(character)))
        {
            return false;
        }

        id = candidate;
        return true;
    }
}
