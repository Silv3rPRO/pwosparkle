using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PWOProtocol
{
    public class GameClient
    {
        private const string ClientVersion = "AprilFools";

        public Random Rand { get; private set; } = new Random();

        public bool IsConnected { get; private set; }
        public bool IsAuthenticated { get; private set; }

        public string PlayerName { get; private set; }
        public int PlayerX { get; private set; }
        public int PlayerY { get; private set; }
        public string MapName { get; private set; }
        public bool IsSurfing { get; private set; }
        public bool IsInside { get; private set; }
        public bool IsInBattle { get; private set; }
        public int Money { get; private set; }

        private List<Pokemon> _team;
        public ReadOnlyCollection<Pokemon> Team { get { return _team?.AsReadOnly(); } }

        private List<Npc> _npcs;
        public ReadOnlyCollection<Npc> Npcs { get { return _npcs?.AsReadOnly(); } }

        private List<InventoryItem> _inventory;
        public ReadOnlyCollection<InventoryItem> Inventory { get { return _inventory?.AsReadOnly(); } }

        private List<ChatChannel> _channels;
        public ReadOnlyCollection<ChatChannel> Channels { get { return _channels?.AsReadOnly(); } }

        public Map Map { get; private set; }
        public Battle Battle { get; private set; }

        public event Action ConnectionClosed;
        public event Action LoggedIn;
        public event Action<AuthenticationResult> AuthenticationFailed;
        public event Action<string, int, int> PositionUpdated;
        public event Action TeamUpdated;
        public event Action InventoryUpdated;
        public event Action BattleStarted;
        public event Action<string> BattleMessage;
        public event Action BattleEnded;
        public event Action<int, string> DialogMessage;
        public event Action<IList<ChatChannel>> ChannelsUpdated;
        public event Action<string, string, string> ChatMessage;
        public event Action<string, string, string, string> ChannelMessage;
        public event Action<string, string> ChannelSystemMessage;
        public event Action<string> SystemMessage;
        public event Action<string, string, string, string> PrivateMessage;
        public event Action<string, string, string> LeavePrivateMessage;

        private GameConnection _connection;
        private string _mapsServer;

        private Timeout _movementTimeout = new Timeout();
        private Direction _currentDirection = Direction.Down;
        private Queue<Direction> _movements = new Queue<Direction>();

        private Timeout _battleTimeout = new Timeout();
        private bool _sendBattleRefresh;

        private Timeout _loadMapTimeout = new Timeout();

        private Timeout _dialogTimeout = new Timeout();
        private bool _isDialogActive;
        private int _scriptId;
        private int _scriptStatus;

        private Timeout _reorderTimeout = new Timeout();

        public bool IsInactive
        {
            get { return !_movementTimeout.IsActive
                    && !_battleTimeout.IsActive
                    && !_loadMapTimeout.IsActive
                    && !_dialogTimeout.IsActive 
                    && !_reorderTimeout.IsActive; }
        }

        public bool IsInitialized
        {
            get { return Map != null; }
        }

        public GameClient(GameConnection connection)
        {
            IsConnected = true;
            _connection = connection;
        }

        public int DistanceTo(int cellX, int cellY)
        {
            return Math.Abs(PlayerX - cellX) + Math.Abs(PlayerY - cellY);
        }

        public async Task OpenAsync()
        {
            await _connection.OpenAsync();
            IsConnected = true;
            _connection.PacketReceived += OnPacketReceived;
            _connection.Closed += OnConnectionClosed;
        }

        public void Close()
        {
            _connection.Close();
        }

        public void Update()
        {
            if (!IsConnected)
                return;
            _connection.Update();
            if (!IsAuthenticated)
                return;

            UpdateMovement();
            UpdateBattle();
            UpdateDialog();

            _loadMapTimeout.Update();
            _reorderTimeout.Update();
        }

        private void UpdateMovement()
        {
            _movementTimeout.Update();

            if (!_movementTimeout.IsActive && _movements.Count > 0)
            {
                Direction direction = _movements.Dequeue();

                if (ApplyMovement(direction))
                {
                    if (_currentDirection != direction)
                    {
                        _currentDirection = direction;
                        SendMovement(direction.AsCardinalChar());
                    }
                    SendMovement(direction.AsChar());
                    _movementTimeout.Set(250);
                }
            }
        }

        private void UpdateBattle()
        {
            _battleTimeout.Update();
            if (!IsInBattle && !_battleTimeout.IsActive && _sendBattleRefresh)
            {
                _sendBattleRefresh = false;
                SendResyncRequest();
            }
        }

        private void UpdateDialog()
        {
            _dialogTimeout.Update();
            if (_isDialogActive && !_dialogTimeout.IsActive)
            {
                if (_scriptStatus == 0)
                {
                    _isDialogActive = false;
                }
                else if (_scriptStatus == 1)
                {
                    SelectDialogAnswer(0);
                }
                else if (_scriptStatus == 3)
                {
                    SelectDialogAnswer(1);
                }
            }
        }

        private bool ApplyMovement(Direction direction)
        {
            int destinationX = PlayerX;
            int destinationY = PlayerY;

            switch (direction)
            {
                case Direction.Up:
                    destinationY--;
                    break;
                case Direction.Down:
                    destinationY++;
                    break;
                case Direction.Left:
                    destinationX--;
                    break;
                case Direction.Right:
                    destinationX++;
                    break;
            }

            Map.MoveResult result = Map.CanMove(direction, destinationX, destinationY, IsSurfing, Npcs);
            bool isSuccess = false;

            switch (result)
            {
                case Map.MoveResult.Success:
                    isSuccess = true;
                    break;
                case Map.MoveResult.Jump:
                    isSuccess = true;
                    destinationY += 1;
                    break;
                case Map.MoveResult.NoLongerSurfing:
                    isSuccess = true;
                    IsSurfing = false;
                    break;
            }

            if (isSuccess)
            {
                PlayerX = destinationX;
                PlayerY = destinationY;
                PositionUpdated?.Invoke(MapName, PlayerX, PlayerY);
            }

            return isSuccess;
        }

        public async void SendPacket(string packet)
        {
#if DEBUG
            Console.WriteLine("[>] " + packet);
#endif
            await _connection.SendAsync(packet);
        }

        public void Move(Direction direction)
        {
            if (IsInBattle) return;

            _movements.Enqueue(direction);
        }

        public void Attack(int attack)
        {
            if (attack >= 1 && attack <= 4)
            {
                _battleTimeout.Set();
                SendBattleAction(attack.ToString());
            }
        }

        public void RunFromBattle()
        {
            _battleTimeout.Set();
            SendBattleAction("5");
        }

        public void ReorderPokemon(int pokemonUid, int position)
        {
            if (IsInBattle || !Team.Any((p) => p.Uid == pokemonUid) || position < 1 || position > 6)
            {
                return;
            }
            if (!_reorderTimeout.IsActive)
            {
                SendReorderPokemon(pokemonUid, position);
                _reorderTimeout.Set();
            }
        }

        private void SendReorderPokemon(int pokemonUid, int position)
        {
            SendPacket("reorder|.|" + pokemonUid + "|.|" + position);
        }

        public void TalkToNpc(int npcId)
        {
            _dialogTimeout.Set();
            SendPacket("N|.|" + npcId);
        }

        public void SelectDialogAnswer(int answer)
        {
            _dialogTimeout.Set();
            SendPacket("R|.|" + _scriptId + "|.|" + answer);
        }

        public void SwitchPokemon(int pokemonIndex)
        {
            if (pokemonIndex >= 0 && pokemonIndex < Team.Count)
            {
                _battleTimeout.Set();
                SendBattleAction((6 + pokemonIndex).ToString());
            }
        }

        public void WaitForTeleportation()
        {
            _loadMapTimeout.Set();
        }

        public void SendAuthentication(string username, string password)
        {
            SendPacket("LOG|.|" + username + "|.|" + password + "|.|" + ClientVersion + "|.|" + HardwareHash.GenerateRandom());
            _connection.ResetSecurityByte();
        }

        public void SendResyncRequest()
        {
            SendMessage("/ref");
            _loadMapTimeout.Set();
        }

        public void SendMessage(string message)
        {
            SendPacket("msg|.|" + message);
        }

        public void SendPrivateMessage(string nickname, string text)
        {
            string pmHeader = "/pm " + nickname + "-=-" + PlayerName;
            SendMessage(pmHeader + "|" + text);
        }

        private void SendMovement(string direction)
        {
            SendPacket("q|.|" + direction);
        }

        private void SendBattleAction(string action)
        {
            SendPacket("action|.|" + action);
        }

        private void OnPacketReceived(string packet)
        {
            string[] data = packet.Split(new string[] { "|.|" }, StringSplitOptions.None);
            string type = data[0].ToUpperInvariant();

#if DEBUG
            Console.WriteLine("[<] " + packet);
#endif
            switch (type)
            {
                case "CON":
                    OnMapsServer(data);
                    break;
                case "REF":
                    OnAuthenticationFailed(data);
                    break;
                case "I":
                    OnPlayerInformation(data);
                    break;
                case "Q":
                    OnPlayerPosition(data);
                    break;
                case "D":
                    OnInventoryContent(data);
                    break;
                case "MON":
                    OnPokemonTeam(data);
                    break;
                case "NPC":
                    OnNpcData(data);
                    break;
                case "SB":
                    OnBattleStart(data);
                    break;
                case "A":
                    OnBattleData(data);
                    break;
                case "C":
                    OnChannels(data);
                    break;
                case "MSG":
                    OnChatMessage(data);
                    break;
                case "PM":
                    OnPrivateMessage(data);
                    break;
                case "R":
                    OnDialogMessage(data);
                    break;
                default:
#if DEBUG
                    Console.WriteLine("^ UNHANDLED");
#endif
                    break;
            }
        }

        private void OnMapsServer(string[] data)
        {
            IsAuthenticated = true;
            _mapsServer = data[1];
            LoggedIn?.Invoke();
        }
        
        private void OnAuthenticationFailed(string[] data)
        {
            AuthenticationResult result = (AuthenticationResult)int.Parse(data[1]);
            AuthenticationFailed?.Invoke(result);
            Close();
        }

        private void OnPlayerInformation(string[] data)
        {
            string[] playerData = data[1].Split('|');
            PlayerName = playerData[0];
        }

        private void OnPlayerPosition(string[] data)
        {
            _loadMapTimeout.Set(Rand.Next(1000, 2000));

            IsAuthenticated = true;

            string map = data[2];
            string mapFile = data[3];
            int x = int.Parse(data[4]);
            int y = int.Parse(data[5]);

            if (map != MapName || x != PlayerX || y != PlayerY)
            {
                if (map != MapName)
                {
                    MapName = map;
                    LoadMap(mapFile);
                }
                else
                {
                    SendPacket("syn");
                }
                PlayerX = x;
                PlayerY = y;
                PositionUpdated?.Invoke(MapName, PlayerX, PlayerY);
            }
            IsSurfing = data[6] == "1";
            IsInside = data[7] == "1";

            _movements.Clear();
        }

        private async void LoadMap(string mapFile)
        {
            _loadMapTimeout.Set();
            Map = null;
            _npcs = null;

            MapLoader loader = new MapLoader();
            await loader.Load(_mapsServer, mapFile);
            Map = loader.Map;
            await Task.Delay(Rand.Next(500, 1000));

            SendPacket("npc");
            SendPacket("syn");

            _loadMapTimeout.Set(Rand.Next(500, 1000));
        }

        private void OnInventoryContent(string[] data)
        {
            string[] itemsData = data[3].Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Money = int.Parse(data[2]);

            _inventory = new List<InventoryItem>();
            foreach (string line in itemsData)
            {
                if (line.Length > 0)
                {
                    _inventory.Add(new InventoryItem(line));
                }
            }
            
            InventoryUpdated?.Invoke();
        }

        private void OnPokemonTeam(string[] data)
        {
            string[] teamData = data[1].Split(new string[] { "\r\n" }, StringSplitOptions.None);

            _team = new List<Pokemon>();
            foreach (string line in teamData)
            {
                if (line.Length > 0)
                {
                    _team.Add(new Pokemon(line.Split('|')));
                }
            }

            _reorderTimeout.Set(0);
            TeamUpdated?.Invoke();
        }

        private void OnNpcData(string[] data)
        {
            string[] allNpcData = data[1].Split(new string[] { "/*\\" }, StringSplitOptions.RemoveEmptyEntries);

            _npcs = new List<Npc>();
            foreach (string npcContent in allNpcData)
            {
                _npcs.Add(new Npc(npcContent));
            }
        }

        private void OnBattleStart(string[] data)
        {
            IsInBattle = true;
            Battle = new Battle(data, PlayerName);

            _movements.Clear();
            _battleTimeout.Set();

            BattleStarted?.Invoke();

            string[] messages = Battle.Message.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string message in messages)
            {
                if (!Battle.ProcessMessage(Team, message))
                {
                    BattleMessage?.Invoke(message);
                }
            }
        }

        private void OnBattleData(string[] data)
        {
            _battleTimeout.Set(Rand.Next(5000, 8000));

            string[] messages = data[5].Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string message in messages)
            {
                if (!Battle.ProcessMessage(_team, message))
                {
                    BattleMessage?.Invoke(message);
                }
            }

            TeamUpdated?.Invoke();

            if (Battle.IsFinished)
            {
                _sendBattleRefresh = true;
                IsInBattle = false;
                Battle = null;
                BattleEnded?.Invoke();
            }
        }

        private void OnChannels(string[] data)
        {
            _channels = new List<ChatChannel>();
            string[] channelsData = data[1].Split('|');
            for (int i = 1; i < channelsData.Length; i += 2)
            {
                string channelId = channelsData[i];
                string channelName = channelsData[i + 1];
                _channels.Add(new ChatChannel(channelId, channelName));
            }
            ChannelsUpdated?.Invoke(Channels);
        }

        private void OnChatMessage(string[] data)
        {
            string fullMessage = data[1];
            string[] chatData = fullMessage.Split(':');
            
            if (fullMessage.Substring(0, 7) == "*GREEN*")
            {
                fullMessage = fullMessage.Substring(7);
            }
            else if (fullMessage.Substring(0, 5) == "*RED*")
            {
                fullMessage = fullMessage.Substring(5);
            }
            else if (fullMessage.Substring(0, 6) == "*BLUE*")
            {
                fullMessage = fullMessage.Substring(6);
            }
            else if (fullMessage.Substring(0, 8) == "*YELLOW*")
            {
                fullMessage = fullMessage.Substring(8);
            }
            else if (fullMessage.Substring(0, 6) == "*CYAN*")
            {
                fullMessage = fullMessage.Substring(6);
            }
            else if (fullMessage.Substring(0, 7) == "*WHITE*")
            {
                fullMessage = fullMessage.Substring(7);
            }
            string message;
            if (chatData.Length <= 1)
            {
                string channelName;

                int start = fullMessage.IndexOf('(') + 1;
                int end = fullMessage.IndexOf(')');
                channelName = fullMessage.Substring(start, end - start);
                
                message = fullMessage.Substring(end + 2);

                ChannelSystemMessage?.Invoke(channelName, message);
                return;
            }
            if (chatData[0] != "*GREEN*System")
            {
                string channelName = null;
                string mode = null;
                string author;

                int start = (fullMessage[0] == '(' ? 1 : 0);
                int end;
                if (start != 0)
                {
                    end = fullMessage.IndexOf(')');
                    channelName = fullMessage.Substring(start, end - start);
                }
                start = fullMessage.IndexOf('[') + 1;
                if (start != 0 && fullMessage.ToCharArray()[start] != 'n')
                {
                    end = fullMessage.IndexOf(']');
                    mode = fullMessage.Substring(start, end - start);
                }
                if (channelName == "PM")
                {
                    end = fullMessage.IndexOf(':');
                    string header = fullMessage.Substring(0, end);
                    start = header.LastIndexOf(' ') + 1;
                    author = header.Substring(start);
                }
                else
                {
                    start = fullMessage[0] == '(' ? fullMessage.IndexOf(")") + 2 : 0;
                    end = fullMessage.IndexOf(":");
                    author = fullMessage.Substring(start, end - start);
                }
                start = fullMessage.IndexOf(':') + 2;
                message = fullMessage.Substring(start == 1 ? 0 : start);
                if (channelName != null)
                {
                    ChannelMessage?.Invoke(channelName, mode, author, message);
                }
                else
                {
                    ChatMessage?.Invoke(mode, author, message);
                }
                return;
            }
            int offset = fullMessage.IndexOf(':') + 2;
            message = fullMessage.Substring(offset == 1 ? 0 : offset);

            SystemMessage?.Invoke(message);
        }

        private void OnPrivateMessage(string[] data)
        {
            string[] nicknames = data[1].Split(new[] { "-=-" }, StringSplitOptions.None);
            if (nicknames.Length != 2) return;

            string conversation;
            if (nicknames[0] != PlayerName)
            {
                conversation = nicknames[0];
            }
            else
            {
                conversation = nicknames[1];
            }

            string mode = null;
            int offset = data[2].IndexOf('[') + 1;
            int end = 0;
            if (offset != 0 && offset < data[2].IndexOf(':'))
            {
                end = data[2].IndexOf(']');
                mode = data[2].Substring(offset, end - offset);
            }

            if (data[2].Substring(0, 4) == "Rem:")
            {
                LeavePrivateMessage?.Invoke(conversation, mode, data[2].Substring(4 + end));
                return;
            }

            string modeRemoved = data[2];
            if (end != 0)
            {
                modeRemoved = data[2].Substring(end + 2);
            }
            offset = modeRemoved.IndexOf(' ');
            string speaker = modeRemoved.Substring(0, offset);

            offset = data[2].IndexOf(':') + 2;
            string message = data[2].Substring(offset);

            PrivateMessage?.Invoke(conversation, mode, speaker, message);
        }

        private void OnDialogMessage(string[] data)
        {
            int status = int.Parse(data[1]);
            int id = int.Parse(data[2]);
            string script = data[3];

            if (script.Contains("-#-") && status > 1)
            {
                string[] messageData = script.Split(new string[] { "-#-" }, StringSplitOptions.None);
                script = messageData[0];
            }
            string[] messages = script.Split(new string[] { "-=-" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string message in messages)
            {
                DialogMessage?.Invoke(id, message);
            }

            _isDialogActive = true;
            _scriptId = id;
            _scriptStatus = status;
            _dialogTimeout.Set(Rand.Next(2000, 5000));
        }

        private void OnConnectionClosed()
        {
            IsConnected = false;
            ConnectionClosed?.Invoke();
        }
    }
}
