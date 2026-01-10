namespace HomeschoolHub.Models
{
    public class VideoResource
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string YouTubeVideoId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AgeGroup { get; set; }
    }
}

