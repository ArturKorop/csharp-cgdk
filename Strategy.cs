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
            if (Self.CanUseMedikit() && Info.WoundedTeammates.Count(x => x.Hitpoints <= 50) > 0 &&
                Info.VisibleEnemies.Count > 0 && Self.CanMove())
            {
                foreach (var teammate in Info.WoundedTeammates.Where(x => x.Hitpoints <= 50))
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
                        move.Action = ActionType.Move;
                        move.X = path.Last().X;
                        move.Y = path.Last().Y;

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
            if (Self.Hitpoints < Self.MaximalHitpoints && Self.CanHeal())
            {
                move.Action = ActionType.Heal;
                move.X = Self.X;
                move.Y = Self.Y;

                return true;
            }
            if (Self.Hitpoints <= 50 && Self.CanUseMedikit())
            {
                move.Action = ActionType.UseMedikit;
                move.X = Self.X;
                move.Y = Self.Y;

                return true;
            }
            if (Self.CanHeal() && Info.WoundedTeammates.Count > 0 && Self.CanMove())
            {
                var target =
                    Info.WoundedTeammates.First(
                        x => x.Hitpoints == Info.WoundedTeammates.Where(y => y.Hitpoints > 0).Min(y => y.Hitpoints));

                var targetPoint = pathFinder.GetNextPoint(Self.X, Self.Y, target.X, target.Y,
                                                          Info.Teammates.Select(x => new Point(x.X, x.Y)).ToList());
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
                //var nextPoint = pathFinder.GetNextPoint(Self.X, Self.Y, targetTemamate.X, targetTemamate.Y,GetTeammates());
                //TODO: possible no way!
                if (path != null && path.Count > 0)
                {
                    move.Action = ActionType.Move;
                    move.X = path.Last().X;
                    move.Y = path.Last().Y;

                    return true;
                }
            }

            return false;
        }

        protected override bool KillingEnemy(Move move)
        {
            if (Info.CanKilledEnemies.Count >= 1 && Self.CanShout() && Info.CanShoutedEnemies.Count == 1)
            {
                move.Action = ActionType.Shoot;
                move.X = Info.CanKilledEnemies[0].X;
                move.Y = Info.CanKilledEnemies[0].Y;
                return true;
            }

            return false;
        }

        protected override bool ShoutEnemy(Move move)
        {
            if (Info.CanShoutedEnemies.Count == 1 && Self.CanShout() && Info.VisibleEnemies.Count == 1)
            {
                var possibleTarget =
                    Info.CanShoutedEnemies.Where(x => x.Hitpoints == Info.CanShoutedEnemies.Min(y => y.Hitpoints))
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
    }

    public class SoldierBehavior : DefaultBehavior
    {
        public SoldierBehavior(World world, Trooper self, Game game) : base(world, self, game)
        {
        }
    }

    public class CommanderBehavior : DefaultBehavior
    {
        public CommanderBehavior(World world, Trooper self, Game game) : base(world, self, game)
        {
        }

        protected override bool MoveToTeammate(Move move)
        {
            if (Info.Teammates.Count > 0 && Self.CanMove())
            {
                var targetTemamate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Soldier);
                if (targetTemamate == null) return false;

                var pathFinder = new PathFinder(World.Cells);
                var nextPoint = pathFinder.GetNextPoint(Self.X, Self.Y, targetTemamate.X, targetTemamate.Y,
                                                        GetTeammates());
                //TODO: possible no way!
                if (nextPoint != null)
                {
                    move.Action = ActionType.Move;
                    move.X = nextPoint.X;
                    move.Y = nextPoint.Y;

                    return true;
                }
            }

            return false;
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
            if (Info.AddActionToKill != AdditionalAction.None)
            {
                if (Info.AddActionToKill == AdditionalAction.FromStandingToKneel && Self.CanChangeStance())
                {
                    move.Action = ActionType.LowerStance;

                    return true;
                }
                if (Info.AddActionToKill == AdditionalAction.UseGrenade && Info.CanKilledEnemies.Count > 0 &&
                    Self.CanUseGrenadeImmediately())
                {
                    move.Action = ActionType.ThrowGrenade;
                    move.X = Info.CanKilledEnemies.First().X;
                    move.Y = Info.CanKilledEnemies.First().Y;

                    return true;
                }
            }
            if (Info.CanShoutedEnemies.Count == 0 && Self.Stance != TrooperStance.Standing && Self.CanChangeStance())
            {
                move.Action = ActionType.RaiseStance;

                return true;
            }
            if (Info.VisibleEnemies.Count > 0 && Self.NeedFieldRation())
            {
                move.Action = ActionType.EatFieldRation;

                return true;
            }
            if (BattleManager.NeededAction == AdditionalAction.MoveToTargetGlobalPoint)
            {
                return MoveToTarget(move);
            }

            return false;
        }

        protected virtual bool KillingEnemy(Move move)
        {
            if (Info.CanKilledEnemies.Count >= 1 && Self.CanShout())
            {
                move.Action = ActionType.Shoot;
                move.X = Info.CanKilledEnemies[0].X;
                move.Y = Info.CanKilledEnemies[0].Y;
                return true;
            }

            return false;
        }

        protected virtual bool MustHealTeammateOrSelf(Move move)
        {
            if (Self.Hitpoints <= 50 && Self.CanUseMedikit() &&
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
            if (Info.CanShoutedEnemies.Count > 0 && Self.CanShout())
            {
                var possibleTarget =
                    Info.CanShoutedEnemies.Where(x => x.Hitpoints == Info.CanShoutedEnemies.Min(y => y.Hitpoints))
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

        protected virtual bool GatherBonus(Move move)
        {
            if (Info.AvaliableBonuses.Count > 0 && Self.CanMove())
            {
                var pathFinder = new PathFinder(World.Cells);
                var medikit = Info.AvaliableBonuses.FirstOrDefault(x => x.Type == BonusType.Medikit);
                if (medikit != null)
                {
                    var target = pathFinder.GetNextPoint(Self.X, Self.Y, medikit.X, medikit.Y,
                                                         Info.Teammates.Select(x => new Point(x.X, x.Y)).ToList());
                    move.Action = ActionType.Move;
                    move.X = target.X;
                    move.Y = target.Y;
                    return true;
                }
            }

            return false;
        }

        protected virtual bool MoveToTeammate(Move move)
        {
            return false;
        }

        protected virtual bool MoveToEnemy(Move move)
        {
            if (Info.VisibleEnemies.Count > 0 && Self.CanMoveCarefully())
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

            return false;
        }

        protected virtual bool MoveToTarget(Move move)
        {
            if (Self.CanMoveCarefully())
            {
                var targetPoint = BattleManager.CurrentPoint;
                var pathFinder = new PathFinder(World.Cells);
                var nextPoint = pathFinder.GetNextPoint(Self.X, Self.Y, targetPoint.X, targetPoint.Y, GetTeammates());
                //TODO: possible no way!
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

            move.Action = ActionType.EndTurn;
            return false;
        }

        protected List<Point> GetTeammates()
        {
            return Info.Teammates.Select(x => new Point(x.X, x.Y)).ToList();
        }
    }

    public class Information
    {
        private readonly World _world;
        private readonly Trooper _self;
        private readonly Game _game;

        public List<Trooper> VisibleEnemies { get; private set; }
        public List<Trooper> CanShoutedEnemies { get; private set; }
        public List<Trooper> CanKilledEnemies { get; private set; }
        public List<Trooper> Teammates { get; private set; }
        public List<Trooper> WoundedTeammates { get; private set; }
        public List<Bonus> AvaliableBonuses { get; private set; }
        public AdditionalAction AddActionToKill = AdditionalAction.None;
        public Point NextPoint { get; set; }

        public Information(World world, Trooper self, Game game)
        {
            _world = world;
            _self = self;
            _game = game;

            CheckVisibleEnemies();
            CheckCanShoutEnemies();
            CheckTeammates();
            CheckWoundedTeammates();
            CheckAvaliableBonuses();
            CheckCanKilledEnemies();
        }

        private void CheckVisibleEnemies()
        {
            VisibleEnemies = _world.Troopers.Where(x => !x.IsTeammate && x.Hitpoints != 0).ToList();
        }

        private void CheckCanShoutEnemies()
        {
            CanShoutedEnemies =
                _world.Troopers.Where(
                    x =>
                    !x.IsTeammate &&
                    _world.IsVisible(_self.ShootingRange, _self.X, _self.Y, _self.Stance, x.X, x.Y, x.Stance)).ToList();
        }

        private void CheckTeammates()
        {
            Teammates = _world.Troopers.Where(x => x.IsTeammate && x.Id != _self.Id).ToList();
        }

        private void CheckWoundedTeammates()
        {
            WoundedTeammates =
                _world.Troopers.Where(x => x.IsTeammate && x.Hitpoints < x.MaximalHitpoints && x.Id != _self.Id)
                      .ToList();
        }

        private void CheckAvaliableBonuses()
        {
            var troopers = _world.Troopers.Where(x => x.Id != _self.Id);
            AvaliableBonuses =
                _world.Bonuses.Where(x => ((x.Type == BonusType.FieldRation && !_self.IsHoldingFieldRation) ||
                                           (x.Type == BonusType.Medikit && !_self.IsHoldingMedikit) ||
                                           (x.Type == BonusType.Grenade && !_self.IsHoldingGrenade)) &&
                                          troopers.All(y => y.X != x.X && y.Y != x.Y)).ToList();
        }

        private void CheckCanKilledEnemies()
        {
            if (CanShoutedEnemies == null) return;

            var defaultDmg = _self.GetDamage(_self.Stance)*((int) _self.ActionPoints/_self.ShootCost);
            CanKilledEnemies = CanShoutedEnemies.Where(x => (x.Hitpoints <= defaultDmg)).ToList();
            if (CanKilledEnemies.Count > 0) return;

            var dmg = _self.GetDamage(TrooperStance.Kneeling);
            var proneDmg = dmg*((int) (_self.ActionPoints - _game.StanceChangeCost)/_self.ShootCost);
            if (proneDmg > defaultDmg)
            {
                defaultDmg = proneDmg;

                var tempCanShoutEnemies = _world.Troopers.Where(
                    x =>
                    !x.IsTeammate &&
                    _world.IsVisible(_self.ShootingRange, _self.X, _self.Y, TrooperStance.Kneeling, x.X, x.Y, x.Stance))
                                                .ToList();
                CanKilledEnemies = tempCanShoutEnemies.Where(x => (x.Hitpoints <= defaultDmg
                                                                  )).ToList();
                if (tempCanShoutEnemies.Count > 0)
                {
                    AddActionToKill = AdditionalAction.FromStandingToKneel;
                    CanShoutedEnemies = tempCanShoutEnemies;

                    return;
                }
            }
            if (_self.CanUseGrenadeImmediately())
            {
                var tempGrenadeEnemies =
                    _world.Troopers.Where(
                        x =>
                        !x.IsTeammate &&
                        _world.IsVisible(_game.GrenadeThrowRange, _self.X, _self.Y, TrooperStance.Standing, x.X, x.Y,
                                         TrooperStance.Standing)).ToList();
                if (tempGrenadeEnemies.Any())
                {
                    AddActionToKill = AdditionalAction.UseGrenade;
                    CanKilledEnemies = tempGrenadeEnemies.ToList();
                }
            }
        }
    }

    public enum AdditionalAction
    {
        None,
        FromStandingToKneel,
        UseGrenade,
        UseFieldRation,
        MoveTo,
        MoveToTargetGlobalPoint
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