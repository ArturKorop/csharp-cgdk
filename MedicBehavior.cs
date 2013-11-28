using System;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class MedicBehavior : DefaultBehaviorV2
    {
        public MedicBehavior(World world, Trooper self, Game game)
            : base(world, self, game)
        {
        }

        protected override void CanHealSelf()
        {
            if (Info.Teammates.Count == 0) return;

            var pathFinder = new PathFinder(World.Cells);
            if (Self.CanUseMedikit() && Info.WoundedTeammates.Count(x => x.Hitpoints <= 60) > 0 &&
                Info.VisibleEnemies.Count > 0 && Self.CanMove())
            {
                foreach (var teammate in Info.WoundedTeammates.Where(x => x.Hitpoints <= 60))
                {
                    var path = pathFinder.GetPathToNeighbourCell(new Point(teammate.X, teammate.Y),
                                                                 new Point(Self.X, Self.Y),
                                                                 Info.Teammates.Select(x => new Point(x.X, x.Y))
                                                                     .ToList());
                    if (path.Count == 0)
                    {
                        AddAction(new Move {Action = ActionType.UseMedikit, X = teammate.X, Y = teammate.Y},
                                  Priority.HealTeammate, "CanHealTeammate",
                                  String.Format("Teammate: {0}[{1},{2}]", teammate.Type, teammate.X, teammate.Y));

                        return;
                    }
                    if (path.Count*Self.MoveCost() + Game.MedikitUseCost <= Self.ActionPoints)
                    {
                        AddAction(new Move {Action = ActionType.Move, X = path.First().X, Y = path.First().Y},
                                  Priority.HealTeammate, "CanHealTeammate",
                                  String.Format("Teammate: {0}[{1},{2}]", teammate.Type, teammate.X, teammate.Y));

                        return;
                    }
                }
            }
            if (Self.CanHeal() && Info.WoundedTeammates.Count > 0)
            {
                foreach (var woundedTeammate in Info.WoundedTeammates)
                {
                    if (Math.Abs(Self.X - woundedTeammate.X) + Math.Abs(Self.Y - woundedTeammate.Y) == 1)
                    {
                        AddAction(new Move {Action = ActionType.Heal, X = woundedTeammate.X, Y = woundedTeammate.Y},
                                  Priority.HealTeammate, "CanHealTeammate",
                                  String.Format("Teammate: {0}[{1},{2}]", woundedTeammate.Type, woundedTeammate.X,
                                                woundedTeammate.Y));

                        return;
                    }
                }
            }
            if (Self.Hitpoints <= 60 && Self.CanUseMedikit())
            {
                AddAction(new Move {Action = ActionType.UseMedikit, X = Self.X, Y = Self.Y}, Priority.HealSelf,
                          "CanHealSelf", String.Format("Self: {0}[{1},{2}]", Self.Type, Self.X, Self.Y));

                return;
            }
            if (Self.Hitpoints < Self.MaximalHitpoints && Self.CanHeal())
            {
                AddAction(new Move {Action = ActionType.Heal, X = Self.X, Y = Self.Y}, Priority.HealSelf,
                          "CanHealSelf", String.Format("Teammate: {0}[{1},{2}]", Self.Type, Self.X, Self.Y));

                return;
            }
            if (Self.CanHeal() && Info.WoundedTeammates.Count > 0 && Self.CanMove())
            {
                var target =
                    Info.WoundedTeammates.First(
                        x => x.Hitpoints == Info.WoundedTeammates.Where(y => y.Hitpoints > 0).Min(y => y.Hitpoints));
                pathFinder = new PathFinder(World.Cells);
                var targetPoint = pathFinder.GetNextPoint(Self.X, Self.Y, target.X, target.Y,
                                                          Info.Teammates.Select(x => new Point(x.X, x.Y)).ToList());

                AddAction(new Move {Action = ActionType.Move, X = targetPoint.X, Y = targetPoint.Y},
                          Priority.HealTeammate, "CanHealTeammate",
                          String.Format("Teammate: {0}[{1},{2}]", target.Type, target.X, target.Y));

                return;
            }
            if (Self.Hitpoints < Self.MaximalHitpoints && Self.CanHeal())
            {
                AddAction(new Move {Action = ActionType.Heal, X = Self.X, Y = Self.Y}, Priority.HealSelf,
                          "CanHealSelf", String.Format("Self: {0}[{1},{2}]", Self.Type, Self.X, Self.Y));
            }
        }

        protected override void CanHealTeammate()
        {
        }

        protected override void CanMoveToTeammate()
        {
            if (BattleManagerV2.HeadOfSquad.Id == Self.Id) return;

            BattleManagerV2.AddHiddenEnemies(Info.VisibleEnemies);
            if (Info.Teammates.Count <= 0 || !Self.CanMove() || BattleManagerV2.HeadOfSquad.Id == Self.Id) return;

            var targetTemamate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Soldier) ??
                                 Info.Teammates[0];
            var targetOtherTeammate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Commander) ??
                                      Info.Teammates[0];
            var pathFinder = new PathFinder(World.Cells);
            var path = pathFinder.GetPathToNeighbourCell(new Point(targetTemamate.X, targetTemamate.Y),
                                                         new Point(Self.X, Self.Y),
                                                         GetTeammates());
            if (path == null) return;
                
            if (path.Count > Self.ActionPoints/Self.MoveCost())
            {
                pathFinder = new PathFinder(World.Cells);
                path = pathFinder.GetPathToNeighbourCell(new Point(targetOtherTeammate.X, targetOtherTeammate.Y),
                                                         new Point(Self.X, Self.Y),
                                                         GetTeammates());
            }
            if (path != null && path.Count > 0)
            {
                AddAction(new Move {Action = ActionType.Move, X = path.First().X, Y = path.First().Y},
                          Priority.MedicMoveToTeammate, "CanMoveToTeammate", "");
            }
        }

        /*protected override bool ShoutEnemy(Move move)
        {
            if (Info.Teammates.Count == 0 && Info.CanShoutedEnemiesImmediately.Count == 1 && Self.CanShout() &&
                Info.VisibleEnemies.Count == 1)
            {
                var possibleTarget =
                    Info.CanShoutedEnemiesImmediately.Where(
                        x => x.Hitpoints == Info.CanShoutedEnemiesImmediately.Min(y => y.Hitpoints))
                        .ToArray();
                var target = possibleTarget.FirstOrDefault(x => x.Type == TrooperType.FieldMedic);
                target = target ?? possibleTarget[0];
                move.Action = ActionType.Shoot;
                move.X = target.X;
                move.Y = target.Y;

                return true;
            }

            return false;
        }*/

        protected override void CanGatherBonus()
        {
            if (Info.AvaliableBonuses.Count == 0 ||
                !((Self.CanMove() && BattleManager.Step < BattleManager.StepCarefullCount) ||
                  (Self.CanMoveCarefully() && BattleManager.Step >= BattleManager.StepCarefullCount))) return;

            var minPath =
                Info.AvaliableBonuses.Select(
                    x =>
                    CurrentPathFinder.GetPathToNeighbourCell(new Point(x.X, x.Y), new Point(Self.X, Self.Y),
                                                             GetTeammates()))
                    .Min(y => y.Count);
            if (minPath > 3) return;

            var currentBonuse =
                Info.AvaliableBonuses.First(
                    x =>
                    CurrentPathFinder.GetPathToNeighbourCell(new Point(x.X, x.Y), new Point(Self.X, Self.Y),
                                                             GetTeammates())
                                     .Count == minPath);
            var nextPoint = CurrentPathFinder.GetNextPoint(Self.X, Self.Y, currentBonuse.X, currentBonuse.Y,
                                                           GetTeammates());
            AddAction(new Move {Action = ActionType.Move, X = nextPoint.X, Y = nextPoint.Y}, Priority.GatherBonus,
                      "CanGatherBonus",
                      String.Format("Bonus[{0},{1}] - {2}", currentBonuse.X, currentBonuse.Y, currentBonuse.Type));
        }

        protected override void CanCheckPositionForTeammates()
        {
        }

        protected override void CanCheckBestPosition()
        {
        }

        protected override void CanLowerStance()
        {
        }

        protected override void CanStayNearTeammate()
        {
            if (Info.Teammates.Count > 0 && Self.CanMove())
            {
                foreach (var teammate in Info.Teammates)
                {
                    if (!PathFinder.IsThisNeightbours(Self.ToPoint(), teammate.ToPoint())) continue;
                    AddAction(new Move {Action = ActionType.EndTurn}, Priority.EndTurnNearTeammate,
                              "CanStayNearTeammate", "");
                    return;
                }
            }
        }

        protected override void CanCheckBestPositionForShoutHiddenEnemies()
        {
        }
    }
}