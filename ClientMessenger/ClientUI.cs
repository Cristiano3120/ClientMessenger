using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows;

namespace ClientMessenger
{
    /// <summary>
    /// Methods of this class must be called from the UI thread.
    /// </summary>
    internal static partial class ClientUI
    {
    #pragma warning disable
        private const int WM_SYSCOMMAND = 0x112;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_RESTORE = 0xF120;
        private const int SC_CLOSE = 0xF060;
    #pragma warning restore

        public static string GetDefaultProfilPic()
            => Client.GetDynamicPath(@"Images/profilPic.png");


        /// <summary>
        /// Takes the type of the window to close (<typeparamref name="TWindowToClose"/>) and the type of the window to open (<typeparamref name="TWindowToOpen"/>).
        /// It then creates a new Instance of <typeparamref name="TWindowToOpen"/> and calls .Show() on it. After that a 300ms delay follows.
        /// At last it searches <see cref="Application.Current.Windows"/> for an window of type <typeparamref name="TWindowToClose"/> and calls .Close() on it.
        /// <typeparamref name="TWindowToClose"/> has to be of type <see cref="Window"/> if you give it the value <see cref="Window"/> all open Windows will be closed.
        /// </summary>
        /// <typeparam name="TWindowToClose"> If this has the value <see cref="Window"/> all open windows will be closed</typeparam>
        /// <typeparam name="TWindowToOpen"></typeparam>
        public static void SwitchWindows<TWindowToClose, TWindowToOpen>() where TWindowToClose : Window where TWindowToOpen : Window, new()
        {
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                //Delay so the switch looks smoother
                const ushort windowSwitchDelay = 300;

                new TWindowToOpen().Show();
                await Task.Delay(windowSwitchDelay);

                foreach (Window window in Application.Current.Windows)
                {
                    if (typeof(TWindowToClose).IsAssignableFrom(window.GetType()))
                    {
                        window.Close();
                        return;
                    }
                }
            });
        }

        /// <summary>
        /// Searches for the window and returns it if found. 
        /// If it doesn´t find anything it creates a new Instance of <typeparamref name="T"/>
        /// </summary>
        /// <returns>The window of type <typeparamref name="T"/></returns>
        public static T GetWindow<T>() where T : Window, new()
            => Application.Current.Dispatcher.Invoke(() => Application.Current.Windows.OfType<T>().FirstOrDefault() ?? new T());

        /// <summary>
        /// Checks if the text being pasted from the clipboard exceeds the specified character limit.
        /// If the pasted content exceeds the limit, the paste operation is canceled.
        /// </summary>
        /// <param name="textBox">The <see cref="TextBox"/> where the paste operation is occurring.</param>
        /// <param name="maxChars">The maximum allowed character count for the <see cref="TextBox"/> content after pasting.</param>
        public static void RestrictClipboardPasting(TextBox textBox, byte maxChars)
        {
            DataObject.AddPastingHandler(textBox, (_, args) =>
            {
                string clipboardText = Clipboard.GetText();
                int availableChars = maxChars - textBox.Text.Length + textBox.SelectedText.Length;

                if (availableChars <= 0)
                {
                    args.CancelCommand();
                    return;
                }

                if (clipboardText.Length > availableChars)
                {
                    clipboardText = clipboardText[..availableChars];
                }

                int selectionStart = textBox.SelectionStart;
                textBox.SelectedText = clipboardText;
                args.CancelCommand();

                textBox.SelectionStart = selectionStart + clipboardText.Length;
                textBox.SelectionLength = 0;
            });
        }

        #region Control Window state

        [LibraryImport("user32.dll", EntryPoint = "SendMessageW", StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public static void RegisterWindowButtons(Button minimizeButton, Button maximizeButton, Button closeButton)
        {
            minimizeButton.Click += MinimizeButton_Click;
            maximizeButton.Click += MaximizeButton_Click;
            closeButton.Click += CloseButton_Click;
        }

        private static void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow((Button)sender);
            nint windowHandle = new WindowInteropHelper(window).Handle;

            int wParam = window.WindowState == WindowState.Maximized
                ? SC_RESTORE
                : SC_MAXIMIZE;

            SendMessage(windowHandle, WM_SYSCOMMAND, wParam, IntPtr.Zero);
        }

        private static void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow((Button)sender);
            nint windowHandle = new WindowInteropHelper(window).Handle;
            SendMessage(windowHandle, WM_SYSCOMMAND, SC_MINIMIZE, IntPtr.Zero);
        }

        private static void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow((Button)sender);
            nint windowHandle = new WindowInteropHelper(window).Handle;
            SendMessage(windowHandle, WM_SYSCOMMAND, SC_CLOSE, IntPtr.Zero);
        }

        #endregion
    }
}
