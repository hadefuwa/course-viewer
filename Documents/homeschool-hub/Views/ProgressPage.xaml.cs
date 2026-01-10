using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HomeschoolHub.Data;
using HomeschoolHub.Models;

namespace HomeschoolHub.Views
{
    public sealed partial class ProgressPage : Page
    {
        private DatabaseContext _db = new();
        private int _studentId = 0;

        public ProgressPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is int studentId)
            {
                _studentId = studentId;
            }

            LoadProgress();
        }

        private void LoadProgress()
        {
            var progressList = _db.GetStudentProgress(_studentId);
            ProgressStackPanel.Children.Clear();

            if (progressList.Count == 0)
            {
                var noProgressText = new TextBlock
                {
                    Text = "No progress yet. Complete some lessons or quizzes to see your progress here!",
                    FontSize = 18,
                    TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0),
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                };
                ProgressStackPanel.Children.Add(noProgressText);
                return;
            }

            var summaryText = new TextBlock
            {
                Text = $"You've completed {progressList.Count} activities!",
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            ProgressStackPanel.Children.Add(summaryText);

            foreach (var progress in progressList)
            {
                var card = CreateProgressCard(progress);
                ProgressStackPanel.Children.Add(card);
            }
        }

        private Border CreateProgressCard(Progress progress)
        {
            var card = new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 15),
                CornerRadius = new CornerRadius(10),
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
                BorderThickness = new Thickness(1)
            };

            var stackPanel = new StackPanel { Spacing = 10 };

            string activityType = progress.QuizId.HasValue ? "Quiz" : "Lesson";
            string title = activityType + " #" + (progress.QuizId ?? progress.LessonId);

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };

            var dateText = new TextBlock
            {
                Text = $"Completed: {progress.CompletedAt:MMM dd, yyyy}",
                FontSize = 14,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
            };

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(dateText);

            if (progress.Score.HasValue && progress.MaxScore.HasValue)
            {
                var scoreText = new TextBlock
                {
                    Text = $"Score: {progress.Score}/{progress.MaxScore} ({progress.Score * 100 / progress.MaxScore}%)",
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 74, 144, 226)),
                    Margin = new Thickness(0, 5, 0, 0)
                };
                stackPanel.Children.Add(scoreText);
            }
            else
            {
                var completedText = new TextBlock
                {
                    Text = "✓ Completed",
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),
                    Margin = new Thickness(0, 5, 0, 0)
                };
                stackPanel.Children.Add(completedText);
            }

            card.Child = stackPanel;
            return card;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}

