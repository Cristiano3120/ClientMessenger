using System.Windows;

namespace ClientMessenger
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += StartClient;
        }

        private async void StartClient(object sender, RoutedEventArgs args)
        {
            await Client.Start();
        }
    }
}