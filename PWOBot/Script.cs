using NLua;
using PWOProtocol;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PWOBot
{
    public class Script
    {
        public BotClient Bot { get; private set; }
        public string Name { get; private set; }
        public string Author { get; private set; }
        public string Description { get; private set; }

        public event Action<string> ScriptMessage;

        private Lua _lua;
        private string _content;
        private bool _actionExecuted;

        public Script(BotClient bot, string content)
        {
            Bot = bot;
            _content = content;
        }

        public void Initialize()
        {
            CreateLuaInstance();

            try
            {
                Name = _lua.GetString("name");
            }
            catch (Exception)
            {
                Name = "";
            }
            try
            {
                Author = _lua.GetString("author");
            }
            catch (Exception)
            {
                Author = "";
            }
            try
            {
                Description = _lua.GetString("description");
            }
            catch (Exception)
            {
                Description = "";
            }
        }

        private void CreateLuaInstance()
        {
            _lua = new Lua();
            _lua.DoString("import = function () end");
            _lua.DoString("io = nil");
            _lua.DoString("file = nil");
            _lua.DoString("os = nil");
            _lua.GetFunction("math.randomseed").Call(Environment.TickCount);

            _lua["log"] = new Action<string>(Log);
            _lua["fatal"] = new Action<string>(Fatal);

            CallLuaScript(_content, 2000);

            _lua["getMapName"] = new Func<string>(GetMapName);
            _lua["getPlayerX"] = new Func<int>(GetPlayerX);
            _lua["getPlayerY"] = new Func<int>(GetPlayerY);
            _lua["getTeamSize"] = new Func<int>(GetTeamSize);
            _lua["getPokemonHealth"] = new Func<int, int>(GetPokemonHealth);
            _lua["moveToCell"] = new Func<int, int, bool>(MoveToCell);
            _lua["moveToRectangle"] = new Func<int, int, int, int, bool>(MoveToRectangle);
            _lua["talkToNpcOnCell"] = new Func<int, int, bool>(TalkToNpcOnCell);
            _lua["isNpcOnCell"] = new Func<int, int, bool>(IsNpcOnCell);
            _lua["waitForTeleportation"] = new Action(WaitForTeleportation);
            _lua["attack"] = new Func<bool>(Attack);
        }

        public bool ExecutePathAction()
        {
            _actionExecuted = false;
            using (LuaFunction function = _lua.GetFunction("onPathAction"))
            {
                if (function != null)
                {
                    CallLuaFunction(function, 2000);
                }
            }
            return _actionExecuted;
        }

        public bool ExecuteBattleAction()
        {
            _actionExecuted = false;
            using (LuaFunction function = _lua.GetFunction("onBattleAction"))
            {
                if (function != null)
                {
                    CallLuaFunction(function, 2000);
                }
            }
            return _actionExecuted;
        }

        private void CallLuaScript(string script, int timeout)
        {
            CallLuaActionWithTimeout(() => _lua.DoString(script), delegate
            {
                throw new Exception("the execution of the script timed out");
            }, timeout);
        }

        private void CallLuaFunction(LuaFunction function, int timeout)
        {
            CallLuaActionWithTimeout(() => function.Call(), delegate
            {
                Fatal("error: the execution of the script timed out");
                CreateLuaInstance();
            }, timeout);
        }

        private void CallLuaActionWithTimeout(Action action, Action error, int timeout)
        {
            CancellationTokenSource cancelToken = new CancellationTokenSource();
            CancellationToken token = cancelToken.Token;
            Task<Exception> task = Task.Run(delegate
            {
                try
                {
                    Thread thread = Thread.CurrentThread;
                    using (token.Register(thread.Abort))
                    {
                        action();
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }, token);
            int index = Task.WaitAny(task, Task.Delay(timeout));
            if (index != 0)
            {
                cancelToken.Cancel();
                error();
            }
            else if (task.Result != null)
            {
                throw task.Result;
            }
        }

        // API: Display the specified message to the message log.
        private void Log(string message)
        {
            ScriptMessage?.Invoke(message);
        }

        // API: Display the specified message to the message log and stop the bot.
        private void Fatal(string message)
        {
            ScriptMessage?.Invoke(message);
            Bot.Stop();
        }
        
        // API: Return the current map name, like "Viridian City" or "Pokecenter A".
        private string GetMapName()
        {
            return Bot.Game.MapName;
        }

        private int GetPlayerX()
        {
            return Bot.Game.PlayerX;
        }

        private int GetPlayerY()
        {
            return Bot.Game.PlayerY;
        }

        private int GetTeamSize()
        {
            return Bot.Game.Team.Count;
        }

        private int GetPokemonHealth(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonHealth: tried to retrieve the non-existing pokemon " + index);
                return 0;
            }
            return Bot.Game.Team[index - 1].CurrentHealth;
        }

        private bool MoveToCell(int x, int y)
        {
            if (!CheckAction(false)) return false;
            return ExecuteAction(Bot.MoveToCell(x, y));
        }

        private bool MoveToRectangle(int minX, int minY, int maxX, int maxY)
        {
            if (!CheckAction(false)) return false;

            if (minX > maxX || minY > maxY)
            {
                Fatal("error: moveToRectangle: the maximum cell cannot be less than the minimum cell");
                return false;
            }

            int tries = 0;
            int x, y, collider;
            do
            {
                if (++tries == 100) return false;
                x = Bot.Game.Rand.Next(minX, maxX);
                y = Bot.Game.Rand.Next(minY, maxY);
                collider = Bot.Game.Map.GetCollider(x, y);
            } while ((Bot.Game.PlayerX == x && Bot.Game.PlayerY == y) || (collider != 2 && collider != 4));

            return ExecuteAction(Bot.MoveToCell(x, y));
        }

        private void WaitForTeleportation()
        {
            Bot.Game.WaitForTeleportation();
        }

        private bool TalkToNpcOnCell(int x, int y)
        {
            if (!CheckAction(false)) return false;

            Npc target = Bot.Game.Npcs.FirstOrDefault(npc => npc.X == x && npc.Y == y);

            if (target == null)
            {
                Fatal("error: talkToNpcOnCell: could not find any NPC at location [" + x + ", " + y + "]");
                return false;
            }

            if (Bot.Game.DistanceTo(x, y) <= 1)
            {
                Bot.Game.TalkToNpc(target.Id);
                return ExecuteAction(true);
            }
            else
            {
                return ExecuteAction(Bot.MoveToCell(x, y, 1));
            }
        }

        private bool IsNpcOnCell(int x, int y)
        {
            return Bot.Game.Npcs.Any(npc => npc.X == x && npc.Y == y);
        }

        private bool Attack()
        {
            if (!CheckAction(true)) return false;
            return ExecuteAction(Bot.AI.Attack());
        }

        private bool CheckAction(bool inBattle)
        {
            if (_actionExecuted)
            {
                Fatal("error: you can only execute one action per frame");
                return false;
            }
            if (Bot.Game.IsInBattle != inBattle)
            {
                if (inBattle)
                {
                    Fatal("error: you cannot execute a battle action while not in a battle");
                }
                else
                {
                    Fatal("error: you cannot execute a path action while in a battle");
                }
                return false;
            }
            return true;
        }

        private bool ExecuteAction(bool result)
        {
            if (result)
            {
                _actionExecuted = true;
            }
            return result;
        }
    }
}
