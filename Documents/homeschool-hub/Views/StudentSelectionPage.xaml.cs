using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HomeschoolHub.Data;
using HomeschoolHub.Models;
using System;

namespace HomeschoolHub.Views
{
    public sealed partial class StudentSelectionPage : Page
    {
        private DatabaseContext _db = new();

        public StudentSelectionPage()
        {
            this.InitializeComponent();
            LoadStudents();
        }

        private void LoadStudents()
        {
            var students = _db.GetStudents();
            StudentsListView.ItemsSource = students;
        }

        private void CreateStudentButton_Click(object sender, RoutedEventArgs e)
        {
            var name = StudentNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ShowMessage("Please enter a student name.");
                return;
            }

            if (!int.TryParse(StudentAgeTextBox.Text, out int age) || age < 3 || age > 18)
            {
                ShowMessage("Please enter a valid age (3-18).");
                return;
            }

            var student = new Student
            {
                Name = name,
                Age = age,
                CreatedAt = DateTime.Now
            };

            _db.AddStudent(student);
            LoadStudents();
            
            StudentNameTextBox.Text = "";
            StudentAgeTextBox.Text = "7";
            
            ShowMessage($"Student {name} created successfully!");
        }

        private void StudentsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StudentsListView.SelectedItem is Student selectedStudent)
            {
                // Navigate back to main page - it will load this student
                this.Frame.Navigate(typeof(MainPage));
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private async void ShowMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Homeschool Hub",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}

