using System.Windows;

namespace ClientMessenger
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += StartClientAsync;
        }

        private async void StartClientAsync(object sender, RoutedEventArgs args)
        {
            await Client.StartAsync();
        }
    }
}