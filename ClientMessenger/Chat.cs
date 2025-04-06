using System.Windows.Controls;

namespace ClientMessenger
{
    public class Chat(ScrollViewer ChatPanel, DateTime LastOpend)
    {
        public ScrollViewer ScrollViewer { get; init; } = ChatPanel;
        public DateTime LastOpend { get; set; } = LastOpend;
        public double LastScrollViewerVerticalOffset { get; set; }
        public Message LastMessage { get; set; }
        public int MessageCount { get; set; }
    }
}
