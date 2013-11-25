using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class SniperBehavior : DefaultBehavior
    {
        public SniperBehavior(World world, Trooper self, Game game) : base(world, self, game)
        {
        }

        protected override bool MoveToTeammate(Move move)
        {
            if (Info.Teammates.Count > 0 && Self.CanMove())
            {
                var targetTemamate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Commander) ??
                                     Info.Teammates[0];
                var targetCommander = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Soldier) ??
                                      Info.Teammates[0];
                var pathFinder = new PathFinder(World.Cells);
                var path = pathFinder.GetPathToNeighbourCell(new Point(targetTemamate.X, targetTemamate.Y), new Point(Self.X, Self.Y),
                                              GetTeammates());
                if (path.Count > Self.ActionPoints / Self.MoveCost())
                {
                    pathFinder = new PathFinder(World.Cells);
                    path = pathFinder.GetPathToNeighbourCell(new Point(targetCommander.X, targetCommander.Y), new Point(Self.X, Self.Y),
                                              GetTeammates());
                }
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
    }
}