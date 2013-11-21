using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class DefaultBehavior : IBehavior
    {
        protected readonly Trooper Self;
        protected readonly Information Info;
        protected readonly World World;
        protected readonly Game Game;
        public string Step { get; protected set; }

        public DefaultBehavior(World world, Trooper self, Game game)
        {
            Self = self;
            World = world;
            Game = game;
            Info = new Information(world, Self, game);
            BattleManager.UpdatePoint(World.Troopers.Where(x => x.IsTeammate).ToArray());
        }

        public virtual void Run(Move move)
        {
            Step = "Default";
            if (NeedDoSpecialAction(move))
            {
                Step = "NeedDoSpecialAction";
                return;
            }
            if (KillingEnemy(move))
            {
                Step = "KillingEnemy";
                return;
            }
            if (UseGrenade(move))
            {
                Step = "UseGrenade";
                return;
            }
            if (MustHealTeammateOrSelf(move))
            {
                Step = "MustHealTeammateOrSelf";
                return;
            }
            if (ShoutEnemy(move))
            {
                Step = "ShoutEnemy";
                return;
            }
            if (MoveToEnemy(move))
            {
                Step = "MoveToEnemy";
                return;
            }
            if (GatherBonus(move))
            {
                Step = "GatherBonus";
                return;
            }
            if (MoveToTeammate(move))
            {
                Step = "MoveToTeammate";
                return;
            }
            MoveToTarget(move);
            Step = "MoveToTarget";
        }

        protected virtual bool NeedDoSpecialAction(Move move)
        {
            if (NeedDoSpecialAction_Action(move)) return true;
            if (NeedDoSpecialAction_RaiseStance(move)) return true;
            if (NeedDoSpecialAction_EatFieldRation(move)) return true;
            if (NeedMoveToFreeTeammate(move)) return true;

            return false;
        }

        #region NeedDoSpecialActionFunc

        protected bool NeedDoSpecialAction_RaiseStance(Move move)
        {
            if (Info.CanShoutedEnemiesImmediately.Count == 0 && Self.Stance != TrooperStance.Standing && Self.CanChangeStance())
            {
                move.Action = ActionType.RaiseStance;

                return true;
            }

            return false;
        }

        protected bool NeedDoSpecialAction_EatFieldRation(Move move)
        {
            if (Info.VisibleEnemies.Count > 0 && Self.NeedFieldRation())
            {
                move.Action = ActionType.EatFieldRation;

                return true;
            }

            return false;
        }

        protected bool NeedDoSpecialAction_Action(Move move)
        {
            if (Info.Action != AdditionalAction.None)
            {
                if (Info.Action == AdditionalAction.SetLowerStance && Self.CanChangeStance())
                {
                    move.Action = ActionType.LowerStance;

                    return true;
                }
                if (Info.Action == AdditionalAction.MoveTo && Self.CanMove())
                {
                    move.Action = ActionType.Move;
                    move.X = Info.NextPoint.X;
                    move.Y = Info.NextPoint.Y;

                    return true;
                }
            }

            return false;
        }

        protected bool NeedMoveToFreeTeammate(Move move)
        {
            return BattleManager.NeededAction == AdditionalAction.MoveToTargetGlobalPoint && MoveToTarget(move);
        }

        #endregion

        protected virtual bool KillingEnemy(Move move)
        {
            if (Info.CanKilledEnemiesImmediately.Count >= 1 && Self.CanShout())
            {
                var target = Info.CanKilledEnemiesImmediately.FirstOrDefault(x => x.Type == TrooperType.Soldier) ??
                             Info.CanKilledEnemiesImmediately[0];

                move.Action = ActionType.Shoot;
                move.X = target.X;
                move.Y = target.Y;
                return true;
            }

            return false;
        }

        protected virtual bool MustHealTeammateOrSelf(Move move)
        {
            if (Self.Hitpoints <= 75 && Self.CanUseMedikit() &&
                (Info.VisibleEnemies.Count > 0 || Info.Teammates.Count(x => x.Type == TrooperType.FieldMedic) == 0))
            {
                move.Action = ActionType.UseMedikit;
                move.X = Self.X;
                move.Y = Self.Y;

                return true;
            }

            return false;
        }

        protected virtual bool ShoutEnemy(Move move)
        {
            if (Info.CanShoutedEnemiesImmediately.Count > 0 && Self.CanShout())
            {
                var possibleTarget =
                    Info.CanShoutedEnemiesImmediately.Where(x => x.Hitpoints == Info.CanShoutedEnemiesImmediately.Min(y => y.Hitpoints))
                        .ToArray();
                var target = possibleTarget.FirstOrDefault(x => x.Type == TrooperType.FieldMedic);
                target = target ?? possibleTarget[0];
                move.Action = ActionType.Shoot;
                move.X = target.X;
                move.Y = target.Y;

                return true;
            }

            return false;
        }

        protected virtual bool UseGrenade(Move move)
        {
            if (Info.CanUseGrenadeEnemiesImmediately.Count > 0 && Self.CanUseGrenadeImmediately())
            {
                var target =
                    Info.CanUseGrenadeEnemiesImmediately.FirstOrDefault(x => x.Type == TrooperType.FieldMedic) ??
                    Info.CanUseGrenadeEnemiesImmediately.First();
                move.Action = ActionType.ThrowGrenade;
                move.X = target.X;
                move.Y = target.Y;

                return true;
            }

            return false;
        }

        protected virtual bool GatherBonus(Move move)
        {
            if (Info.AvaliableBonuses.Count > 0 && Self.CanMove())
            {
                var pathFinder = new PathFinder(World.Cells);

                var minPath = Info.AvaliableBonuses.Select(
                    x => pathFinder.GetPath(new Point(x.X, x.Y), new Point(Self.X, Self.Y),
                                            GetTeammates())).Min(y => y.Count);
                if (minPath > 5) return false;

                var currentBonuse = Info.AvaliableBonuses.First(x => pathFinder.GetPath(new Point(x.X, x.Y), new Point(Self.X, Self.Y),
                                                                                        GetTeammates()).Count == minPath);
                var nextPoint = pathFinder.GetNextPoint(Self.X, Self.Y, currentBonuse.X, currentBonuse.Y, GetTeammates());
                move.Action = ActionType.Move;
                move.X = nextPoint.X;
                move.Y = nextPoint.Y;

                return true;

            }

            return false;
        }

        protected virtual bool MoveToTeammate(Move move)
        {
            return false;
        }

        protected virtual bool MoveToEnemy(Move move)
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

            return false;
        }

        protected virtual bool MoveToTarget(Move move)
        {
            if (Self.CanMoveCarefully() && Info.VisibleEnemies.Count == 0)
            {
                var targetPoint = BattleManager.CurrentPoint;
                var pathFinder = new PathFinder(World.Cells);
                var nextPoint = pathFinder.GetNextPoint(Self.X, Self.Y, targetPoint.X, targetPoint.Y, GetTeammates());
                if (nextPoint == null)
                {
                    move.Action = ActionType.EndTurn;
                    return true;
                }
                if ((nextPoint.X == Self.X && nextPoint.Y == Self.Y))
                {
                    BattleManager.NeededAction = AdditionalAction.MoveToTargetGlobalPoint;
                }

                move.Action = ActionType.Move;
                move.X = nextPoint.X;
                move.Y = nextPoint.Y;

                return true;
            }
            if (!Self.CanMoveCarefully())
            {
                move.Action = ActionType.EndTurn;
                return false;
            }

            move.Action = ActionType.EndTurn;
            return false;
        }

        protected List<Point> GetTeammates()
        {
            return Info.Teammates.Select(x => new Point(x.X, x.Y)).ToList();
        }
    }
}