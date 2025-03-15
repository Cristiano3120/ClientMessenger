using System.Windows.Controls;

namespace ClientMessenger
{
    public class Chat(ScrollViewer ChatPanel, DateTime LastOpend)
    {
        public ScrollViewer ChatPanel { get; init; } = ChatPanel;
        public DateTime LastOpend { get; set; } = LastOpend;
    }
}
