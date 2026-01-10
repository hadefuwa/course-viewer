using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HomeschoolHub.Data;
using HomeschoolHub.Models;
using System;
using System.Collections.Generic;

namespace HomeschoolHub.Views
{
    public sealed partial class QuizListPage : Page
    {
        private DatabaseContext _db = new();
        private string _category = "";
        private int _ageGroup = 0;
        private int _studentId = 0;

        public QuizListPage()
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

            LoadQuizzes();
        }

        private void LoadQuizzes()
        {
            var quizzes = _db.GetQuizzes(_category, _ageGroup);
            QuizzesStackPanel.Children.Clear();

            foreach (var quiz in quizzes)
            {
                var card = CreateQuizCard(quiz);
                QuizzesStackPanel.Children.Add(card);
            }
        }

        private Border CreateQuizCard(Quiz quiz)
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
                Text = quiz.Title,
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };

            var questionCountText = new TextBlock
            {
                Text = $"{quiz.Questions.Count} questions",
                FontSize = 14,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
            };

            var button = new Button
            {
                Content = "Start Quiz",
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 78, 205, 196)),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 0),
                CornerRadius = new CornerRadius(5),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            button.Click += (s, e) => StartQuiz(quiz);

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(questionCountText);
            stackPanel.Children.Add(button);

            card.Child = stackPanel;
            return card;
        }

        private void StartQuiz(Quiz quiz)
        {
            var parameters = new Dictionary<string, object>
            {
                { "Quiz", quiz },
                { "StudentId", _studentId }
            };
            this.Frame.Navigate(typeof(QuizPage), parameters);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}

