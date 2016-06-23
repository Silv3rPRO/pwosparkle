using PWOProtocol;

namespace PWOBot
{
    public class BattleAI
    {
        private GameClient _client;

        public BattleAI(GameClient client)
        {
            _client = client;
        }

        public int UsablePokemonsCount
        {
            get
            {
                int usablePokemons = 0;
                foreach (Pokemon pokemon in _client.Team)
                {
                    if (IsPokemonUsable(pokemon))
                    {
                        usablePokemons += 1;
                    }
                }
                return usablePokemons;
            }
        }

        public Pokemon ActivePokemon
        {
            get
            {
                return _client.Team[_client.Battle.ActiveIndex];
            }
        }

        public bool Attack(bool useBestAttack = true)
        {
            if (UsePreconditions())
            {
                return true;
            }
            if (IsPokemonUsable(ActivePokemon) && UseAttack(useBestAttack))
            {
                return true;
            }
            if (SendNextPokemon())
            {
                return true;
            }
            if (_client.Battle.IsWild && Run())
            {
                return true;
            }
            return false;
        }

        public bool UseMove(string moveName)
        {
            if (UsePreconditions())
            {
                return true;
            }
            for (int i = 0; i < ActivePokemon.Moves.Length; ++i)
            {
                PokemonMove move = ActivePokemon.Moves[i];
                if (move.CurrentPoints > 0)
                {
                    if (move.Name.ToUpperInvariant() == moveName)
                    {
                        _client.Attack(i + 1);
                        return true;
                    }
                }
            }
            return Attack();
        }

        public bool Run()
        {
            if (UsePreconditions())
            {
                return true;
            }
            if (!_client.Battle.IsWild)
            {
                return Attack();
            }
            _client.RunFromBattle();
            return true;
        }

        private bool UsePreconditions()
        {
            if (ActivePokemon.CurrentHealth == 0)
            {
                return SendNextPokemon();
            }
            return false;
        }

        private bool UseAttack(bool useBestAttack)
        {
            PokemonMove bestMove = null;
            int bestIndex = 0;
            double bestPower = 0;

            PokemonMove worstMove = null;
            int worstIndex = 0;
            double worstPower = 0;

            for (int i = 0; i < ActivePokemon.Moves.Length; ++i)
            {
                PokemonMove move = ActivePokemon.Moves[i];
                if (move.CurrentPoints > 0)
                {
                    if (IsMoveOffensive(move))
                    {
                        PokemonType attackType = move.Type;

                        PokemonType playerType1 = TypesManager.Instance.Type1[ActivePokemon.PokedexId];
                        PokemonType playerType2 = TypesManager.Instance.Type2[ActivePokemon.PokedexId];

                        PokemonType opponentType1 = TypesManager.Instance.Type1[_client.Battle.OpponentPokedexId];
                        PokemonType opponentType2 = TypesManager.Instance.Type2[_client.Battle.OpponentPokedexId];

                        double accuracy = (move.Accuracy < 0 ? 101.0 : move.Accuracy);

                        double power = move.Power * accuracy;

                        if (attackType == playerType1 || attackType == playerType2)
                        {
                            power *= 1.5;
                        }

                        power *= TypesManager.Instance.GetMultiplier(attackType, opponentType1);
                        power *= TypesManager.Instance.GetMultiplier(attackType, opponentType2);
                        
                        if (power > 0)
                        {
                            if (bestMove == null || power > bestPower)
                            {
                                bestMove = move;
                                bestPower = power;
                                bestIndex = i;
                            }

                            if (worstMove == null || power < worstPower)
                            {
                                worstMove = move;
                                worstPower = power;
                                worstIndex = i;
                            }
                        }
                    }
                }
            }

            if (useBestAttack && bestMove != null)
            {
                _client.Attack(bestIndex + 1);
                return true;
            }
            if (!useBestAttack && worstMove != null)
            {
                _client.Attack(worstIndex + 1);
                return true;
            }
            return false;
        }

        public bool SendNextPokemon()
        {
            int i = 0;
            foreach (Pokemon pokemon in _client.Team)
            {
                if (IsPokemonUsable(pokemon) && pokemon != ActivePokemon)
                {
                    _client.SwitchPokemon(i);
                    return true;
                }
                ++i;
            }
            return false;
        }

        public bool IsPokemonUsable(Pokemon pokemon)
        {
            if (pokemon.CurrentHealth > 0)
            {
                foreach (PokemonMove move in pokemon.Moves)
                {
                    if (move.CurrentPoints > 0 && IsMoveOffensive(move))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsMoveOffensive(PokemonMove move)
        {
            return move.Power > 0;
        }
    }
}
