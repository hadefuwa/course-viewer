using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HomeschoolHub.Data;
using HomeschoolHub.Models;
using System;
using System.Collections.Generic;

namespace HomeschoolHub.Views
{
    public sealed partial class LessonViewPage : Page
    {
        private DatabaseContext _db = new();
        private string _category = "";
        private int _ageGroup = 0;
        private int _studentId = 0;

        public LessonViewPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is Dictionary<string, object> parameters)
            {
                _category = parameters.ContainsKey("Category") ? parameters["Category"].ToString() ?? "" : "";
                _ageGroup = parameters.ContainsKey("AgeGroup") ? Convert.ToInt32(parameters["AgeGroup"]) : 0;
                _studentId = parameters.ContainsKey("StudentId") ? Convert.ToInt32(parameters["StudentId"]) : 0;
            }

            CategoryTitle.Text = $"{_category} Lessons";
            LoadLessons();
        }

        private void LoadLessons()
        {
            var lessons = _db.GetLessons(_category, _ageGroup);
            LessonsStackPanel.Children.Clear();

            foreach (var lesson in lessons)
            {
                var card = CreateLessonCard(lesson);
                LessonsStackPanel.Children.Add(card);
            }
        }

        private Border CreateLessonCard(Lesson lesson)
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

            var titleText = new TextBlock
            {
                Text = lesson.Title,
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };

            var contentText = new TextBlock
            {
                Text = lesson.Content,
                FontSize = 16,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var button = new Button
            {
                Content = "Mark as Complete",
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 74, 144, 226)),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 0),
                CornerRadius = new CornerRadius(5),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            button.Click += (s, e) => MarkLessonComplete(lesson);

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(contentText);
            stackPanel.Children.Add(button);

            card.Child = stackPanel;
            return card;
        }

        private async void MarkLessonComplete(Lesson lesson)
        {
            var progress = new Progress
            {
                StudentId = _studentId,
                LessonId = lesson.Id,
                Completed = true,
                CompletedAt = DateTime.Now
            };

            _db.SaveProgress(progress);

            var dialog = new ContentDialog
            {
                Title = "Great Job!",
                Content = $"You completed: {lesson.Title}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}

