using System.Windows;

namespace ClientMessenger
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _ = Client.Start();
        }
    }
}