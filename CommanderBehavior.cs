using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class CommanderBehavior : DefaultBehaviorV2
    {
        public CommanderBehavior(World world, Trooper self, Game game)
            : base(world, self, game)
        {
        }

        protected override void CanReatreate()
        {
            /*if(Info.VisibleEnemies.Count == 0 || !Self.CanMove() || Info.FightingEnemies.Count > 0) return;

            if (Self.ActionPoints < Self.MoveCost() + Self.ShootCost)
            {
                var point = CurrentPathFinder.GetSafePoint(Self, World.Troopers.Where(x => !x.IsTeammate).ToList(),
                                                           World, GetTeammates());
                if(point == null) return;

                AddAction(new Move { Action = ActionType.Move, X = point.X, Y = point.Y }, Priority.Retreat, "CanReatreate", "");
                BattleManagerV2.HiddenEnemies.AddRange(Info.VisibleEnemies);
            }*/
        }
    }
}