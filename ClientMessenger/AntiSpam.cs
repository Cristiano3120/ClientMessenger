namespace ClientMessenger
{
    internal static class AntiSpam
    {
        private static DateTime _lastInputTime = DateTime.MinValue;

        /// <summary>
        /// Checks if the client is allowed to send another payload or if they need to wait.
        /// </summary>
        /// <param name="timeToWait">
        /// The remaining <see cref="TimeSpan"/> the client needs to wait before they can send another payload.
        /// If the client is allowed to send <c>timeToWait</c> will be <see cref="TimeSpan.Zero"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the client can send another payload or <c>false</c> if they need to wait.
        /// </returns>
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
