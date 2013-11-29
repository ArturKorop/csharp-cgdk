using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class PathFinder
    {
        private readonly Point[,] _mapField = new Point[30,20];
        private Map _map;

        public PathFinder(CellType[][] cells)
        {
            for (int i = 0; i < 30; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    var temp = new Point(i, j)
                        {
                            Value = cells[i][j] == CellType.Free ? FieldStatus.Avaliable : FieldStatus.Unavaliable
                        };
                    _mapField[i, j] = temp;
                }
            }
        }

        public List<Point> GetPathToNeighbourCell(Point target, Point self, List<Point> teammates)
        {
            var tempMapField = CopyDeepMap();
            foreach (var teammate in teammates)
                tempMapField[teammate.X, teammate.Y].Value = FieldStatus.Unavaliable;

            _map = new Map(tempMapField, tempMapField[target.X, target.Y]);
            var result = Find(tempMapField[self.X, self.Y], null);
            if (result == null)
                return null;

            var path = new List<Point>();
            result = result.Parent;
            while (result.X != self.X || result.Y != self.Y)
            {
                path.Add(new Point(result.X, result.Y));
                result = result.Parent;
            }
            path.Reverse();

            return path;
        }

        public List<Point> GetPathToPoint(Point target, Point self, List<Point> teammates)
        {
            var tempMapField = CopyDeepMap();
            foreach (var teammate in teammates)
                tempMapField[teammate.X, teammate.Y].Value = FieldStatus.Unavaliable;

            if (tempMapField[target.X, target.Y].Value == FieldStatus.Unavaliable) return null;

            _map = new Map(tempMapField, tempMapField[target.X, target.Y]);
            var result = Find(tempMapField[self.X, self.Y], null);
            if (result == null)
                return null;

            var path = new List<Point>();
            while (result.X != self.X || result.Y != self.Y)
            {
                path.Add(new Point(result.X, result.Y));
                result = result.Parent;
            }
            path.Reverse();

            return path;
        }

        public Point GetNextPoint(int currentX, int currentY, int targetX, int targetY, List<Point> teammates)
        {
            var tempMapField = CopyDeepMap();
            foreach (var temmate in teammates)
            {
                tempMapField[temmate.X, temmate.Y].Value = FieldStatus.Unavaliable;
            }
            _map = new Map(tempMapField, tempMapField[targetX, targetY]);

            var result = Find(tempMapField[currentX, currentY], null);

            if (result == null)
            {
                return new Point(currentX, currentY);
            }

            var tempResult = result;
            while (result.X != currentX || result.Y != currentY)
            {
                tempResult = result;
                result = result.Parent;
            }

            return tempResult;
        }

        public Point FindTargetPoint(int targetX, int targetY, IEnumerable<Point> temmates)
        {
            var tempMapField = CopyDeepMap();
            foreach (var temmate in temmates)
                tempMapField[temmate.X, temmate.Y].Value = FieldStatus.Unavaliable;

            var target = new Point(targetX, targetY);
            if (target.Value == FieldStatus.Avaliable) return target;

            _map = new Map(tempMapField, target);
            var result = FindNearestAvaliablePoint(target, null);

            return result;
        }

        public List<Point> AvaliablePositionToShout(Trooper self, Trooper target, World world, List<Point> teammates)
        {
            var points = new List<Point>();
            for (int i = 0; i < 30; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    if (world.IsVisible(self.ShootingRange, i, j, self.Stance, target.X, target.Y, target.Stance))
                    {
                        var tempPoint = new Point(i, j);
                        var path = GetPathToPoint(tempPoint, self.ToPoint(), teammates);
                        if (path != null && path.Count <= (self.ActionPoints/self.MoveCost()))
                            points.Add(new Point(i, j));
                    }
                }
            }

            return points;
        }

        public Point GetSafePoint(Trooper self, List<Trooper> enemies, World world, List<Point> teammates)
        {
            if (self.X == 29 || self.X == 0 || self.Y == 0 || self.Y == 19) return null;

            var avaliableNeighbours = new List<Point>();
            int x = self.X - 1;
            int y = self.Y;
            if (x >= 0 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == self.X - 1 && t.Y == self.Y))
                avaliableNeighbours.Add(new Point(x, y));

            x = self.X + 1;
            y = self.Y;
            if (x < 30 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == self.X + 1 && t.Y == self.Y))
                avaliableNeighbours.Add(new Point(x, y));

            x = self.X;
            y = self.Y - 1;
            if (y >= 0 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == self.X && t.Y == self.Y - 1))
                avaliableNeighbours.Add(new Point(x, y));

            x = self.X;
            y = self.Y + 1;
            if (y < 20 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == self.X && t.Y == self.Y + 1))
                avaliableNeighbours.Add(new Point(x, y));

            if (avaliableNeighbours.Count == 0) return null;

            var warningPoints = new List<Point>();
            foreach (var enemy in enemies)
            {
                for (int i = 0; i < 30; i++)
                {
                    for (int j = 0; j < 20; j++)
                    {
                        if (world.IsVisible(enemy.ShootingRange, enemy.X, enemy.Y, enemy.Stance, i, j, self.Stance))
                        {
                            warningPoints.Add(new Point(i, j));
                        }
                    }
                }
            }
            if (warningPoints.Count == 0) return null;

            var safePoints = avaliableNeighbours.Where(an => !warningPoints.Contains(an)).ToList();
            if (safePoints.Count == 0) return null;

            var minPath = teammates.Select(t => (Math.Abs(self.X - t.X) + Math.Abs(self.Y - t.Y))).Min();
            var teammate = teammates.First(t => (Math.Abs(self.X - t.X) + Math.Abs(self.Y - t.Y)) == minPath);
            if (teammate == null) return null;

            var minPathToTeammateFromNewPosition =
                safePoints.Select(p => (Math.Abs(teammate.X - p.X) + Math.Abs(teammate.Y - p.Y))).Min();
            var minimumSafePoint =
                safePoints.First(
                    p => (Math.Abs(teammate.X - p.X) + Math.Abs(teammate.Y - p.Y)) == minPathToTeammateFromNewPosition);

            return minimumSafePoint;
        }

        public static bool IsThisNeightbours(Point self, Point target)
        {
            return Math.Abs(self.X - target.X) + Math.Abs(self.Y - target.Y) == 1;
        }

        private Point Find(Point self, IEnumerable<Point> allNeighbours)
        {
            var neighbours = _map.GetAvaliableNeighbours(self);
            if (allNeighbours != null)
                neighbours.AddRange(allNeighbours);

            neighbours.Sort((x, y) => (x.Cost).CompareTo(y.Cost));
            foreach (var neighbour in neighbours.Where(x => x.Value != FieldStatus.Searched))
            {
                if (neighbour.Value != FieldStatus.Target)
                    return Find(neighbour, neighbours);

                return neighbour;
            }

            if (allNeighbours == null)
                return null;

            var tempAllNeighbours = allNeighbours as List<Point>;
            if (tempAllNeighbours == null)
                return null;

            var minCost =
                tempAllNeighbours.Select(y => Math.Abs(y.X - _map.Target.X) + Math.Abs(y.Y - _map.Target.Y)).Min();
            return tempAllNeighbours.First(x => Math.Abs(x.X - _map.Target.X) + Math.Abs(x.Y - _map.Target.Y) == minCost);
        }

        private Point FindNearestAvaliablePoint(Point target, IEnumerable<Point> allNeighbours)
        {
            var neighbours = _map.GetAvaliableNeighbours(target);
            if (allNeighbours != null)
                neighbours.AddRange(allNeighbours);

            foreach (var neighbour in neighbours)
            {
                if (neighbour.Value == FieldStatus.Avaliable)
                    return neighbour;

                return FindNearestAvaliablePoint(neighbour, neighbours);
            }

            return null;
        }

        private Point[,] CopyDeepMap()
        {
            var tempMapField = new Point[30,20];
            for (int i = 0; i < 30; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    var temp = _mapField[i, j];
                    tempMapField[i, j] = new Point(temp.X, temp.Y) {Value = temp.Value};
                }
            }

            return tempMapField;
        }

        public Point GetPointToUseGrenade(int greanadeRange, World world, Point self, int possibleStepToEnemy,
                                          List<Trooper> visibleEnemies, List<Point> teammates)
        {
            var avaliableNeighbours = new List<Point>();
            if (possibleStepToEnemy == 1)
            {
                int x = self.X - 1;
                int y = self.Y;
                if (x >= 0 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == self.X - 1 && t.Y == self.Y))
                    avaliableNeighbours.Add(new Point(x, y));

                x = self.X + 1;
                y = self.Y;
                if (x < 30 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == self.X + 1 && t.Y == self.Y))
                    avaliableNeighbours.Add(new Point(x, y));

                x = self.X;
                y = self.Y - 1;
                if (y >= 0 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == self.X && t.Y == self.Y - 1))
                    avaliableNeighbours.Add(new Point(x, y));

                x = self.X;
                y = self.Y + 1;
                if (y < 20 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == self.X && t.Y == self.Y + 1))
                    avaliableNeighbours.Add(new Point(x, y));

                if (avaliableNeighbours.Count == 0) return null;
            }

           foreach (var avaliableNeighbour in from avaliableNeighbour in avaliableNeighbours
                                               from visibleEnemy in visibleEnemies
                                               where
                                                   world.IsVisible(greanadeRange, avaliableNeighbour.X,
                                                                   avaliableNeighbour.Y,
                                                                   TrooperStance.Standing, visibleEnemy.X,
                                                                   visibleEnemy.Y,
                                                                   TrooperStance.Standing)
                                               select avaliableNeighbour)
            {
                return avaliableNeighbour;
            }

            if (possibleStepToEnemy == 2)
            {
                int x = self.X - possibleStepToEnemy;
                int y = self.Y;
                if (x >= 0 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == x && t.Y == y))
                    avaliableNeighbours.Add(new Point(x, y));

                x = self.X + possibleStepToEnemy;
                y = self.Y;
                if (x < 30 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == x && t.Y == y))
                    avaliableNeighbours.Add(new Point(x, y));

                x = self.X;
                y = self.Y - possibleStepToEnemy;
                if (y >= 0 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == x && t.Y == y))
                    avaliableNeighbours.Add(new Point(x, y));

                x = self.X;
                y = self.Y + possibleStepToEnemy;
                if (y < 20 && world.Cells[x][y] == CellType.Free && !teammates.Any(t => t.X == x && t.Y == y))
                    avaliableNeighbours.Add(new Point(x, y));

                if (avaliableNeighbours.Count == 0) return null;
            }

            if (!avaliableNeighbours.Any()) return null;

            foreach (var avaliableNeighbour in from avaliableNeighbour in avaliableNeighbours
                                               from visibleEnemy in visibleEnemies
                                               where
                                                   world.IsVisible(greanadeRange, avaliableNeighbour.X,
                                                                   avaliableNeighbour.Y,
                                                                   TrooperStance.Standing, visibleEnemy.X,
                                                                   visibleEnemy.Y,
                                                                   TrooperStance.Standing)
                                               select avaliableNeighbour)
            {
                return avaliableNeighbour;
            }

            return null;
        }
    }

    public class Point : IEquatable<Point>
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Point Parent { get; set; }
        public FieldStatus Value { get; set; }
        public int Cost { get; set; }
        public int MoveCost { get; set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Point other)
        {
            return other.X == X && other.Y == Y;
        }

        public override string ToString()
        {
            return String.Format("[{0},{1}]: {2}:{3}", X, Y, Value, Cost);
        }
    }

    public class Map
    {
        private readonly Point[,] _map;
        private readonly int _width;
        private readonly int _height;
        public Point Target { get; private set; }

        public Map(Point[,] map, Point target)
        {
            _map = map;
            _width = _map.GetLength(0);
            _height = _map.GetLength(1);
            Target = target;
            _map[target.X, target.Y].Value = FieldStatus.Target;
            Target.Value = FieldStatus.Target;
        }

        public List<Point> GetAvaliableNeighbours(Point self)
        {
            var neighbours = new List<Point>();
            if (self.X - 1 >= 0)
            {
                var point = _map[self.X - 1, self.Y];
                AddPoint(self, point, neighbours);
            }
            if (self.X + 1 < _width)
            {
                var point = _map[self.X + 1, self.Y];
                AddPoint(self, point, neighbours);
            }
            if (self.Y - 1 >= 0)
            {
                var point = _map[self.X, self.Y - 1];
                AddPoint(self, point, neighbours);
            }
            if (self.Y + 1 < _height)
            {
                var point = _map[self.X, self.Y + 1];
                AddPoint(self, point, neighbours);
            }
            self.Value = FieldStatus.Searched;

            return neighbours;
        }

        private void AddPoint(Point self, Point point, List<Point> neighbours)
        {
            if (point.Value != FieldStatus.Unavaliable && point.Value != FieldStatus.Searched)
            {
                point.Parent = self;
                point.MoveCost = self.MoveCost + 10;
                point.Cost = point.MoveCost + GetCost(point.X, point.Y);
                neighbours.Add(point);
            }
        }

        private int GetCost(int currentX, int currentY)
        {
            var result = 10*(Math.Abs(currentX - Target.X) + Math.Abs(currentY - Target.Y));

            return result;
        }

        public void ConsoleDisplay()
        {
            for (int i = 0; i < _height; i++)
            {
                Console.WriteLine();
                for (int j = 0; j < _width; j++)
                {
                    Console.Write((int) _map[j, i].Value);
                }
            }
        }
    }

    public enum FieldStatus
    {
        Avaliable,
        Unavaliable,
        Target,
        Hero,
        Searched,
        Path
    }
}