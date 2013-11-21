using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class Strategy
    {
    }

    public interface IBehavior
    {
        void Run(Move move);
        string Step { get; }
    }

    public class MedicBehavior : DefaultBehavior
    {
        public MedicBehavior(World world, Trooper self, Game game) : base(world, self, game)
        {
        }

        protected override bool MustHealTeammateOrSelf(Move move)
        {
            var pathFinder = new PathFinder(World.Cells);
            if (Self.CanUseMedikit() && Info.WoundedTeammates.Count(x => x.Hitpoints <= 60) > 0 &&
                Info.VisibleEnemies.Count > 0 && Self.CanMove())
            {
                foreach (var teammate in Info.WoundedTeammates.Where(x => x.Hitpoints <= 60))
                {
                    var path = pathFinder.GetPath(new Point(teammate.X, teammate.Y), new Point(Self.X, Self.Y),
                                                  Info.Teammates.Select(x => new Point(x.X, x.Y)).ToList());
                    if (path.Count == 0)
                    {
                        move.Action = ActionType.UseMedikit;
                        move.X = teammate.X;
                        move.Y = teammate.Y;

                        return true;
                    }
                    if (path.Count*Self.MoveCost() + Game.MedikitUseCost <= Self.ActionPoints)
                    {
                        if (Self.Stance != TrooperStance.Standing)
                        {
                            move.Action = ActionType.RaiseStance;

                            return true;
                        }

                        move.Action = ActionType.Move;
                        move.X = path.First().X;
                        move.Y = path.First().Y;

                        return true;
                    }
                }
            }
            if (Self.CanHeal() && Info.WoundedTeammates.Count > 0)
            {
                foreach (var woundedTeammate in Info.WoundedTeammates)
                {
                    if (Math.Abs(Self.X - woundedTeammate.X) + Math.Abs(Self.Y - woundedTeammate.Y) == 1)
                    {
                        move.Action = ActionType.Heal;
                        move.X = woundedTeammate.X;
                        move.Y = woundedTeammate.Y;

                        return true;
                    }
                }
            }
            if (Self.Hitpoints <= 60 && Self.CanUseMedikit())
            {
                move.Action = ActionType.UseMedikit;
                move.X = Self.X;
                move.Y = Self.Y;

                return true;
            }
            if (Self.Hitpoints < Self.MaximalHitpoints && Self.CanHeal())
            {
                move.Action = ActionType.Heal;
                move.X = Self.X;
                move.Y = Self.Y;

                return true;
            }
            if (Self.CanHeal() && Info.WoundedTeammates.Count > 0 && Self.CanMove())
            {
                var target =
                    Info.WoundedTeammates.First(
                        x => x.Hitpoints == Info.WoundedTeammates.Where(y => y.Hitpoints > 0).Min(y => y.Hitpoints));
                pathFinder = new PathFinder(World.Cells);
                var targetPoint = pathFinder.GetNextPoint(Self.X, Self.Y, target.X, target.Y,
                                                          Info.Teammates.Select(x => new Point(x.X, x.Y)).ToList());
                if (Self.Stance != TrooperStance.Standing)
                {
                    move.Action = ActionType.RaiseStance;

                    return true;
                }
                //TODO: maybe no way to target!
                move.Action = ActionType.Move;
                move.X = targetPoint.X;
                move.Y = targetPoint.Y;

                return true;
            }
            if (Self.Hitpoints < Self.MaximalHitpoints && Self.CanHeal())
            {
                move.Action = ActionType.Heal;
                move.X = Self.X;
                move.Y = Self.Y;

                return true;
            }

            return false;
        }

        protected override bool MoveToTeammate(Move move)
        {
            if (Info.Teammates.Count > 0 && Self.CanMove())
            {
                var targetTemamate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Soldier) ??
                                     Info.Teammates[0];
                var targetCommander = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Commander) ??
                                      Info.Teammates[0];
                var pathFinder = new PathFinder(World.Cells);
                var path = pathFinder.GetPath(new Point(targetTemamate.X, targetTemamate.Y), new Point(Self.X, Self.Y),
                                              GetTeammates());
                if (path.Count > Self.ActionPoints/Self.MoveCost())
                {
                    pathFinder = new PathFinder(World.Cells);
                    path = pathFinder.GetPath(new Point(targetCommander.X, targetCommander.Y), new Point(Self.X, Self.Y),
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

        protected override bool KillingEnemy(Move move)
        {
            if (Info.CanKilledEnemiesImmediately.Count >= 1 && Self.CanShout() && Info.WoundedTeammates.Count(x => x.Hitpoints <= 40) == 0)
            {
                move.Action = ActionType.Shoot;
                move.X = Info.CanKilledEnemiesImmediately[0].X;
                move.Y = Info.CanKilledEnemiesImmediately[0].Y;
                return true;
            }

            return false;
        }

        protected override bool ShoutEnemy(Move move)
        {
            if (Info.CanShoutedEnemiesImmediately.Count == 1 && Self.CanShout() && Info.VisibleEnemies.Count == 1)
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

        protected override bool MoveToEnemy(Move move)
        {
            return false;
        }
    }

    public class SoldierBehavior : DefaultBehavior
    {
        public SoldierBehavior(World world, Trooper self, Game game) : base(world, self, game)
        {
        }

        protected override bool MoveToTeammate(Move move)
        {
            if (Info.Teammates.Count > 0 && Self.CanMove())
            {
                var targetTemamate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Commander);
                if (targetTemamate == null) return false;

                var medicTeammate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.FieldMedic) ??
                                      Info.Teammates[0];
                var pathFinder = new PathFinder(World.Cells);
                var path = pathFinder.GetPath(new Point(targetTemamate.X, targetTemamate.Y), new Point(Self.X, Self.Y),
                                              GetTeammates());
                pathFinder = new PathFinder(World.Cells);
                var pathWithoutTeammates = pathFinder.GetPath(new Point(targetTemamate.X, targetTemamate.Y), new Point(Self.X, Self.Y),
                                              new List<Point>());
                if (path.Count > Self.ActionPoints / Self.MoveCost() && path.Count >= pathWithoutTeammates.Count + 2)
                {
                    pathFinder = new PathFinder(World.Cells);
                    path = pathFinder.GetPath(new Point(medicTeammate.X, medicTeammate.Y), new Point(Self.X, Self.Y),
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

    public class CommanderBehavior : DefaultBehavior
    {
        public CommanderBehavior(World world, Trooper self, Game game) : base(world, self, game)
        {
        }
    }

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
            if (Self.Hitpoints <= 70 && Self.CanUseMedikit() &&
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

    public static class Extensions
    {
        private static Game _game;

        public static void Init(Game game)
        {
            _game = game;
        }

        public static bool CanHeal(this Trooper self)
        {
            return self.ActionPoints >= _game.FieldMedicHealCost && self.Type == TrooperType.FieldMedic;
        }

        public static bool CanUseMedikit(this Trooper self)
        {
            return self.ActionPoints >= _game.MedikitUseCost && self.IsHoldingMedikit;
        }

        public static bool CanShout(this Trooper self)
        {
            return self.ActionPoints >= self.ShootCost;
        }

        public static bool CanMove(this Trooper self)
        {
            return self.Stance == TrooperStance.Standing
                       ? self.ActionPoints >= _game.StandingMoveCost
                       : self.Stance == TrooperStance.Prone
                             ? self.ActionPoints >= _game.ProneMoveCost
                             : self.ActionPoints >= _game.KneelingMoveCost;
        }

        public static bool CanMoveCarefully(this Trooper self)
        {
            return self.Stance == TrooperStance.Standing
                       ? self.ActionPoints >= _game.StandingMoveCost + self.InitialActionPoints/2
                       : self.Stance == TrooperStance.Prone
                             ? self.ActionPoints >= _game.ProneMoveCost + self.InitialActionPoints/2
                             : self.ActionPoints >= _game.KneelingMoveCost + self.InitialActionPoints/2;
        }

        public static int MoveCost(this Trooper self)
        {
            return self.Stance == TrooperStance.Standing
                       ? _game.StandingMoveCost
                       : self.Stance == TrooperStance.Prone
                             ? _game.ProneMoveCost
                             : _game.KneelingMoveCost;
        }

        public static bool CanUseGrenadeImmediately(this Trooper self)
        {
            return self.IsHoldingGrenade && self.ActionPoints >= _game.GrenadeThrowCost;
        }

        public static bool CanUseGrenadeWithFieldRation(this Trooper self)
        {
            return self.IsHoldingGrenade && (self.ActionPoints >= _game.GrenadeThrowCost ||
                                             (self.IsHoldingFieldRation &&
                                              self.ActionPoints + _game.FieldRationBonusActionPoints -
                                              _game.FieldRationEatCost >= _game.GrenadeThrowCost));
        }

        public static bool CanChangeStance(this Trooper self)
        {
            return self.ActionPoints >= _game.StanceChangeCost;
        }

        public static bool NeedFieldRation(this Trooper self)
        {
            return self.IsHoldingFieldRation &&
                   self.ActionPoints >= _game.FieldRationEatCost &&
                   self.ActionPoints - _game.FieldRationEatCost + _game.FieldRationBonusActionPoints <=
                   self.InitialActionPoints;
        }
    }
}