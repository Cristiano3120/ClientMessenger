﻿using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows;

namespace ClientMessenger
{
    internal static partial class ClientUI
    {
        private const int WM_SYSCOMMAND = 0x112;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_RESTORE = 0xF120;
        private const int SC_CLOSE = 0xF060;

        /// <summary>
        /// Takes the type of the window to close (<typeparamref name="TWindowToClose"/>) and the type of the window to open (<typeparamref name="TWindowToOpen"/>).
        /// It then creates a new Instance of <typeparamref name="TWindowToOpen"/> and calls .Show() on it. After that a 300ms delay follows.
        /// At last it searches <see cref="Application.Current.Windows"/> for an window of type <typeparamref name="TWindowToClose"/> and calls .Close() on it.
        /// </summary>
        /// <typeparam name="TWindowToClose"></typeparam>
        /// <typeparam name="TWindowToOpen"></typeparam>
        public static void SwitchWindows<TWindowToClose, TWindowToOpen>() where TWindowToClose : Window where TWindowToOpen : Window, new()
        {
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                new TWindowToOpen().Show();
                await Task.Delay(300);

                foreach (Window window in Application.Current.Windows)
                {
                    if (typeof(TWindowToClose) == window.GetType())
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
        public static T GetWindow<T>() where T : Window, new() =>
            Application.Current.Windows.OfType<T>().FirstOrDefault() ?? new T();

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

        #region Event_Click

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

        #endregion
    }
}
