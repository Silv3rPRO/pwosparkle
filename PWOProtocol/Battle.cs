using System;
using System.Collections.Generic;

namespace PWOProtocol
{
    public class Battle
    {
        public int ActiveIndex { get; private set; }
        public int OpponentPokedexId { get; private set; }
        public int OpponentLevel { get; private set; }
        public string OpponentGender { get; private set; }
        public string Message { get; private set; }

        public bool IsWild { get; private set; }

        public bool IsFinished { get; private set; }

        private string _playerName;

        public Battle(string[] data, string playerName)
        {
            _playerName = playerName;

            ActiveIndex = int.Parse(data[6]) - 1;
            OpponentPokedexId = int.Parse(data[3]);
            OpponentLevel = int.Parse(data[5]);
            Message = data[7];
            OpponentGender = data[9];

            IsWild = (data[10] == "" && data[11] == "" && data[12] == "");
        }

        public bool ProcessMessage(IList<Pokemon> team, string message)
        {
            if (message.Length == 0)
                return true;

            if (message == "END-BATTLE")
            {
                IsFinished = true;
                return true;
            }

            string[] data = message.Split(':');

            if (message.StartsWith("DAMAGE:"))
            {
                int currentHealth = int.Parse(data[2]);
                int maxHealth = int.Parse(data[3]);

                if (data[1] == _playerName)
                {
                    team[ActiveIndex].UpdateHealth(maxHealth, currentHealth);
                }
                else
                {
                    // TODO opponent health
                }
                return true;
            }

            if (message.StartsWith("CHANGE:"))
            {
                int index = Convert.ToInt32(data[2]) - 1;

                if (data[1] == _playerName)
                {
                    ActiveIndex = index;
                }
                else
                {
                }
                return true;
            }

            if (message.StartsWith("UPDATE:"))
            {
                // TODO status update, maybe more
                return true;
            }

            if (message.StartsWith("POKEMON FAINTED"))
            {
                return true;
            }

            return false;
        }
    }
}
