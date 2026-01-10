using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using HomeschoolHub.Data;
using HomeschoolHub.Models;
using System;
using System.Collections.Generic;

namespace HomeschoolHub.Views
{
    public sealed partial class QuizPage : Page
    {
        private DatabaseContext _db = new();
        private Quiz? _quiz;
        private int _studentId = 0;
        private int _currentQuestionIndex = 0;
        private Dictionary<int, int> _answers = new();

        public QuizPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is Dictionary<string, object> parameters)
            {
                _quiz = parameters.ContainsKey("Quiz") ? parameters["Quiz"] as Quiz : null;
                _studentId = parameters.ContainsKey("StudentId") ? Convert.ToInt32(parameters["StudentId"]) : 0;
            }

            if (_quiz == null)
            {
                this.Frame.GoBack();
                return;
            }

            QuizTitleText.Text = _quiz.Title;
            LoadQuestion();
        }

        private void LoadQuestion()
        {
            if (_quiz == null || _currentQuestionIndex >= _quiz.Questions.Count)
                return;

            QuestionStackPanel.Children.Clear();

            var question = _quiz.Questions[_currentQuestionIndex];
            QuestionNumberText.Text = $"Question {_currentQuestionIndex + 1} of {_quiz.Questions.Count}";

            var questionText = new TextBlock
            {
                Text = question.QuestionText,
                FontSize = 22,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
            };
            QuestionStackPanel.Children.Add(questionText);

            var radioGroup = new StackPanel { Spacing = 10 };

            for (int i = 0; i < question.Options.Count; i++)
            {
                var radioButton = new RadioButton
                {
                    Content = question.Options[i],
                    FontSize = 18,
                    Padding = new Thickness(10),
                    Tag = i
                };

                if (_answers.ContainsKey(_currentQuestionIndex) && _answers[_currentQuestionIndex] == i)
                {
                    radioButton.IsChecked = true;
                }

                radioButton.Checked += (s, e) =>
                {
                    _answers[_currentQuestionIndex] = (int)radioButton.Tag;
                };

                radioGroup.Children.Add(radioButton);
            }

            QuestionStackPanel.Children.Add(radioGroup);

            // Update navigation buttons
            PreviousButton.IsEnabled = _currentQuestionIndex > 0;
            
            if (_currentQuestionIndex == _quiz.Questions.Count - 1)
            {
                NextButton.Visibility = Visibility.Collapsed;
                SubmitButton.Visibility = Visibility.Visible;
            }
            else
            {
                NextButton.Visibility = Visibility.Visible;
                SubmitButton.Visibility = Visibility.Collapsed;
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentQuestionIndex > 0)
            {
                _currentQuestionIndex--;
                LoadQuestion();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_quiz != null && _currentQuestionIndex < _quiz.Questions.Count - 1)
            {
                _currentQuestionIndex++;
                LoadQuestion();
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_quiz == null) return;

            // Check if all questions are answered
            if (_answers.Count < _quiz.Questions.Count)
            {
                var dialog = new ContentDialog
                {
                    Title = "Not Finished",
                    Content = "Please answer all questions before submitting.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            // Calculate score
            int score = 0;
            for (int i = 0; i < _quiz.Questions.Count; i++)
            {
                if (_answers.ContainsKey(i) && _answers[i] == _quiz.Questions[i].CorrectAnswerIndex)
                {
                    score++;
                }
            }

            // Save progress
            var progress = new Progress
            {
                StudentId = _studentId,
                QuizId = _quiz.Id,
                Completed = true,
                Score = score,
                MaxScore = _quiz.Questions.Count,
                CompletedAt = DateTime.Now
            };

            _db.SaveProgress(progress);

            // Show results
            var resultDialog = new ContentDialog
            {
                Title = "Quiz Complete!",
                Content = $"You scored {score} out of {_quiz.Questions.Count}!\n\nThat's {score * 100 / _quiz.Questions.Count}%",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await resultDialog.ShowAsync();

            this.Frame.GoBack();
        }
    }
}

