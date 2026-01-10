namespace HomeschoolHub.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int AgeGroup { get; set; }
        public int OrderIndex { get; set; }
    }
}

