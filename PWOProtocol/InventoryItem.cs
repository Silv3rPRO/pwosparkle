using System;

namespace PWOProtocol
{
    public class InventoryItem
    {
        public string Name { get; private set; }
        public int Id { get; private set; }
        public int Quantity { get; private set; }
        public int Scope { get; private set; }

        public InventoryItem(string content)
        {
            string[] data = content.Split(new [] { "-=-" }, StringSplitOptions.None);
            Name = data[0];
            Id = int.Parse(data[1]);
            Quantity = int.Parse(data[2]);
            Scope = int.Parse(data[3]);
        }
    }
}
