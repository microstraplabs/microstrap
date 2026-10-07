namespace Bloxstrap.Utility
{
    internal static class RobloxNewsFeed
    {
        private static readonly (string Url, string Category)[] FeedUrls =
        {
            ("https://devforum.roblox.com/c/updates/announcements/36.json", "Announcements")
        };

        public static async Task<IReadOnlyList<RobloxNewsTopic>> GetAnnouncementsAsync()
        {
            var topics = new List<RobloxNewsTopic>();
            foreach (var (feedUrl, category) in FeedUrls)
            {
                try
                {
                    var feed = await Http.GetJson<RobloxAnnouncementsResponse>(feedUrl);
                    var feedTopics = feed.TopicList?.Topics ?? new List<RobloxNewsTopic>();
                    foreach (var topic in feedTopics)
                        topic.Category = category;
                    topics.AddRange(feedTopics);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("RobloxNewsFeed::GetAnnouncementsAsync", ex);
                }
            }

            return topics
                .Where(topic => topic.Id > 0)
                .GroupBy(topic => topic.Id)
                .Select(group => group.First())
                .OrderByDescending(topic => topic.CreatedAt)
                .ToList();
        }
    }

    internal sealed class RobloxAnnouncementsResponse
    {
        [JsonPropertyName("topic_list")]
        public RobloxTopicList? TopicList { get; set; }
    }

    internal sealed class RobloxTopicList
    {
        [JsonPropertyName("topics")]
        public List<RobloxNewsTopic>? Topics { get; set; }
    }

    internal sealed class RobloxNewsTopic
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("fancy_title")]
        public string FancyTitle { get; set; } = "";

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = "";

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("excerpt")]
        public string Excerpt { get; set; } = "";

        [JsonIgnore]
        public string Category { get; set; } = "Announcements";

        public string DisplayTitle => String.IsNullOrWhiteSpace(FancyTitle) ? Title : FancyTitle;

        public string Summary
        {
            get
            {
                string plainText = Regex.Replace(Excerpt, "<[^>]*>", " ");
                return WebUtility.HtmlDecode(Regex.Replace(plainText, "\\s+", " ")).Trim();
            }
        }

        public string Url => Id > 0 && !String.IsNullOrWhiteSpace(Slug)
            ? $"https://devforum.roblox.com/t/{Uri.EscapeDataString(Slug)}/{Id}"
            : "https://devforum.roblox.com/c/updates/announcements/36";
    }
}
