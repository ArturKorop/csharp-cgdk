using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class SniperBehavior : DefaultBehaviorV2
    {
        public SniperBehavior(World world, Trooper self, Game game) : base(world, self, game)
        {
        }

        protected override void CanMoveToTeammate()
        {
            if (Info.Teammates.Count <= 0 || !Self.CanMove() || BattleManagerV2.HeadOfSquad.Id == Self.Id) return;

            var targetTemamate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Commander) ??
                                 Info.Teammates[0];
                
            var path = CurrentPathFinder.GetPathToNeighbourCell(new Point(targetTemamate.X, targetTemamate.Y),
                                                                new Point(Self.X, Self.Y),
                                                                GetTeammates());
            if (path != null && path.Count > 0)
            {
                AddAction(new Move { Action = ActionType.Move, X = path.First().X, Y = path.First().Y },
                          Priority.MoveToTeammate, "CanMoveToTeammate", "");
            }
        }
    }
}