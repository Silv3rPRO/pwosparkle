using System;
using System.Text;

namespace PWOProtocol
{
    public class HardwareHash
    {
        public static string GenerateRandom()
        {
            StringBuilder mac = new StringBuilder();
            Random random = new Random();
            for (int i = 0; i < 6; ++i)
            {
                if (i != 0)
                {
                    mac.Append(':');
                }
                mac.Append(random.Next(255).ToString("X2"));
            }
            return mac.ToString();
        }
    }
}
