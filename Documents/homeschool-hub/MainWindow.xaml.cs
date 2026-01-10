using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HomeschoolHub.Views;

namespace HomeschoolHub
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "Homeschool Hub";
            
            // Navigate to main page
            ContentFrame.Navigate(typeof(MainPage));
        }
    }
}

