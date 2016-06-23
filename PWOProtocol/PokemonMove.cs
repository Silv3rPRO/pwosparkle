namespace PWOProtocol
{
    public class PokemonMove
    {
        public string Name { get; private set; }
        public int Category { get; private set; }
        public PokemonType Type { get; private set; }
        public int Power { get; private set; }
        public int Accuracy { get; private set; }
        public int CurrentPoints { get; private set; }
        public int MaximumPoints { get; private set; }

        public PokemonMove(string[] data, int index)
        {
            Name = data[index];
            Category = int.Parse(data[index + 1]);
            Type = (PokemonType)int.Parse(data[index + 2]);
            Power = int.Parse(data[index + 3]);
            Accuracy = int.Parse(data[index + 4]);
            CurrentPoints = int.Parse(data[index + 5]);
            MaximumPoints = int.Parse(data[index + 6]);
        }
    }
}
