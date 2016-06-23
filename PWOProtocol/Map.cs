using System.Collections.Generic;

namespace PWOProtocol
{
    public class Map
    {
        public enum MoveResult
        {
            Success,
            Fail,
            Jump,
            NoLongerSurfing
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        
        private int[] _tiles;
        private int[,] _colliders;

        public Map(string content)
        {
            string[] data = content.Split(':');
            ReadHeader(data[data.Length - 1]);
            ReadTiles(data);
            InitColliders();
        }

        public int GetCollider(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y <= Height)
            {
                return _colliders[x, y];
            }
            return 0;
        }

        public MoveResult CanMove(Direction direction, int destinationX, int destinationY, bool isSurfing, IList<Npc> npcs)
        {
            if (destinationX < 0 || destinationX >= Width
                || destinationY < 0 || destinationY >= Height)
            {
                return MoveResult.Fail;
            }

            if (npcs != null)
            {
                foreach (Npc npc in npcs)
                {
                    if (npc.X == destinationX && npc.Y == destinationY)
                    {
                        return MoveResult.Fail;
                    }
                }
            }

            int collider = GetCollider(destinationX, destinationY);

            if (collider == 4)
            {
                return isSurfing ? MoveResult.NoLongerSurfing : MoveResult.Success;
            }

            if (collider == 2 && isSurfing)
            {
                return MoveResult.Success;
            }

            if (collider == 10 && direction == Direction.Down)
            {
                return MoveResult.Jump;
            }

            return MoveResult.Fail;
        }

        private void ReadHeader(string header)
        {
            string[] data = header.Split(' ');
            Width = int.Parse(data[0].Substring(2));
            Height = int.Parse(data[1].Substring(2));
        }

        private void ReadTiles(string[] data)
        {
            _tiles = new int[data.Length - 1];
            for (int i = 0; i < _tiles.Length; ++i)
            {
                _tiles[i] = int.Parse(data[i]);
            }
        }

        private void InitColliders()
        {
            int dimensionX = Width + 1;
            int dimensionY = Height + 1;
            int delta = dimensionX * dimensionY * 3;

            _colliders = new int[dimensionX, dimensionY];
            for (int x = 0; x < dimensionX; ++x)
            {
                for (int y = 0; y < dimensionY; ++y)
                {
                    _colliders[x, y] = _tiles[delta + x * dimensionY + y];
                }
            }
        }
    }
}
