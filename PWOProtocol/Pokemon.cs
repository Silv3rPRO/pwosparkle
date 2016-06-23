namespace PWOProtocol
{
    public class Pokemon
    {
        public int Uid { get; private set; }
        public int PokedexId { get; private set; }
        public int Level { get; private set; }
        public int CurrentExperience { get; private set; }
        public int RequiredExperience { get; private set; }

        public int MaximumHealth { get; private set; }
        public int CurrentHealth { get; private set; }

        private string _status;
        public string Status
        {
            get
            {
                return CurrentHealth == 0 ? "KO" : _status;
            }
            set
            {
                _status = value;
            }
        }

        public string Name
        {
            get { return PokemonNamesManager.Instance.Names[PokedexId]; }
        }

        public string Health
        {
            get { return CurrentHealth + "/" + MaximumHealth; }
        }

        public string Experience
        {
            get { return CurrentExperience + "/" + RequiredExperience; }
        }

        public PokemonMove[] Moves { get; private set; } = new PokemonMove[4];

        public Pokemon(string[] data)
        {
            Uid = int.Parse(data[0]);
            PokedexId = int.Parse(data[1]);
            Level = int.Parse(data[3]);
            Status = data[4];
            MaximumHealth = int.Parse(data[5]);
            CurrentHealth = int.Parse(data[6]);

            CurrentExperience = int.Parse(data[42]);
            RequiredExperience = int.Parse(data[43]);

            Moves[0] = new PokemonMove(data, 7);
            Moves[1] = new PokemonMove(data, 14);
            Moves[2] = new PokemonMove(data, 21);
            Moves[3] = new PokemonMove(data, 28);
        }

        public void UpdateHealth(int max, int current)
        {
            MaximumHealth = max;
            CurrentHealth = current;
        }
    }
}
