namespace PWOProtocol
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }
    public static class DirectionExtensions
    {
        public static string AsChar(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return "u";
                case Direction.Down:
                    return "d";
                case Direction.Left:
                    return "l";
                case Direction.Right:
                    return "r";
            }
            return null;
        }

        public static string AsCardinalChar(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return "n";
                case Direction.Down:
                    return "s";
                case Direction.Left:
                    return "w";
                case Direction.Right:
                    return "e";
            }
            return null;
        }

        public static Direction FromChar(char c)
        {
            switch (c)
            {
                case 'u':
                    return Direction.Up;
                case 'd':
                    return Direction.Down;
                case 'l':
                    return Direction.Left;
                case 'r':
                    return Direction.Right;
            }
            throw new System.Exception("The direction '" + c + "' does not exist");
        }
    }
}
