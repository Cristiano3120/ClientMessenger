using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;

namespace ClientMessenger
{
    internal static partial class ClientUI
    {
        private const int WM_SYSCOMMAND = 0x112;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_RESTORE = 0xF120;
        private const int SC_CLOSE = 0xF060;

        public static void CloseAllWindowsExceptOne<T>()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (typeof(T) != window.GetType())
                {
                    window.Close();
                    break;
                }
            }
        }

        public static T? GetWindow<T>() where T : Window =>
            Application.Current.Windows.OfType<T>().FirstOrDefault();

        #region Control Window state

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        public static void RegisterWindowButtons(Button minimizeButton, Button maximizeButton, Button closeButton)
        {
            minimizeButton.Click += MinimizeButton_Click;
            maximizeButton.Click += MaximizeButton_Click;
            closeButton.Click += CloseButton_Click;

            minimizeButton.MouseEnter += ChangeCursorOnHover;
            maximizeButton.MouseEnter += ChangeCursorOnHover;
            closeButton.MouseEnter += ChangeCursorOnHover;
        }

        private static void ChangeCursorOnHover(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            button.Cursor = Cursors.Hand;
        }

        private static void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow((Button)sender);
            var windowHandle = new WindowInteropHelper(window).Handle;

            var wParam = window.WindowState == WindowState.Maximized
                ? SC_RESTORE
                : SC_MAXIMIZE;

            SendMessage(windowHandle, WM_SYSCOMMAND, wParam, IntPtr.Zero);
        }

        private static void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow((Button)sender);
            var windowHandle = new WindowInteropHelper(window).Handle;
            SendMessage(windowHandle, WM_SYSCOMMAND, SC_MINIMIZE, IntPtr.Zero);
        }

        private static void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow((Button)sender);
            var windowHandle = new WindowInteropHelper(window).Handle;
            SendMessage(windowHandle, WM_SYSCOMMAND, SC_CLOSE, IntPtr.Zero);
        }

        #endregion
    }
}
