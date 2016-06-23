using System;

namespace PWOProtocol
{
    public class Timeout
    {
        public bool IsActive { get; private set; }
        private DateTime _expirationTime;

        public bool Update()
        {
            if (IsActive && DateTime.UtcNow >= _expirationTime)
            {
                IsActive = false;
            }
            return IsActive;
        }

        public void Set()
        {
            Set(10000);
        }

        public void Set(int milliseconds)
        {
            IsActive = true;
            _expirationTime = DateTime.UtcNow.AddMilliseconds(milliseconds);
        }
    }
}
