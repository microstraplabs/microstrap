namespace Bloxstrap
{
    public sealed class RecommendedGame
    {
        public long UniverseId { get; init; }
        public long PlaceId { get; init; }
        public string Name { get; init; } = "Unknown experience";
        public string Description { get; init; } = "";
        public string Creator { get; init; } = "";
        public long Playing { get; init; }
        public long Visits { get; init; }
        public string? ThumbnailUrl { get; init; }
    }

    /// <summary>
    /// Maximum number of games that can be listed at once.
    /// </summary>
    public static class GamesServiceLimits
    {
        public const int MaxGames = 124;
    }

    /// <summary>
    /// Provides games from Roblox's own web endpoints: keyword search uses the
    /// omni-search API (the same one the Roblox app uses), while the default
    /// browsing list comes from the discovery sorts on the charts page.
    /// </summary>
    internal static class RobloxGamesService
    {
        private class GamesSession
        {
            public string Id { get; } = Guid.NewGuid().ToString();

            public List<string> RemainingSortIds { get; } = new();

            public string SearchNextPageToken { get; set; } = "";

            public string SearchQuery { get; set; } = "";

            public bool SearchExhausted { get; set; } = false;
        }

        private static GamesSession? _session;

        private static GamesSession Session => _session ??= new GamesSession();

        /// <summary>
        /// Resets the session - the next page fetch will start from the top of
        /// the discovery sorts, or run a fresh search.
        /// </summary>
        public static void ResetSession()
        {
            _session = null;
        }

        /// <summary>
        /// Loads the next page of games for the given query. An empty query
        /// fetches recommendations from the discovery sorts. Repeated calls
        /// keep returning fresh games (up to a hard cap of
        /// <see cref="GamesServiceLimits.MaxGames"/>) so lists can be
        /// "infinitely" scrolled until Roblox runs out of content.
        /// </summary>
        public static async Task<IReadOnlyList<RecommendedGame>> GetGamesPageAsync(string query)
        {
            query = query.Trim();

            if (!query.Equals(Session.SearchQuery, StringComparison.OrdinalIgnoreCase))
            {
                Session.SearchQuery = query;
                Session.SearchNextPageToken = "";
                Session.SearchExhausted = false;
                Session.RemainingSortIds.Clear();
            }

            List<RecommendedGame> games = new();

            if (String.IsNullOrEmpty(query))
            {
                // recommendations from the charts page sorts
                if (Session.RemainingSortIds.Count == 0)
                    Session.RemainingSortIds.AddRange(await GetSortIdsAsync(Session.Id));

                while (Session.RemainingSortIds.Count > 0 && games.Count < 25)
                {
                    string sortId = Session.RemainingSortIds[0];

                    List<RecommendedGame> sortGames = await GetSortContentAsync(Session.Id, sortId);

                    Session.RemainingSortIds.RemoveAt(0);

                    games.AddRange(sortGames);
                }
            }
            else
            {
                // keyword search
                if (!Session.SearchExhausted)
                {
                    var (pageGames, nextPageToken) = await SearchGamesAsync(Session.Id, query, Session.SearchNextPageToken);

                    games.AddRange(pageGames);

                    if (String.IsNullOrEmpty(nextPageToken))
                        Session.SearchExhausted = true;
                    else
                        Session.SearchNextPageToken = nextPageToken;
                }
            }

            return games.Take(GamesServiceLimits.MaxGames).ToList();
        }

        /// <summary>
        /// Fetches the details of a single game by its place id.
        /// </summary>
        public static async Task<IReadOnlyList<RecommendedGame>> GetGameDetailsAsync(long placeId)
        {
            // the universe lookup returns a plain { "universeId": ... } object
            var universeResponse = await Http.GetJson<UniverseIdResponse>($"https://apis.roblox.com/universes/v1/places/{placeId}/universe");

            long universeId = universeResponse?.UniverseId ?? 0;

            if (universeId == 0)
                return Array.Empty<RecommendedGame>();

            return await GetGameDetailsAsync(new List<(long, long)> { (universeId, placeId) });
        }

        /// <summary>
        /// Shorthand for fetching the first page of games, without a session.
        /// </summary>
        public static async Task<IReadOnlyList<RecommendedGame>> GetRecommendedGamesAsync()
        {
            try
            {
                return await GetGamesPageAsync("");
            }
            catch (Exception)
            {
                ResetSession();
                throw;
            }
        }

        /// <summary>
        /// Fetches every sort available on the charts page, with the
        /// personalised/recommended ones first.
        /// </summary>
        private static async Task<List<string>> GetSortIdsAsync(string sessionId)
        {
            using var response = await App.HttpClient.GetAsync($"https://apis.roblox.com/explore-api/v1/get-sorts?device=computer&country=all&sessionId={sessionId}");
            response.EnsureSuccessStatusCode();

            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            List<string> sortIds = FindSortIds(document.RootElement)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(sortId => !sortId.Contains("filter", StringComparison.OrdinalIgnoreCase)) // filter pseudo-sorts hold no games
                .ToList();

            // put the sorts people are most likely to want first
            sortIds.Sort((x, y) =>
            {
                int Rank(string sortId)
                {
                    if (sortId.Contains("recommend", StringComparison.OrdinalIgnoreCase)) return 0;
                    if (sortId.Contains("continue", StringComparison.OrdinalIgnoreCase)) return 1;
                    if (sortId.Contains("trending", StringComparison.OrdinalIgnoreCase)) return 2;
                    if (sortId.Contains("popular", StringComparison.OrdinalIgnoreCase)) return 3;
                    return 4;
                }

                return Rank(x).CompareTo(Rank(y));
            });

            return sortIds;
        }

        private static async Task<List<RecommendedGame>> GetSortContentAsync(string sessionId, string sortId)
        {
            string url = $"https://apis.roblox.com/explore-api/v1/get-sort-content?sessionId={sessionId}&sortId={Uri.EscapeDataString(sortId)}&device=computer&country=all";

            using var response = await App.HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            List<(long UniverseId, long PlaceId)> references = FindGameReferences(document.RootElement)
                .GroupBy(x => x.UniverseId)
                .Select(x => x.First())
                .ToList();

            return await GetGameDetailsAsync(references);
        }

        private static async Task<(List<RecommendedGame> Games, string NextPageToken)> SearchGamesAsync(string sessionId, string query, string pageToken)
        {
            string url = $"https://apis.roblox.com/search-api/omni-search?searchQuery={Uri.EscapeDataString(query)}&sessionId={sessionId}&pageType=all&pageToken={Uri.EscapeDataString(pageToken)}";

            using var response = await App.HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            string? nextPageToken = null;
            if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("nextPageToken", out var tokenElement) && tokenElement.ValueKind == JsonValueKind.String)
                nextPageToken = tokenElement.GetString();

            // the response is shaped as:
            // { "searchResults": [ { "contentGroupType": "Game", "contents": [ { "universeId": ..., "rootPlaceId": ... } ] } ], "nextPageToken": ... }
            var references = new List<(long UniverseId, long PlaceId)>();

            if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("searchResults", out var resultsElement) && resultsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var unit in resultsElement.EnumerateArray())
                {
                    if (unit.TryGetProperty("contentGroupType", out var groupType) && !String.Equals(groupType.GetString(), "Game", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!unit.TryGetProperty("contents", out var contentsElement) || contentsElement.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var content in contentsElement.EnumerateArray())
                    {
                        long universeId = ReadLong(content, "universeId");
                        long placeId = ReadLong(content, "rootPlaceId", "universeRootPlaceId");

                        if (universeId != 0 && placeId != 0)
                            references.Add((universeId, placeId));
                    }
                }
            }

            var games = await GetGameDetailsAsync(references);

            return (games, nextPageToken ?? "");
        }

        /// <summary>
        /// Fills in full details (name, creator, player counts, thumbnails) for
        /// a list of games.
        /// </summary>
        private static async Task<List<RecommendedGame>> GetGameDetailsAsync(List<(long UniverseId, long PlaceId)> references)
        {
            references = references
                .Where(x => x.UniverseId != 0 && x.PlaceId != 0)
                .GroupBy(x => x.UniverseId)
                .Select(x => x.First())
                .ToList();

            if (references.Count == 0)
                return new List<RecommendedGame>();

            // the games API rejects large batches ("Too many universe IDs"),
            // so fetch details and thumbnails in chunks
            const int BatchSize = 50;

            var games = new List<RecommendedGame>();

            foreach (var batch in references.Chunk(BatchSize))
            {
                string ids = String.Join(',', batch.Select(x => x.UniverseId));

                var details = await Http.GetJson<ApiArrayResponse<GameDetailResponse>>($"https://games.roblox.com/v1/games?universeIds={ids}");
                var thumbnails = await Http.GetJson<ApiArrayResponse<ThumbnailResponse>>($"https://thumbnails.roblox.com/v1/games/icons?universeIds={ids}&returnPolicy=PlaceHolder&size=512x512&format=Png&isCircular=false");

                var thumbnailMap = thumbnails.Data.ToDictionary(x => x.TargetId, x => x.ImageUrl);

                foreach (var reference in batch)
                {
                    var detail = details.Data.FirstOrDefault(x => x.Id == reference.UniverseId);

                    if (detail is null)
                        continue;

                    games.Add(new RecommendedGame
                    {
                        UniverseId = detail.Id,
                        PlaceId = detail.RootPlaceId != 0 ? detail.RootPlaceId : reference.PlaceId,
                        Name = detail.Name,
                        Description = detail.Description,
                        Creator = detail.Creator?.Name ?? "",
                        Playing = detail.Playing,
                        Visits = detail.Visits,
                        ThumbnailUrl = thumbnailMap.GetValueOrDefault(detail.Id)
                    });
                }
            }

            return games;
        }

        private static IEnumerable<string> FindSortIds(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if ((property.Name.Equals("sortId", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                        && property.Value.ValueKind == JsonValueKind.String)
                    {
                        string? value = property.Value.GetString();
                        if (!String.IsNullOrWhiteSpace(value))
                            yield return value;
                    }

                    foreach (string value in FindSortIds(property.Value))
                        yield return value;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in element.EnumerateArray())
                    foreach (string value in FindSortIds(child))
                        yield return value;
            }
        }

        private static IEnumerable<(long UniverseId, long PlaceId)> FindGameReferences(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                long universeId = ReadLong(element, "universeId", "universeID");
                long placeId = ReadLong(element, "placeId", "rootPlaceId");

                if (universeId != 0 || placeId != 0)
                    yield return (universeId, placeId);

                foreach (var property in element.EnumerateObject())
                    foreach (var value in FindGameReferences(property.Value))
                        yield return value;
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in element.EnumerateArray())
                    foreach (var value in FindGameReferences(child))
                        yield return value;
            }
        }

        private static long ReadLong(JsonElement element, params string[] names)
        {
            foreach (string name in names)
            {
                if (!element.TryGetProperty(name, out var value))
                    continue;

                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number))
                    return number;
                if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out number))
                    return number;
            }

            return 0;
        }
    }
}
