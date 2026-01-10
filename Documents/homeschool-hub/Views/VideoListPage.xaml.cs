using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HomeschoolHub.Data;
using HomeschoolHub.Models;
using System;
using System.Collections.Generic;

namespace HomeschoolHub.Views
{
    public sealed partial class VideoListPage : Page
    {
        private DatabaseContext _db = new();
        private string _category = "";
        private int _ageGroup = 0;
        private int _studentId = 0;

        public VideoListPage()
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

            LoadVideos();
        }

        private void LoadVideos()
        {
            var videos = _db.GetVideoResources(_category, _ageGroup);
            VideosStackPanel.Children.Clear();

            foreach (var video in videos)
            {
                var card = CreateVideoCard(video);
                VideosStackPanel.Children.Add(card);
            }
        }

        private Border CreateVideoCard(VideoResource video)
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
                Text = video.Title,
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };

            var descriptionText = new TextBlock
            {
                Text = video.Description,
                FontSize = 14,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var button = new Button
            {
                Content = "Watch Video",
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 243, 129, 129)),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 0),
                CornerRadius = new CornerRadius(5),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            button.Click += (s, e) => WatchVideo(video);

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(descriptionText);
            stackPanel.Children.Add(button);

            card.Child = stackPanel;
            return card;
        }

        private void WatchVideo(VideoResource video)
        {
            var parameters = new Dictionary<string, object>
            {
                { "Video", video },
                { "StudentId", _studentId }
            };
            this.Frame.Navigate(typeof(VideoPage), parameters);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}

