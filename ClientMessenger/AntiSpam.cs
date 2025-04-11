namespace ClientMessenger
{
    internal static class AntiSpam
    {
        private static DateTime _lastInputTime = DateTime.MinValue;

        public static bool CheckIfCanSendData(float cooldown, out TimeSpan timeToWait)
        {
            timeToWait = TimeSpan.Zero;

            if (_lastInputTime == DateTime.MinValue)
            {
                _lastInputTime = DateTime.Now;
                return true;
            }

            TimeSpan interval = DateTime.Now - _lastInputTime;
            if (interval > TimeSpan.FromSeconds(cooldown))
            {
                _lastInputTime = DateTime.Now;
                return true;
            }

            timeToWait = TimeSpan.FromSeconds(cooldown) - interval;
            return false;
        }
    }
}
