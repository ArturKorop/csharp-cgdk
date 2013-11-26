using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public static class TacticCommander
    {
        private static World _world;
        private static Trooper _self;
        private static Game _game;
        private static PathFinder _currentPathFinder;
        private static Trooper _headSquad;
        private static CurrentTactic _tactic;

        private static CurrentTactic Tactic
        {
            get { return _tactic; }
            set
            {
                if (value == _tactic) return;

                _tactic = value;
                ClearQueue();
                UpdateAfterChangeTactic();
            }
        }

        private static int _globalStep;
        private static Point _wayPoint = new Point(1, 1);

        private static List<Trooper> _squad;
        private static readonly List<Trooper> QueueOfSquad = new List<Trooper>();
        private static List<Trooper> _visibleEnemies;
        private static List<Trooper> _hiddenEnemies;
        private static Dictionary<Bonus, bool> _bonuses;

        private static readonly Queue<Move> CommanderActions = new Queue<Move>();
        private static readonly Queue<Move> SoldierActions = new Queue<Move>();
        private static readonly Queue<Move> MedicActions = new Queue<Move>();
        private static readonly Queue<Move> SniperActions = new Queue<Move>();

        public static void Update(World world, Trooper self, Game game)
        {
            _world = world;
            _self = self;
            if (_game == null) _game = game;
            if (_currentPathFinder == null) _currentPathFinder = new PathFinder(_world.Cells);

            if (!QueueOfSquad.Contains(_self)) QueueOfSquad.Add(self);
            else if (QueueOfSquad.First().Equals(self) && self.ActionPoints == self.InitialActionPoints) _globalStep++;

            CheckEnvironment();
            if (_visibleEnemies.Count > 0) Tactic = CurrentTactic.Fighting;
            else if (_bonuses.Count > 0) Tactic = CurrentTactic.GatheringBonuses;
            else Tactic = CurrentTactic.GoingToWayPoint;
        }

        public static void Run(Move move)
        {
            var currentAction = new Move {Action = ActionType.EndTurn};
            if (_self.Type == TrooperType.Commander && CommanderActions.Any())
                currentAction = CommanderActions.Dequeue();
            else if (_self.Type == TrooperType.Soldier && SoldierActions.Any())
                currentAction = SoldierActions.Dequeue();
            else if (_self.Type == TrooperType.FieldMedic && MedicActions.Any())
                currentAction = MedicActions.Dequeue();

            move.Action = currentAction.Action;
            move.X = currentAction.X;
            move.Y = currentAction.Y;
        }

        private static void UpdateAfterChangeTactic()
        {
            CheckHeadSquad();
            CalcNextCommanderStep();
            CalcNextSoldierStep();
            CalcNextMedicStep();
        }

        private static void CheckHeadSquad()
        {
            _headSquad = _squad.FirstOrDefault(x => x.Type == TrooperType.Commander) ??
                         _squad.FirstOrDefault(x => x.Type == TrooperType.Soldier) ??
                         _squad.First();
        }

        private static void ClearQueue()
        {
            CommanderActions.Clear();
            SoldierActions.Clear();
            MedicActions.Clear();
            SniperActions.Clear();
        }

        private static void CheckEnvironment()
        {
            _squad = _world.Troopers.Where(x => x.IsTeammate).ToList();
            _squad.Sort((x, y) => x.Id.CompareTo(y.Id));
            _visibleEnemies = _world.Troopers.Where(x => !x.IsTeammate).ToList();
            _bonuses = _world.Bonuses.ToDictionary(x => x, x => false);
        }

        private static void CalcNextCommanderStep()
        {
            var commander = _squad.FirstOrDefault(x => x.Type == TrooperType.Commander);
            if (commander == null) return;

            switch (Tactic)
            {
                case CurrentTactic.GatheringBonuses:
                    GatherBonuses(commander, CommanderActions);
                    break;
                case CurrentTactic.Fighting:
                    break;
                case CurrentTactic.GoingToWayPoint:
                    CheckPathToWayPoint();
                    break;
            }
        }

        private static void CheckPathToWayPoint()
        {
            var path = _currentPathFinder.GetPathToNeighbourCell(_wayPoint, _self.ToPoint(), GetTeammates());
            var maxStep = _self.ActionPoints/_self.MoveCost() - 2;
            if (maxStep < 1) return;

            for (int i = maxStep - 1; i >= 0; i--)
            {
                
            }
        }

        private static void CheckPathToHeadSquad()
        {
            
        }

        private static void CalcNextSoldierStep()
        {
            var soldier = _squad.FirstOrDefault(x => x.Type == TrooperType.Soldier);
            if (soldier == null) return;

            if (_globalStep == 0)
            {
                CheckBonuseInFirstStep(soldier, SoldierActions);
            }
        }

        private static void CalcNextMedicStep()
        {
            var medic = _squad.FirstOrDefault(x => x.Type == TrooperType.Soldier);
            if (medic == null) return;

            if (_globalStep == 0)
            {
                CheckBonuseInFirstStep(medic, MedicActions);
            }
        }

        private static void GatherBonuses(Trooper self, Queue<Move> queue)
        {
            if (_globalStep == 0)
            {
                CheckBonuseInFirstStep(self, queue);
            }
        }

        private static void CheckBonuseInFirstStep(Trooper self, Queue<Move> queue)
        {
            var info = new Information(_world, self, _game);
            if (info.AvaliableBonuses.Count == 0) return;

            var bonuses =
                info.AvaliableBonuses.Where(x => _bonuses.Contains(new KeyValuePair<Bonus, bool>(x, false))).Select(
                    x =>
                    new
                        {
                            Bonus = x,
                            Path =
                        _currentPathFinder.GetPathToPoint(x.ToPoint(), self.ToPoint(), info.Teammates.ToPointList())
                        })
                    .ToList();
            if (bonuses.Count == 0) return;

            bonuses.Sort((x, y) => x.Path.Count.CompareTo(y.Path.Count));
            var currentBonus = bonuses.First();
            if(currentBonus.Path.Count > self.ActionPoints/ self.MoveCost()) return;

            _bonuses.Remove(currentBonus.Bonus);
            _bonuses.Add(currentBonus.Bonus, true);
            foreach (var point in currentBonus.Path)
            {
                queue.Enqueue(new Move {Action = ActionType.Move, X = point.X, Y = point.Y});
            }
        }

        private static List<Point> GetTeammates()
        {
            return _world.Troopers.Where(x => x.IsTeammate && x.Id != _self.Id).ToPointList();
        } 
    }

    public enum CurrentTactic
    {
        GoingToWayPoint,
        Fighting,
        GatheringBonuses
    }
}