using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HomeschoolHub.Data;
using HomeschoolHub.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;

namespace HomeschoolHub.Views
{
    public sealed partial class MainPage : Page
    {
        private DatabaseContext _db = new();
        private Student? _currentStudent;

        public MainPage()
        {
            this.InitializeComponent();
            LoadCurrentStudent();
        }

        private void LoadCurrentStudent()
        {
            var students = _db.GetStudents();
            if (students.Count > 0)
            {
                _currentStudent = students[0];
                StudentNameText.Text = $"Hello, {_currentStudent.Name}!";
            }
            else
            {
                StudentNameText.Text = "Please select a student";
                // Navigate to student selection
                this.Frame.Navigate(typeof(StudentSelectionPage));
            }
        }

        private void MathsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStudent == null)
            {
                ShowStudentRequiredMessage();
                return;
            }
            var parameters = new Dictionary<string, object>
            {
                { "Category", "Maths" },
                { "AgeGroup", 4 },
                { "StudentId", _currentStudent.Id }
            };
            this.Frame.Navigate(typeof(LessonViewPage), parameters);
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStudent == null)
            {
                ShowStudentRequiredMessage();
                return;
            }
            var parameters = new Dictionary<string, object>
            {
                { "Category", "History" },
                { "AgeGroup", 7 },
                { "StudentId", _currentStudent.Id }
            };
            this.Frame.Navigate(typeof(QuizListPage), parameters);
        }

        private void ArduinoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStudent == null)
            {
                ShowStudentRequiredMessage();
                return;
            }
            var parameters = new Dictionary<string, object>
            {
                { "Category", "Arduino" },
                { "AgeGroup", 8 },
                { "StudentId", _currentStudent.Id }
            };
            this.Frame.Navigate(typeof(LessonViewPage), parameters);
        }

        private void Fusion360Button_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStudent == null)
            {
                ShowStudentRequiredMessage();
                return;
            }
            var parameters = new Dictionary<string, object>
            {
                { "Category", "Fusion360" },
                { "AgeGroup", 10 },
                { "StudentId", _currentStudent.Id }
            };
            this.Frame.Navigate(typeof(VideoListPage), parameters);
        }

        private void ProgressButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStudent == null)
            {
                ShowStudentRequiredMessage();
                return;
            }
            this.Frame.Navigate(typeof(ProgressPage), _currentStudent.Id);
        }

        private void StudentButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(StudentSelectionPage));
        }

        private async void ShowStudentRequiredMessage()
        {
            var dialog = new ContentDialog
            {
                Title = "Student Required",
                Content = "Please select or create a student profile first.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}

