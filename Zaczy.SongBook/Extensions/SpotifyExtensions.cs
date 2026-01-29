using System.Text.RegularExpressions;

namespace Zaczy.SongBook.Extensions
{
    public static class SpotifyExtensions
    {
        // Matches typical Spotify track URLs/URIs and captures the track id in group "id".
        // Examples matched:
        //  - https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp
        //  - https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp?si=...
        //  - spotify:track:3n3Ppam7vgaVa1iaRUc9Lp
        //  - https://open.spotify.com/embed/track/3n3Ppam7vgaVa1iaRUc9Lp
        private const string SpotifyTrackPattern =
            @"(?:(?:https?:\/\/(?:open\.)?spotify\.com\/(?:embed\/)?track\/)|(?:https?:\/\/play\.spotify\.com\/track\/)|(?:spotify:track:))(?<id>[A-Za-z0-9]{22})";

        private static readonly Regex SpotifyTrackRegex = new(SpotifyTrackPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Extracts a Spotify track id (22-char) from a variety of Spotify links/URIs.
        /// Returns null if no id could be found.
        /// </summary>
        public static string? ExtractSpotifyTrackId(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var trimmed = input.Trim();

            // 1) Try full-pattern match (URLs and spotify: URIs)
            var m = SpotifyTrackRegex.Match(trimmed);
            if (m.Success)
                return m.Groups["id"].Value;

            // 2) Fallback: if the input itself is just the id (22 alnum chars)
            if (Regex.IsMatch(trimmed, @"^[A-Za-z0-9]{22}$"))
                return trimmed;

            return null;
        }

    }
}