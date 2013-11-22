using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class SoldierBehavior : DefaultBehavior
    {
        public SoldierBehavior(World world, Trooper self, Game game) : base(world, self, game)
        {
        }

        protected override bool MoveToTeammate(Move move)
        {
            if (Info.Teammates.Count > 0 && Self.CanMove() && Info.CanShoutedEnemiesImmediately.Count == 0)
            {
                var targetTemamate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Commander);
                if (targetTemamate == null) return false;

                var medicTeammate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.FieldMedic) ??
                                      Info.Teammates[0];
                var pathFinder = new PathFinder(World.Cells);
                var path = pathFinder.GetPathToNeighbourCell(new Point(targetTemamate.X, targetTemamate.Y), new Point(Self.X, Self.Y),
                                              GetTeammates());
                pathFinder = new PathFinder(World.Cells);
                var pathWithoutTeammates = pathFinder.GetPathToNeighbourCell(new Point(targetTemamate.X, targetTemamate.Y), new Point(Self.X, Self.Y),
                                              new List<Point>());
                if (path.Count > Self.ActionPoints / Self.MoveCost() && path.Count >= pathWithoutTeammates.Count + 2)
                {
                    pathFinder = new PathFinder(World.Cells);
                    path = pathFinder.GetPathToNeighbourCell(new Point(medicTeammate.X, medicTeammate.Y), new Point(Self.X, Self.Y),
                                              GetTeammates());
                }
                //TODO: possible no way!
                if (path != null && path.Count > 0)
                {
                    move.Action = ActionType.Move;
                    move.X = path.First().X;
                    move.Y = path.First().Y;

                    return true;
                }
            }

            return false;
        }

        protected override bool MoveToEnemy(Move move)
        {
            if (!Self.CanMoveCarefully()) return false;

            var commander = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Commander);
            if (commander == null)
            {
                if (Info.VisibleEnemies.Count > 0 && Info.FightingEnemies.Count == 0 && Self.CanMoveCarefully())
                {
                    var pathFinder = new PathFinder(World.Cells);
                    var target =
                        Info.VisibleEnemies.First(
                            x => x.Hitpoints == Info.VisibleEnemies.Where(y => y.Hitpoints > 0).Min(y => y.Hitpoints));
                    var point = pathFinder.GetNextPoint(Self.X, Self.Y, target.X, target.Y, GetTeammates());
                    move.Action = ActionType.Move;
                    move.X = point.X;
                    move.Y = point.Y;

                    return true;
                }

                if (Info.FightingEnemies.Count > 0 && Self.CanMove())
                {
                    var pathFinder = new PathFinder(World.Cells);
                    var target =
                        Info.FightingEnemies.First(
                            x => x.Hitpoints == Info.FightingEnemies.Where(y => y.Hitpoints > 0).Min(y => y.Hitpoints));
                    var point = pathFinder.GetNextPoint(Self.X, Self.Y, target.X, target.Y, GetTeammates());
                    move.Action = ActionType.Move;
                    move.X = point.X;
                    move.Y = point.Y;

                    return true;
                }
            }
            else
            {
                var pathFinder = new PathFinder(World.Cells);
                var nextPoint = pathFinder.GetNextPoint(Self.X, Self.Y, commander.X, commander.Y, GetTeammates());
                move.Action = ActionType.Move;
                move.X = nextPoint.X;
                move.Y = nextPoint.Y;

                return true;
            }

            return false;
        }
    }
}