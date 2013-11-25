using System;
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
        protected readonly PathFinder Path;
        public string StepInfo { get; protected set; }
        public string AddInfo { get; protected set; }

        public DefaultBehavior(World world, Trooper self, Game game)
        {
            Self = self;
            World = world;
            Game = game;
            Path = new PathFinder(World.Cells);
            Info = new Information(world, Self, game);
            BattleManager.UpdatePoint(World.Troopers.Where(x => x.IsTeammate).ToArray());
        }

        public virtual void Run(Move move)
        {
            StepInfo = "Default";

            StepInfo = "NeedDoSpecialAction";
            if (NeedDoSpecialAction(move)) return;

            StepInfo = "CheckBestPosition";
            if (CheckBestPosition(move)) return;

            StepInfo = "CheckPositionForTeammates";
            if (CheckPositionForTeammates(move)) return;

            StepInfo = "RaiseStance";
            if (RaiseStance(move)) return;

            StepInfo = "LowerStance";
            if (LowerStance(move)) return;

            StepInfo = "KillingEnemy";
            if (KillingEnemy(move)) return;

            StepInfo = "UseGrenade";
            if (UseGrenade(move)) return;

            StepInfo = "MustHealSelf";
            if (MustHealSelf(move)) return;

            StepInfo = "MustHealTeammate";
            if (MustHealTeammate(move)) return;

            StepInfo = "ShoutEnemy";
            if (ShoutEnemy(move)) return;

            StepInfo = "GatherBonus";
            if (GatherBonus(move)) return;

            StepInfo = "MoveToTeammate";
            if (MoveToTeammate(move)) return;

            StepInfo = "MoveToTarget";
            if(MoveToTarget(move)) return;

            StepInfo = "Retreat";
            if (Retreat(move)) return;

            StepInfo = "EndTurn";
            EndTurn(move);

        }

        #region BestBehavior

        protected virtual bool NeedDoSpecialAction(Move move)
        {

            if (NeedDoSpecialAction_EatFieldRation(move)) return true;
            if (NeedMoveToFreeTeammate(move)) return true;

            return false;
        }

        #region NeedDoSpecialActionFunc

        protected bool NeedDoSpecialAction_EatFieldRation(Move move)
        {
            if (Info.VisibleEnemies.Count > 0 && Self.NeedFieldRation())
            {
                move.Action = ActionType.EatFieldRation;

                return true;
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
                             Info.CanKilledEnemiesImmediately.FirstOrDefault(x => x.Type == TrooperType.Commander) ??
                             Info.CanKilledEnemiesImmediately[0];

                move.Action = ActionType.Shoot;
                move.X = target.X;
                move.Y = target.Y;
                AddInfo += String.Format(" Enemy[{0},{1}]", target.Type, target.Hitpoints);

                return true;
            }

            return false;
        }

        protected virtual bool MustHealSelf(Move move)
        {
            if (!Self.CanUseMedikit()) return false;

            if (Self.Hitpoints <= 70 && Info.Teammates.Count(x => x.Type == TrooperType.FieldMedic) == 0)
            {
                move.Action = ActionType.UseMedikit;
                move.X = Self.X;
                move.Y = Self.Y;

                return true;
            }

            return false;
        }

        protected virtual bool MustHealTeammate(Move move)
        {
            if (!Self.CanUseMedikit() || Info.WoundedTeammates.Count == 0) return false;

            var woundedNeighbourTeammates =
                Info.WoundedTeammates.Where(
                    woundedTeammate =>
                    PathFinder.IsThisNeightbours(Self.ToPoint(), woundedTeammate.ToPoint()) &&
                    woundedTeammate.Hitpoints <= 60).ToList();
            if (woundedNeighbourTeammates.Count == 0) return false;

            woundedNeighbourTeammates.Sort((x, y) => x.Hitpoints.CompareTo(y.Hitpoints));
            move.Action = ActionType.UseMedikit;
            move.X = woundedNeighbourTeammates.First().X;
            move.Y = woundedNeighbourTeammates.First().Y;
            AddInfo += String.Format(" Teammate[{0},{1}]", woundedNeighbourTeammates.First().Type, woundedNeighbourTeammates.First().Hitpoints);

            return true;
        }

        protected virtual bool CheckBestPosition(Move move)
        {
            if (Info.CanKilledEnemiesImmediately.Any() || Info.CanShoutedEnemiesImmediately.Count > 0 ||
                Info.VisibleEnemies.Count == 0 || Self.Stance != TrooperStance.Standing || !Self.CanMove()) return false;

            return CheckBestPositionCalc(move, null);
        }

        private bool CheckBestPositionCalc(Move move, List<Point> forbiddenPoints)
        {
            var avaliablePoints = new List<Point>();
            foreach (var visibleEnemy in Info.VisibleEnemies)
            {
                var teammatesAndForbiddenPoints = GetTeammates();
                if (forbiddenPoints != null) teammatesAndForbiddenPoints.AddRange(forbiddenPoints);

                avaliablePoints.AddRange(Path.AvaliablePositionToShout(Self, visibleEnemy, World,
                                                                       teammatesAndForbiddenPoints));
            }

            if (avaliablePoints.Count == 0) return false;

            var minPath =
                avaliablePoints.Select(x => Path.GetPathToPoint(x, Self.ToPoint(), GetTeammates()))
                               .OrderBy(x => x.Count)
                               .First();
            if (minPath.Count == 0) return false;

            move.Action = ActionType.Move;
            move.X = minPath.First().X;
            move.Y = minPath.First().Y;

            return true;
        }

        protected virtual bool CheckPositionForTeammates(Move move)
        {
            if (Info.CanKilledEnemiesImmediately.Any() || Info.CanShoutedEnemiesImmediately.Count == 0 ||
                Info.Teammates.Count == 0 || !Self.CanMove()) return false;

            var otherDDTeamate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Soldier) ??
                                 Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Commander);
            if (otherDDTeamate == null) return false;

            var info = new Information(World, otherDDTeamate, Game);
            if (info.CanKilledEnemiesImmediately.Any() || info.CanShoutedEnemiesImmediately.Any()) return false;

            var avaliablePointsForTeammate = new List<Point>();
            foreach (var visibleEnemy in Info.VisibleEnemies)
                avaliablePointsForTeammate.AddRange(Path.AvaliablePositionToShout(otherDDTeamate, visibleEnemy, World,
                                                                                  GetTeammates()));

            if (avaliablePointsForTeammate.Count == 0) return false;

            var minPath =
                avaliablePointsForTeammate.Select(
                    x =>
                    Path.GetPathToPoint(x, otherDDTeamate.ToPoint(),
                                        Info.Teammates.Where(y => y.Id != Self.Id).Select(z => z.ToPoint()).ToList()))
                                          .OrderBy(x => x.Count)
                                          .First();
            if (minPath.Count == 0) return false;

            if (minPath.Contains(Self.ToPoint()))
                return CheckBestPositionCalc(move, new List<Point> { Self.ToPoint() });

            return false;
        }

        protected virtual bool ShoutEnemy(Move move)
        {
            if (Info.CanShoutedEnemiesImmediately.Count > 0 && Self.CanShout())
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
                AddInfo += String.Format(" Enemy[{0},{1}]", target.Type, target.Hitpoints);

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
                AddInfo += String.Format(" Enemy[{0},{1}]", target.Type, target.Hitpoints);

                return true;
            }

            return false;
        }

        protected virtual bool GatherBonus(Move move)
        {
            if (Info.AvaliableBonuses.Count > 0 && ((Self.CanMove() && BattleManager.Step < BattleManager.StepCarefullCount) || (Self.CanMoveCarefully() && BattleManager.Step >= BattleManager.StepCarefullCount)))
            {
                var pathFinder = new PathFinder(World.Cells);

                var minPath = Info.AvaliableBonuses.Select(
                    x => pathFinder.GetPathToNeighbourCell(new Point(x.X, x.Y), new Point(Self.X, Self.Y),
                                                           GetTeammates())).Min(y => y.Count);
                if (minPath > 5) return false;

                var currentBonuse =
                    Info.AvaliableBonuses.First(
                        x => pathFinder.GetPathToNeighbourCell(new Point(x.X, x.Y), new Point(Self.X, Self.Y),
                                                               GetTeammates()).Count == minPath);
                var nextPoint = pathFinder.GetNextPoint(Self.X, Self.Y, currentBonuse.X, currentBonuse.Y, GetTeammates());
                move.Action = ActionType.Move;
                move.X = nextPoint.X;
                move.Y = nextPoint.Y;
                AddInfo += String.Format(" Bonus[{0}[{1},{2}]]", currentBonuse.Type, currentBonuse.X, currentBonuse.Y);

                return true;
            }

            return false;
        }

        protected virtual bool RaiseStance(Move move)
        {
            if (!Self.CanChangeStance() || Info.CanShoutedEnemiesImmediately.Any() || Self.Stance == TrooperStance.Standing) return false;

            move.Action = ActionType.RaiseStance;

            return true;
        }

        protected virtual bool LowerStance(Move move)
        {
            if (!Self.CanChangeStance() || Info.CanShoutedEnemiesImmediately.Count == 0 ||
                Self.Type == TrooperType.FieldMedic || Self.Stance == TrooperStance.Prone) return false;

            int currentDamage = Self.GetDamage(Self.Stance) * (Self.ActionPoints / Self.ShootCost);
            TrooperStance lowerStance = Self.Stance == TrooperStance.Standing
                                            ? TrooperStance.Kneeling
                                            : TrooperStance.Prone;
            if (!World.IsVisible(Self.ShootingRange, Self.X, Self.Y, lowerStance, Info.CanShoutedEnemiesImmediately[0].X,
                                 Info.CanShoutedEnemiesImmediately[0].Y, Info.CanShoutedEnemiesImmediately[0].Stance))
                return false;

            var lowerStanceDamage = Self.GetDamage(lowerStance) *
                                    ((Self.ActionPoints - Game.StanceChangeCost) / Self.ShootCost);
            if (lowerStanceDamage >= currentDamage)
            {
                move.Action = ActionType.LowerStance;

                return true;
            }

            return false;
        }

        protected virtual void EndTurn(Move move)
        {
            move.Action = ActionType.EndTurn;
        }


        #endregion

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
                AddInfo += String.Format(" Carefully Enemy[{0},{1}]", target.Type, target.Hitpoints);

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
                AddInfo += String.Format(" Fighting Enemy[{0},{1}]", target.Type, target.Hitpoints);

                return true;
            }

            return false;
        }

        protected virtual bool MoveToTarget(Move move)
        {
            if (((Self.CanMove() && BattleManager.Step < BattleManager.StepCarefullCount) || (Self.CanMoveCarefully() && BattleManager.Step >= BattleManager.StepCarefullCount)) && Info.VisibleEnemies.Count == 0)
            {
                var targetPoint = BattleManager.CurrentPoint;
                var pathFinder = new PathFinder(World.Cells);
                var path = pathFinder.GetPathToNeighbourCell(new Point(targetPoint.X, targetPoint.Y), Self.ToPoint(),
                                                           GetTeammates());
                var nextPoint = path.FirstOrDefault();
                if (nextPoint == null)
                {
                    move.Action = ActionType.EndTurn;

                    return false;
                }
                if ((nextPoint.X == Self.X && nextPoint.Y == Self.Y))
                {
                    BattleManager.NeededAction = AdditionalAction.MoveToTargetGlobalPoint;

                    return false;
                }

                move.Action = ActionType.Move;
                move.X = nextPoint.X;
                move.Y = nextPoint.Y;
                AddInfo += String.Format(" TargetPoint[{0},{1}]", targetPoint.X, targetPoint.Y);

                return true;
            }

            return false;
        }

        protected virtual bool Retreat(Move move)
        {
            if (!Self.CanMove() || Info.DangerEnemies.Count == 0 || Info.VisibleEnemies.Count == 0) return false;

            var nextPoint = Path.GetSafePoint(Self, Info.VisibleEnemies, World, GetTeammates());
            if (nextPoint == null) return false;

            move.Action = ActionType.Move;
            move.X = nextPoint.X;
            move.Y = nextPoint.Y;

            return true;
        }
        
        protected List<Point> GetTeammates()
        {
            return Info.Teammates.Select(x => new Point(x.X, x.Y)).ToList();
        }
    }
}