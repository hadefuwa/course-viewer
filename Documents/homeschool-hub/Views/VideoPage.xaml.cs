using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HomeschoolHub.Models;
using System;
using System.Collections.Generic;

namespace HomeschoolHub.Views
{
    public sealed partial class VideoPage : Page
    {
        private VideoResource? _video;
        private int _studentId = 0;

        public VideoPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is Dictionary<string, object> parameters)
            {
                _video = parameters.ContainsKey("Video") ? parameters["Video"] as VideoResource : null;
                _studentId = parameters.ContainsKey("StudentId") ? Convert.ToInt32(parameters["StudentId"]) : 0;
            }

            if (_video == null)
            {
                this.Frame.GoBack();
                return;
            }

            VideoTitleText.Text = _video.Title;
            VideoDescriptionText.Text = _video.Description;

            // Load YouTube embed
            await LoadYouTubeVideo(_video.YouTubeVideoId);
        }

        private async System.Threading.Tasks.Task LoadYouTubeVideo(string videoId)
        {
            try
            {
                // Create WebView2 programmatically
                var webView = new Microsoft.UI.Xaml.Controls.WebView2();
                VideoContainer.Child = webView;
                
                await webView.EnsureCoreWebView2Async();
                
                // Create YouTube embed HTML
                var embedHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            margin: 0;
            padding: 0;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            background-color: #000;
        }}
        .video-container {{
            position: relative;
            width: 100%;
            height: 100%;
            max-width: 100%;
            max-height: 100%;
        }}
        iframe {{
            width: 100%;
            height: 100%;
            border: none;
        }}
    </style>
</head>
<body>
    <div class=""video-container"">
        <iframe 
            src=""https://www.youtube.com/embed/{videoId}?autoplay=0&rel=0""
            frameborder=""0""
            allow=""accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture""
            allowfullscreen>
        </iframe>
    </div>
</body>
</html>";

                webView.NavigateToString(embedHtml);
            }
            catch (Exception ex)
            {
                // Fallback: show video link
                var errorHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            margin: 0;
            padding: 20px;
            font-family: Arial, sans-serif;
            background-color: #f0f0f0;
        }}
        a {{
            color: #0066cc;
            text-decoration: none;
            font-size: 18px;
        }}
    </style>
</head>
<body>
    <p>Unable to load video player.</p>
    <p><a href=""https://www.youtube.com/watch?v={videoId}"" target=""_blank"">Click here to watch on YouTube</a></p>
</body>
</html>";
                var webView = new Microsoft.UI.Xaml.Controls.WebView2();
                VideoContainer.Child = webView;
                await webView.EnsureCoreWebView2Async();
                webView.NavigateToString(errorHtml);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}

