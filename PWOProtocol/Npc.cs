using System;

namespace PWOProtocol
{
    public class Npc
    {
        public string Map { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public string Skin { get; private set; }
        public string Name { get; private set; }
        public int Id { get; private set; }

        public Npc(string content)
        {
            string[] data = content.Split(new string[] { "/.\\" }, StringSplitOptions.None);

            Map = data[0];
            X = int.Parse(data[1]);
            Y = int.Parse(data[2]);
            Skin = data[10];
            Name = data[11];
            Id = int.Parse(data[14]);
        }
    }
}
