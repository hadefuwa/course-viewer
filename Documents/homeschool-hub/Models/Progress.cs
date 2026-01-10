using System;

namespace HomeschoolHub.Models
{
    public class Progress
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int LessonId { get; set; }
        public int? QuizId { get; set; }
        public bool Completed { get; set; }
        public int? Score { get; set; }
        public int? MaxScore { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}

