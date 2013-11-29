using System;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class SoldierBehavior : DefaultBehaviorV2
    {
        public SoldierBehavior(World world, Trooper self, Game game) : base(world, self, game)
        {
        }

        protected override void CanMoveToTeammate()
        {
            if (!Self.CanMove() || Info.Teammates.Count == 0 || Self.Id == BattleManagerV2.HeadOfSquad.Id) return;

            var path = CurrentPathFinder.GetPathToNeighbourCell(BattleManagerV2.HeadOfSquad.ToPoint(), Self.ToPoint(),
                                                                GetTeammates());
            if (path == null) return;
            if (path.Count == 0) return;

            var nextPoint = path.First();
            AddAction(new Move { Action = ActionType.Move, X = nextPoint.X, Y = nextPoint.Y }, Priority.MoveToTeammate,
                      "CanMoveToTeammate",
                      String.Format("Teammate - [{0},{1}]", path.Last().X, path.Last().Y));
        }
    }
}