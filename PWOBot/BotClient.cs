using PWOProtocol;
using System;
using System.IO;
using System.Text;

namespace PWOBot
{
    public class BotClient
    {
        public GameClient Game { get; private set; }
        public BattleAI AI { get; private set; }
        public Script Script { get; private set; }

        public bool Running { get; private set; }

        public event Action<bool> StateChanged;
        public event Action<string> LogMessage;

        private Timeout _botTimeout = new Timeout();
        private int _lastMovementSourceX;
        private int _lastMovementSourceY;
        private int _lastMovementDestinationX;
        private int _lastMovementDestinationY;
        private bool _requestedResync;

        public void Update()
        {
            if (!Running)
            {
                return;
            }

            if (Game.IsAuthenticated && Game.IsInitialized && Game.IsInactive)
            {
                ExecuteNextAction();
            }
        }

        public void Start()
        {
            if (Game != null && !Running && Script != null)
            {
                Running = true;
                StateChanged?.Invoke(Running);
            }
        }

        public void Stop()
        {
            if (Running)
            {
                Running = false;
                StateChanged?.Invoke(Running);
            }
        }

        public void SetClient(GameClient client)
        {
            Game = client;
            AI = null;
            Stop();

            if (client != null)
            {
                AI = new BattleAI(client);
                client.BattleEnded += Client_BattleEnded;
            }
        }

        public void LoadScript(string filename)
        {
            string oldDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = new FileInfo(filename).Directory.FullName;
            Script = new Script(this, File.ReadAllText(filename, Encoding.UTF8));
            Script.ScriptMessage += Script_ScriptMessage;
            Script.Initialize();
            Environment.CurrentDirectory = oldDirectory;
        }

        public bool MoveToCell(int x, int y, int requiredDistance = 0)
        {
            if (_lastMovementSourceX == Game.PlayerX && _lastMovementSourceY == Game.PlayerY
                && _lastMovementDestinationX == x && _lastMovementDestinationY == y)
            {
                if (_requestedResync)
                {
                    LogMessage?.Invoke("Bot stuck: stopping the script");
                    Stop();
                }
                else
                {
                    LogMessage?.Invoke("Bot stuck: requesting synchronization");
                    _requestedResync = true;
                    Game.SendResyncRequest();
                }
                return false;
            }

            Pathfinding path = new Pathfinding(Game);
            bool result;

            if (Game.PlayerX == x && Game.PlayerY == y)
            {
                result = path.MoveToSameCell();
            }
            else
            {
                result = path.MoveTo(x, y, requiredDistance);
            }

            if (result)
            {
                _lastMovementSourceX = Game.PlayerX;
                _lastMovementSourceY = Game.PlayerY;
                _lastMovementDestinationX = x;
                _lastMovementDestinationY = y;
                _requestedResync = false;
            }

            return result;
        }

        private void ExecuteNextAction()
        {
            bool executed = false;
            if (Game.IsInBattle)
            {
                executed = Script.ExecuteBattleAction();
            }
            else
            {
                executed = Script.ExecutePathAction();
            }
            _botTimeout.Update();
            if (!executed && !_botTimeout.IsActive)
            {
                LogMessage?.Invoke("No action executed: stopping the bot");
                Stop();
            }
            else
            {
                _botTimeout.Set();
            }
        }

        private void Script_ScriptMessage(string message)
        {
            LogMessage?.Invoke(message);
        }

        private void Client_BattleEnded()
        {
            ResetResync();
        }

        private void ResetResync()
        {
            _requestedResync = false;
            _lastMovementSourceX = -1;
        }
    }
}
