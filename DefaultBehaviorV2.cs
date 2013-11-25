using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class DefaultBehaviorV2 : IBehavior
    {
        protected World World;
        protected Trooper Self;
        protected Game Game;
        protected Information Info;
        protected PathFinder CurrentPathFinder;
        protected Move Move;
        protected List<MoveAction> PriorityActions = new List<MoveAction>();
        protected event Action PossibleActions;

        public string StepInfo { get; private set; }
        public string AddInfo { get; private set; }

        public DefaultBehaviorV2(World world, Trooper self, Game game)
        {
            World = world;
            Self = self;
            Game = game;
            Info = new Information(World, Self, Game);
            CurrentPathFinder = new PathFinder(World.Cells);

            PossibleActions += CanKillEnemies;
            PossibleActions += CanShoutEnemies;
            PossibleActions += CanEatFieldRation;
            PossibleActions += CanRaiseStance;
            PossibleActions += CanLowerStance;
            PossibleActions += CanEndTurn;
            PossibleActions += CanGatherBonus;
            PossibleActions += CanUseGrenade;
            PossibleActions += CanCheckBestPosition;
            PossibleActions += CanCheckPositionForTeammates;
            PossibleActions += CanMoveToTarget;
            PossibleActions += CanHealSelf;
            PossibleActions += CanHealTeammate;
            PossibleActions += CanMoveToTeammate;
            PossibleActions += CanWaitAll;
        }

        public void Run(Move move)
        {
            BattleManagerV2.Update(World.Troopers.Where(x=>x.IsTeammate).ToList());
            PossibleActions();
            var currentMove = PriorityActions.Single(pa => pa.Priority == PriorityActions.Max(pam => pam.Priority));
            StepInfo = currentMove.Message;
            AddInfo = currentMove.AdditionalInfo;
            move.Action = currentMove.Move.Action;
            move.X = currentMove.Move.X;
            move.Y = currentMove.Move.Y;
        }

        protected virtual void CanKillEnemies()
        {
            if (Info.CanKilledEnemiesImmediately.Count == 0 || !Self.CanShout()) return;

            var targetEnemy = Info.CanKilledEnemiesImmediately.FirstOrDefault(e => e.Type == TrooperType.Sniper) ??
                              Info.CanShoutedEnemiesImmediately.FirstOrDefault(e => e.Type == TrooperType.Soldier) ??
                              Info.CanShoutedEnemiesImmediately.FirstOrDefault(e => e.Type == TrooperType.Commander) ??
                              Info.CanShoutedEnemiesImmediately.First();

            AddAction(new Move {Action = ActionType.Shoot, X = targetEnemy.X, Y = targetEnemy.Y}, Priority.Kill,
                      "CanKillEnemies",
                      String.Format("Enemy[{0}{1}] - {2}", targetEnemy.X, targetEnemy.Y, targetEnemy.Type));
        }

        protected virtual void CanShoutEnemies()
        {
            if (Info.CanShoutedEnemiesImmediately.Count == 0 || !Self.CanShout()) return;

            var targetEnemy = Info.CanKilledEnemiesImmediately.FirstOrDefault(e => e.Type == TrooperType.FieldMedic) ??
                              Info.CanShoutedEnemiesImmediately.FirstOrDefault(e => e.Type == TrooperType.Sniper) ??
                              Info.CanShoutedEnemiesImmediately.FirstOrDefault(e => e.Type == TrooperType.Commander) ??
                              Info.CanShoutedEnemiesImmediately.First();

            AddAction(new Move {Action = ActionType.Shoot, X = targetEnemy.X, Y = targetEnemy.Y}, Priority.Shout,
                      "CanShoutEnemies",
                      String.Format("Enemy[{0}{1}] - {2}", targetEnemy.X, targetEnemy.Y, targetEnemy.Type));
        }

        protected virtual void CanEatFieldRation()
        {
            if (Self.NeedFieldRation() && Info.CanShoutedEnemiesImmediately.Any())
            {
                AddAction(new Move {Action = ActionType.EatFieldRation}, Priority.EatFieldRation, "CanEatFieldRation",
                          String.Format("CurrentAP: {0}", Self.ActionPoints));
            }
        }

        protected virtual void CanRaiseStance()
        {
            if (!Self.CanChangeStance() || Self.Stance == TrooperStance.Standing) return;

            if (Info.VisibleEnemies.Count == 0)
                AddAction(new Move {Action = ActionType.RaiseStance}, Priority.RauseStanceNotInFight, "CanRaiseStance",
                          "No visible enemies");
            else if (Info.VisibleEnemies.Any())
                AddAction(new Move {Action = ActionType.RaiseStance}, Priority.RaiseStanceInFight, "CanRaiseStance",
                          "Visible enemies");
        }

        protected virtual void CanLowerStance()
        {
            if (Info.CanShoutedEnemiesImmediately.Count == 0 || !Self.CanChangeStance()) return;

            int currentDamage = Self.GetDamage(Self.Stance)*(Self.ActionPoints/Self.ShootCost);
            TrooperStance lowerStance = Self.Stance == TrooperStance.Standing
                                            ? TrooperStance.Kneeling
                                            : TrooperStance.Prone;

            if (
                !World.IsVisible(Self.ShootingRange, Self.X, Self.Y, lowerStance, Info.CanShoutedEnemiesImmediately[0].X,
                                 Info.CanShoutedEnemiesImmediately[0].Y, Info.CanShoutedEnemiesImmediately[0].Stance))
                return;

            var lowerStanceDamage = Self.GetDamage(lowerStance)*
                                    ((Self.ActionPoints - Game.StanceChangeCost)/Self.ShootCost);
            if (lowerStanceDamage >= currentDamage)
                AddAction(new Move {Action = ActionType.LowerStance}, Priority.LowerStance, "CanLowerStance", "");
        }

        protected virtual void CanEndTurn()
        {
            AddAction(new Move {Action = ActionType.EndTurn}, Priority.EndTurn, "EndTurn", "");
        }

        protected virtual void CanGatherBonus()
        {
            if (Info.AvaliableBonuses.Count == 0 || !Self.CanMoveCarefully()) return;
            
            var minPath =
                Info.AvaliableBonuses.Select(
                    x =>
                    CurrentPathFinder.GetPathToNeighbourCell(new Point(x.X, x.Y), new Point(Self.X, Self.Y), GetTeammates()))
                    .Min(y => y.Count);
            var currentBonuse =
                Info.AvaliableBonuses.First(
                    x =>
                    CurrentPathFinder.GetPathToNeighbourCell(new Point(x.X, x.Y), new Point(Self.X, Self.Y), GetTeammates())
                              .Count == minPath);
            var nextPoint = CurrentPathFinder.GetNextPoint(Self.X, Self.Y, currentBonuse.X, currentBonuse.Y, GetTeammates());
            AddAction(new Move {Action = ActionType.Move, X = nextPoint.X, Y = nextPoint.Y}, Priority.GatherBonus,
                      "CanGatherBonus",
                      String.Format("Bonus[{0},{1}] - {2}", currentBonuse.X, currentBonuse.Y, currentBonuse.Type));
        }

        protected virtual void CanUseGrenade()
        {
            if (!Info.CanUseGrenadeEnemiesImmediately.Any() || !Self.CanUseGrenadeImmediately()) return;

            var targetEnemy =
                Info.CanUseGrenadeEnemiesImmediately.FirstOrDefault(x => x.Type == TrooperType.FieldMedic) ??
                Info.CanUseGrenadeEnemiesImmediately.First();
            AddAction(new Move {Action = ActionType.ThrowGrenade, X = targetEnemy.X, Y = targetEnemy.Y},
                      Priority.UseGrenade, "CanUseGrenade",
                      String.Format("Enemy[{0}{1}] - {2}", targetEnemy.X, targetEnemy.Y, targetEnemy.Type));
        }

        protected virtual void CanCheckBestPosition()
        {
            if (Info.VisibleEnemies.Count == 0 || Self.Stance != TrooperStance.Standing || !Self.CanMove()) return;

            CheckBestPositionCalc(null);
        }

        protected virtual void CanCheckPositionForTeammates()
        {
            if (Info.CanShoutedEnemiesImmediately.Count == 0 ||
                Info.Teammates.Count == 0 || !Self.CanMove()) return;

            var otherDDTeamate = Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Soldier) ??
                                 Info.Teammates.FirstOrDefault(x => x.Type == TrooperType.Commander);
            if (otherDDTeamate == null) return;

            var info = new Information(World, otherDDTeamate, Game);
            if (info.CanKilledEnemiesImmediately.Any() || info.CanShoutedEnemiesImmediately.Any()) return;

            var avaliablePointsForTeammate = new List<Point>();
            foreach (var visibleEnemy in Info.VisibleEnemies)
                avaliablePointsForTeammate.AddRange(CurrentPathFinder.AvaliablePositionToShout(otherDDTeamate,
                                                                                               visibleEnemy, World,
                                                                                               GetTeammates()));

            if (avaliablePointsForTeammate.Count == 0) return;

            var minPath =
                avaliablePointsForTeammate.Select(
                    x =>
                    CurrentPathFinder.GetPathToPoint(x, otherDDTeamate.ToPoint(),
                                                     Info.Teammates.Where(y => y.Id != Self.Id)
                                                         .Select(z => z.ToPoint())
                                                         .ToList()))
                                          .OrderBy(x => x.Count)
                                          .First();
            if (minPath.Count == 0) return;

            if (minPath.Contains(Self.ToPoint()))
                CheckBestPositionCalc(new List<Point> {Self.ToPoint()});
        }

        protected virtual void CanMoveToTarget()
        {
            if (Info.VisibleEnemies.Any() ||
                !((Self.CanMove() && BattleManager.Step < BattleManager.StepCarefullCount) ||
                  (Self.CanMoveCarefully() && BattleManager.Step >= BattleManager.StepCarefullCount))) return;

            var targetPoint = BattleManager.CurrentPoint;
            var pathFinder = new PathFinder(World.Cells);
            var path = pathFinder.GetPathToNeighbourCell(new Point(targetPoint.X, targetPoint.Y), Self.ToPoint(),
                                                         GetTeammates());
            if(path == null) return;
            var nextPoint = path.FirstOrDefault();
            if (nextPoint == null)
                return;

            if ((nextPoint.X == Self.X && nextPoint.Y == Self.Y))
            {
                BattleManager.NeededAction = AdditionalAction.MoveToTargetGlobalPoint;

                return;
            }

            AddAction(new Move {Action = ActionType.Move, X = nextPoint.X, Y = nextPoint.Y}, Priority.MoveToWayPoint,
                      "CanMoveToTarget", String.Format("WayPoint[{0},{1}]", targetPoint.X, targetPoint.Y));
        }

        protected virtual void CanHealSelf()
        {
            if (!Self.CanUseMedikit()) return;

            if (Self.Hitpoints <= 70 && Info.Teammates.Count(x => x.Type == TrooperType.FieldMedic) == 0)
                AddAction(new Move {Action = ActionType.UseMedikit, X = Self.X, Y = Self.Y}, Priority.HealSelf,
                          "CanHealSelf", "");
        }

        protected virtual void CanHealTeammate()
        {
            if (!Self.CanUseMedikit() || Info.WoundedTeammates.Count == 0) return;

            var woundedNeighbourTeammates =
                Info.WoundedTeammates.Where(
                    woundedTeammate =>
                    PathFinder.IsThisNeightbours(Self.ToPoint(), woundedTeammate.ToPoint()) &&
                    woundedTeammate.Hitpoints <= 60).ToList();
            if (woundedNeighbourTeammates.Count == 0) return;

            woundedNeighbourTeammates.Sort((x, y) => x.Hitpoints.CompareTo(y.Hitpoints));
            AddAction(
                new Move
                    {
                        Action = ActionType.UseMedikit,
                        X = woundedNeighbourTeammates.First().X,
                        Y = woundedNeighbourTeammates.First().Y
                    }, Priority.HealTeammate, "CanHealTeammate", "");
        }

        protected virtual void CanMoveToTeammate()
        {
            if (!Self.CanMove() || Info.Teammates.Count == 0 || Self.Id == BattleManagerV2.HeadOfSquad.Id) return;

            var path = CurrentPathFinder.GetPathToNeighbourCell(BattleManagerV2.HeadOfSquad.ToPoint(), Self.ToPoint(), GetTeammates());
            if (path.Count == 0) return;

            var nextPoint = path.First();
            AddAction(new Move {Action = ActionType.Move, X = nextPoint.X, Y = nextPoint.Y}, Priority.MoveToTeammate,
                      "CanMoveToTeammate",
                      String.Format("Teammate - [{0},{1}]", path.Last().X, path.Last().Y));
        }

        protected virtual void CanWaitAll()
        {
            if (!Self.CanMove() || Info.Teammates.Count == 0 || Self.Id != BattleManagerV2.HeadOfSquad.Id) return;

            var pathes =
                Info.Teammates.Select(x => CurrentPathFinder.GetPathToNeighbourCell(Self.ToPoint(), x.ToPoint(), new List<Point>()));

            var maxPath = pathes.Max(x=>x.Count);
            if(maxPath >= 4)
                AddAction(new Move { Action = ActionType.EndTurn }, Priority.WaitAll, "CanWaitAll", "");
        }

        protected void AddAction(Move move, Priority priority, string message, string addInfo)
        {
            PriorityActions.Add(new MoveAction(move, (int) priority) {Message = message, AdditionalInfo = addInfo});
        }

        protected List<Point> GetTeammates()
        {
            return Info.Teammates.Select(x => new Point(x.X, x.Y)).ToList();
        }

        private void CheckBestPositionCalc(List<Point> forbiddenPoints)
        {
            var avaliablePoints = new List<Point>();
            foreach (var visibleEnemy in Info.VisibleEnemies)
            {
                var teammatesAndForbiddenPoints = GetTeammates();
                if (forbiddenPoints != null) teammatesAndForbiddenPoints.AddRange(forbiddenPoints);

                avaliablePoints.AddRange(CurrentPathFinder.AvaliablePositionToShout(Self, visibleEnemy, World,
                                                                                    teammatesAndForbiddenPoints));
            }

            if (avaliablePoints.Count == 0) return;
            if (avaliablePoints.Contains(Self.ToPoint())) return;

            var minPath =
                avaliablePoints.Select(x => CurrentPathFinder.GetPathToPoint(x, Self.ToPoint(), GetTeammates()))
                               .OrderBy(x => x.Count)
                               .First();
            if (minPath.Count == 0) return;

            AddAction(new Move {Action = ActionType.Move, X = minPath.First().X, Y = minPath.First().Y},
                      Priority.SetBestPosition, "CheckBestPositionCalc", "");
        }
    }

    public static class BattleManagerV2
    {
        public static int Step;
        public static List<TrooperStatus> TeamStatus;
        public static Trooper HeadOfSquad;

        public static void Update(List<Trooper> team)
        {
            Step++;
            if (TeamStatus == null)
                TeamStatus = new List<TrooperStatus>();

            foreach (var trooper in team)
            {
                TeamStatus.Add(new TrooperStatus {Trooper = trooper, Status = TrooperCurrentStatus.None});
            }

            HeadOfSquad = team.FirstOrDefault(x => x.Type == TrooperType.Commander) ??
                          team.FirstOrDefault(x => x.Type == TrooperType.Soldier) ??
                          team[0];
        }

        public static void UpdateTrooper(Trooper self, TrooperCurrentStatus status)
        {
            var trooper = TeamStatus.First(x => x.Trooper.Id == self.Id);
            trooper.Status = status;
        }
    }

    public class TrooperStatus
    {
        public Trooper Trooper;
        public TrooperCurrentStatus Status;
    }

    public enum TrooperCurrentStatus
    {
        None,
        MovingToWaypoint,
        MovingToTeammate,
        Fighting,
        GatheringBonuses
    }

    public enum Priority
    {
        Kill = 100,
        RauseStanceNotInFight = 99,
        UseGrenade = 98,
        HealTeammate = 95,
        HealSelf = 90,
        SetBestPosition = 85,
        EatFieldRation = 83,
        LowerStance = 80,
        Shout = 75,
        RaiseStanceInFight = 65,
        WaitAll = 63,
        GatherBonus = 60,
        MoveToTeammate = 55,
        MoveToWayPoint = 50,
        EndTurn = 0
    }

    public struct MoveAction
    {
        public Move Move;
        public int Priority;
        public string Message;
        public string AdditionalInfo;

        public MoveAction(Move move, int priority)
            : this()
        {
            Move = move;
            Priority = priority;
        }

        public override string ToString()
        {
            return String.Format("{5}   {0}: {1}[{2},{3}] - {4}", Message, Move.Action, Move.X, Move.Y, AdditionalInfo,
                                 Priority);
        }
    }
}