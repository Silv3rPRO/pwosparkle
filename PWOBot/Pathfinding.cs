using PWOProtocol;
using System;
using System.Collections.Generic;

namespace PWOBot
{
    public class Pathfinding
    {
        private GameClient _client;
        //private bool _hasSurfAbility; TODO SURF

        private class Node
        {
            public int X;
            public int Y;
            public bool IsSurfing;
            public int Distance;
            public int Score;
            public Node Parent;

            public Direction FromDirection;
            public int DirectionChangeCount;

            public uint Hash
            {
                get { return (uint)X * 0x7FFFU + (uint)Y + (IsSurfing ? 0x40000000U : 0U); }
            }

            public Node(int x, int y, bool isSurfing)
            {
                X = x;
                Y = y;
                IsSurfing = isSurfing;
            }

            public Node(int x, int y, bool isSurfing, Direction direction)
            {
                X = x;
                Y = y;
                IsSurfing = isSurfing;
                FromDirection = direction;
            }
        }

        public Pathfinding(GameClient client)
        {
            _client = client;
            //_hasSurfAbility = false; TODO surf
        }

        public bool MoveTo(int destinationX, int destinationY, int requiredDistance = 0)
        {
            if (destinationX == _client.PlayerX && destinationY == _client.PlayerY)
            {
                return true;
            }

            Node node = FindPath(_client.PlayerX, _client.PlayerY, _client.IsSurfing, destinationX, destinationY, requiredDistance);

            if (node != null)
            {
                Stack<Direction> directions = new Stack<Direction>();
                while (node.Parent != null)
                {
                    if (!node.Parent.IsSurfing && node.IsSurfing)
                    {
                        directions.Clear();
                        // _client.UseSurfAfterMovement(); TODO SURF
                    }
                    else
                    {
                        directions.Push(node.FromDirection);
                    }
                    node = node.Parent;
                }

                while (directions.Count > 0)
                {
                    _client.Move(directions.Pop());
                }
                return true;
            }
            return false;
        }

        public bool MoveToSameCell()
        {
            Map.MoveResult result = _client.Map.CanMove(Direction.Right, _client.PlayerX + 1, _client.PlayerY, _client.IsSurfing, _client.Npcs);
            if (result == Map.MoveResult.Success || result == Map.MoveResult.NoLongerSurfing)
            {
                _client.Move(Direction.Right);
                _client.Move(Direction.Left);
                return true;
            }
            result = _client.Map.CanMove(Direction.Left, _client.PlayerX - 1, _client.PlayerY, _client.IsSurfing, _client.Npcs);
            if (result == Map.MoveResult.Success || result == Map.MoveResult.NoLongerSurfing)
            {
                _client.Move(Direction.Left);
                _client.Move(Direction.Right);
                return true;
            }
            result = _client.Map.CanMove(Direction.Down, _client.PlayerX, _client.PlayerY + 1, _client.IsSurfing, _client.Npcs);
            if (result == Map.MoveResult.Success || result == Map.MoveResult.NoLongerSurfing)
            {
                _client.Move(Direction.Down);
                _client.Move(Direction.Up);
                return true;
            }
            result = _client.Map.CanMove(Direction.Up, _client.PlayerX, _client.PlayerY - 1, _client.IsSurfing, _client.Npcs);
            if (result == Map.MoveResult.Success || result == Map.MoveResult.NoLongerSurfing)
            {
                _client.Move(Direction.Up);
                _client.Move(Direction.Down);
                return true;
            }
            return false;
        }

        private Node FindPath(int fromX, int fromY, bool isSurfing, int toX, int toY, int requiredDistance)
        {
            Dictionary<uint, Node> openList = new Dictionary<uint, Node>();
            HashSet<uint> closedList = new HashSet<uint>();
            Node start = new Node(fromX, fromY, isSurfing);
            openList.Add(start.Hash, start);

            while (openList.Count > 0)
            {
                Node current = GetBestNode(openList.Values);
                int distance = Math.Abs(current.X - toX) + Math.Abs(current.Y - toY);
                if (distance <= requiredDistance)
                {
                    return current;
                }

                openList.Remove(current.Hash);
                closedList.Add(current.Hash);

                List<Node> neighbors = GetNeighbors(current);
                foreach (Node node in neighbors)
                {
                    if (closedList.Contains(node.Hash))
                        continue;
                    //if (_client.Map.HasLink(node.X, node.Y) && node.X != toX && node.Y != toY)
                    //    continue;

                    node.Parent = current;
                    node.Distance = current.Distance + 1;
                    node.Score = node.Distance;

                    node.DirectionChangeCount = current.DirectionChangeCount;
                    if (node.FromDirection != current.FromDirection)
                    {
                        node.DirectionChangeCount += 1;
                    }
                    node.Score += node.DirectionChangeCount / 4;
                    if (node.IsSurfing == true && current.IsSurfing == false)
                    {
                        node.Score += 10;
                    }

                    if (!openList.ContainsKey(node.Hash))
                    {
                        openList.Add(node.Hash, node);
                    }
                    else if (openList[node.Hash].Score > node.Score)
                    {
                        openList.Remove(node.Hash);
                        openList.Add(node.Hash, node);
                    }
                }
            }
            return null;
        }

        private List<Node> GetNeighbors(Node node)
        {
            List<Node> neighbors = new List<Node>();

            Map.MoveResult result = _client.Map.CanMove(Direction.Up, node.X, node.Y - 1, node.IsSurfing, _client.Npcs);
            if (result == Map.MoveResult.Success || result == Map.MoveResult.NoLongerSurfing)
            {
                bool surfing = (result == Map.MoveResult.NoLongerSurfing ? false : node.IsSurfing);
                neighbors.Add(new Node(node.X, node.Y - 1, surfing, Direction.Up));
            }

            result = _client.Map.CanMove(Direction.Down, node.X, node.Y + 1, node.IsSurfing, _client.Npcs);
            if (result == Map.MoveResult.Success || result == Map.MoveResult.NoLongerSurfing)
            {
                bool surfing = (result == Map.MoveResult.NoLongerSurfing ? false : node.IsSurfing);
                neighbors.Add(new Node(node.X, node.Y + 1, surfing, Direction.Down));
            }
            else if (result == Map.MoveResult.Jump)
            {
                neighbors.Add(new Node(node.X, node.Y + 2, node.IsSurfing, Direction.Down));
            }

            result = _client.Map.CanMove(Direction.Left, node.X - 1, node.Y, node.IsSurfing, _client.Npcs);
            if (result == Map.MoveResult.Success || result == Map.MoveResult.NoLongerSurfing)
            {
                bool surfing = (result == Map.MoveResult.NoLongerSurfing ? false : node.IsSurfing);
                neighbors.Add(new Node(node.X - 1, node.Y, surfing, Direction.Left));
            }
            else if (result == Map.MoveResult.Jump)
            {
                neighbors.Add(new Node(node.X - 2, node.Y, node.IsSurfing, Direction.Left));
            }

            result = _client.Map.CanMove(Direction.Right, node.X + 1, node.Y, node.IsSurfing, _client.Npcs);
            if (result == Map.MoveResult.Success || result == Map.MoveResult.NoLongerSurfing)
            {
                bool surfing = (result == Map.MoveResult.NoLongerSurfing ? false : node.IsSurfing);
                neighbors.Add(new Node(node.X + 1, node.Y, surfing, Direction.Right));
            }
            else if (result == Map.MoveResult.Jump)
            {
                neighbors.Add(new Node(node.X + 2, node.Y, node.IsSurfing, Direction.Right));
            }

            /*if (!node.IsSurfing && _hasSurfAbility && _client.Map.CanSurf(node.X, node.Y, node.IsOnGround))
            {
                neighbors.Add(new Node(node.X, node.Y, node.IsOnGround, true));
            } TODO SURF*/

            return neighbors;
        }

        private Node GetBestNode(IEnumerable<Node> nodes)
        {
            List<Node> bestNodes = new List<Node>();
            int bestScore = int.MaxValue;
            foreach (Node node in nodes)
            {
                if (node.Score < bestScore)
                {
                    bestNodes.Clear();
                    bestScore = node.Score;
                }
                if (node.Score == bestScore)
                {
                    bestNodes.Add(node);
                }
            }
            return bestNodes[_client.Rand.Next(bestNodes.Count)];
        }
    }
}
