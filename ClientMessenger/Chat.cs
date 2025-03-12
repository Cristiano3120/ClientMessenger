using System.Windows.Controls;

namespace ClientMessenger
{
    public record Chat(ScrollViewer ChatPanel, DateTime LastOpend)
    {
        public ScrollViewer ChatPanel { get; init; } = ChatPanel;
        public DateTime LastOpend { get; set; } = LastOpend;
    }
}
