using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public sealed class MyStrategy : IStrategy
    {
        public void Move(Trooper self, World world, Game game, Move move)
        {
            Extensions.Init(game);
            BattleManager.Init(world.Troopers.First(x => x.IsTeammate));
            BattleManager.UpdatePoint(world.Troopers.Where(x => x.IsTeammate).ToArray(), world);
            IBehavior behavior = null;
            switch (self.Type)
            {
                case TrooperType.FieldMedic:
                    behavior = new MedicBehavior(world, self, game);
                    break;
                case TrooperType.Soldier:
                    behavior = new SoldierBehavior(world, self, game);
                    break;
                case TrooperType.Commander:
                    behavior = new CommanderBehavior(world, self, game);
                    break;
                case TrooperType.Sniper:
                    behavior = new SniperBehavior(world, self, game);
                    break;
                case TrooperType.Scout:
                    behavior = new DefaultBehaviorV2(world, self, game);
                    break;
            }

            if(behavior == null) return;
            behavior.Run(move);
            var text = String.Format("Step[{8,3}]: {7,20}  -  ID: {0,3}, Type: {1,12}, Action: {2,12}, [{3,2},{4,2}], AP: {5,2}, HP: {6,3}   AddInfo - {9}",
                                     self.Id,
                                     self.Type, move.Action, move.X, move.Y, self.ActionPoints, self.Hitpoints,
                                     behavior.StepInfo, BattleManager.Step, behavior.AddInfo);
            Console.WriteLine(text);
            BattleManager.Text += text + Environment.NewLine;
        }
    }

    public static class BattleManager
    {
        private static int _currentPoint = -1;
        private static readonly List<Point> Points;
        private static int _countOfNeededAciton;
        private static AdditionalAction _neededAction = AdditionalAction.None;

        public const int StepCarefullCount = 18;

        public static int Step;
        public static Point CurrentPoint;
        public static int[,] Map;
        public static AdditionalAction NeededAction
        {
            get
            {
                _countOfNeededAciton--;
                if (_countOfNeededAciton == 0) _neededAction = AdditionalAction.None;
                return _neededAction;
            }
            set
            {
                _neededAction = value;
                if(_neededAction == AdditionalAction.MoveToTargetGlobalPoint)
                    _countOfNeededAciton = 20;
            }
        }

        public static StreamWriter Stream;
        public static string Text;

        static BattleManager()
        {
            if (Points != null) return;
            Points = new List<Point>
                {
                    new Point(1, 1),
                    new Point(1, 18),
                    new Point(28, 18),
                    new Point(28, 1),
                };
        }

        public static void Log()
        {
            Stream = new StreamWriter("log"+ DateTime.Now.ToString("MMddHHmmss") +".txt", true);
            Stream.WriteLine(Text);
        }

        public static void Init(Trooper trooper)
        {
            if(_currentPoint != -1) return;
            if (trooper.X > 15 && trooper.Y > 10)
                _currentPoint = 0;
            else if (trooper.X > 15 && trooper.Y <= 10)
                _currentPoint = 1;
            else if (trooper.X <= 15 && trooper.Y <= 10)
                _currentPoint = 2;
            else
                _currentPoint = 3;

            CurrentPoint = Points[_currentPoint];
            CurrentPoint = new Point(15,10);
        }

        public static void UpdatePoint(Trooper[] troopers, World world)
        {
            foreach (var player in world.Players)
            {
                if(player.ApproximateX != -1)
                    CurrentPoint = new Point(player.ApproximateX, player.ApproximateY);

                _neededAction = _neededAction == AdditionalAction.Request ? AdditionalAction.None : _neededAction;
            }

            Step++;
            if (
                troopers.Any(
                    trooper =>
                    Math.Abs(trooper.X - CurrentPoint.X) + Math.Abs(trooper.Y - CurrentPoint.Y) > 5 &&
                    trooper.IsTeammate))
            {
                return;
            }

            if (troopers.SingleOrDefault(x => x.IsTeammate && x.Type == TrooperType.Commander) !=
                null)
            {
                NeededAction = AdditionalAction.Request;
            }
            else
            {
                switch (_currentPoint)
                {
                    case 0:
                        _currentPoint = 2;
                        break;
                    case 1:
                        _currentPoint = 3;
                        break;
                    case 2:
                        _currentPoint = 0;
                        break;
                    case 3:
                        _currentPoint = 1;
                        break;
                }

                CurrentPoint = Points[_currentPoint];
            }
        }
    }
}