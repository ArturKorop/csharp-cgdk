using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class Information
    {
        private readonly World _world;
        private readonly Trooper _self;
        private readonly Game _game;

        public List<Trooper> VisibleEnemies { get; private set; }
        public List<Trooper> FightingEnemies { get; private set; }
        public List<Trooper> DangerEnemies { get; private set; }
        public List<Trooper> CanShoutedEnemiesImmediately { get; private set; }
        public List<Trooper> CanShoutedHiddenEnemiesImmediately { get; private set; }
        public List<Trooper> CanKilledEnemiesImmediately { get; private set; }
        public List<Trooper> CanKilledEnemiesAfterMoving { get; private set; }
        public List<Trooper> CanUseGrenadeEnemiesImmediately { get; private set; }
        public List<Trooper> Teammates { get; private set; }
        public List<Trooper> WoundedTeammates { get; private set; }
        public List<Bonus> AvaliableBonuses { get; private set; }

        public Information(World world, Trooper self, Game game)
        {
            _world = world;
            _self = self;
            _game = game;

            CheckVisibleEnemies();
            CheckCanShoutEnemiesImmediately();
            CheckTeammates();
            CheckWoundedTeammates();
            CheckAvaliableBonuses();
            CheckCanKilledEnemiesImmediately();
            CheckCanUseGrenadeEnemiesImmediately();
            CheckFightingEnemies();
            CheckDangerEnemies();
            CheckShouteHiddenEnemies();
        }

        private void CheckShouteHiddenEnemies()
        {
            CanShoutedHiddenEnemiesImmediately = new List<Trooper>();

            if(BattleManagerV2.GetHiddenEnemies().Count == 0) return;

            CanShoutedHiddenEnemiesImmediately =
                BattleManagerV2.GetHiddenEnemies().Where(
                    x =>
                    _world.IsVisible(_self.ShootingRange, _self.X, _self.Y, _self.Stance, x.X, x.Y, x.Stance)).ToList();
        }

        private void CheckVisibleEnemies()
        {
            VisibleEnemies = _world.Troopers.Where(x => !x.IsTeammate && x.Hitpoints != 0).ToList();
        }

        private void CheckCanShoutEnemiesImmediately()
        {
            CanShoutedEnemiesImmediately =
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
            AvaliableBonuses = new List<Bonus>();
            var pathFinder = new PathFinder(_world.Cells);
            var troopers = _world.Troopers.Where(x => x.Id != _self.Id).Select(x => x.ToPoint()).ToList();
            AvaliableBonuses =
                _world.Bonuses.Where(x => ((x.Type == BonusType.FieldRation && !_self.IsHoldingFieldRation) ||
                                           (x.Type == BonusType.Medikit && !_self.IsHoldingMedikit) ||
                                           (x.Type == BonusType.Grenade && !_self.IsHoldingGrenade)) &&
                                          !troopers.Contains(x.ToPoint()) &&
                                          pathFinder.GetPathToPoint(x.ToPoint(), _self.ToPoint(),
                                                                    Teammates.Select(y => new Point(y.X, y.Y)).ToList())
                                          != null && pathFinder.GetPathToPoint(x.ToPoint(), _self.ToPoint(),
                                                                               Teammates.Select(y => new Point(y.X, y.Y))
                                                                                        .ToList()).Count <= 3/*_self.ActionPoints/_self.MoveCost()*/).ToList();
        }

        private void CheckCanKilledEnemiesImmediately()
        {
            var defaultDmg = GetCurrentDamage();
            var tempCanShoutEnemies = _world.Troopers.Where(
                x =>
                !x.IsTeammate &&
                _world.IsVisible(_self.ShootingRange, _self.X, _self.Y, TrooperStance.Kneeling, x.X, x.Y, x.Stance))
                                            .ToList();
            CanKilledEnemiesImmediately = tempCanShoutEnemies.Where(x => (x.Hitpoints <= defaultDmg
                                                                         )).ToList();
        }


        private void CheckCanUseGrenadeEnemiesImmediately()
        {
            CanUseGrenadeEnemiesImmediately = new List<Trooper>();
            if (_self.CanUseGrenadeImmediately())
            {
                CanUseGrenadeEnemiesImmediately =
                    _world.Troopers.Where(
                        x =>
                        !x.IsTeammate &&
                        _world.IsVisible(_game.GrenadeThrowRange, _self.X, _self.Y, TrooperStance.Standing, x.X, x.Y,
                                         TrooperStance.Standing)).ToList();
            }
        }

        private void CheckFightingEnemies()
        {
            FightingEnemies = new List<Trooper>();
            if (VisibleEnemies == null || Teammates == null) return;

            foreach (var teammate in Teammates)
            {
                foreach (var visibleEnemy in VisibleEnemies)
                {
                    if (_world.IsVisible(teammate.ShootingRange, teammate.X, teammate.Y, teammate.Stance, visibleEnemy.X,
                                         visibleEnemy.Y, visibleEnemy.Stance))
                        FightingEnemies.Add(visibleEnemy);
                    else if (_world.IsVisible(visibleEnemy.ShootingRange, visibleEnemy.X, visibleEnemy.Y,
                                              visibleEnemy.Stance, teammate.X, teammate.Y, teammate.Stance))
                        FightingEnemies.Add(visibleEnemy);
                }
            }
        }

        private void CheckDangerEnemies()
        {
            DangerEnemies = new List<Trooper>();
            if (VisibleEnemies == null) return;

            foreach (var visibleEnemy in VisibleEnemies)
            {
                if (_world.IsVisible(visibleEnemy.ShootingRange, visibleEnemy.X, visibleEnemy.Y,
                                     visibleEnemy.Stance, _self.X, _self.Y, _self.Stance))
                    DangerEnemies.Add(visibleEnemy);
            }
        }

        /* private void CheckNeedingMoveToAnotherPoint()
        {
            if (CanKilledEnemiesImmediately.Count > 0 || CanShoutedEnemiesImmediately.Count == 0 || _self.Type != TrooperType.Soldier) return;

            var otherDangerTeammate =
                Teammates.FirstOrDefault(x => x.Type == TrooperType.Commander);
            if (otherDangerTeammate == null) return;

            var path = Math.Abs(_self.X - otherDangerTeammate.X) + Math.Abs(_self.Y - otherDangerTeammate.Y);
            if(path > 2) return;

            var target = CanShoutedEnemiesImmediately.First();
            var pathFinder = new PathFinder(_world.Cells);
            var nextPoint = pathFinder.GetNextPoint(_self.X, _self.Y, target.X, target.Y,
                                                    Teammates.Select(x => new Point(x.X, x.Y)).ToList());
            Action = AdditionalAction.MoveToTargetGlobalPoint;
            NextPoint = nextPoint;
        }*/

        /*private void CheckCanKilledEnemiesAfterMoving()
        {
            if(_self.ActionPoints < _self.ShootCost + _self.MoveCost()) return;

            var currentDamage = GetCurrentDamage();
            var allCanKilledEnemies = _world.Troopers.Where(x => !x.IsTeammate && x.Hitpoints <= currentDamage).ToList();
            if(!allCanKilledEnemies.Any()) return;

            var pathFinder = new PathFinder(_world.Cells);
            foreach (var enemy in allCanKilledEnemies)
            {
                var path = pathFinder.GetPathToNeighbourCell(new Point(enemy.X, enemy.Y), new Point(_self.X, _self.Y),
                                              _world.Troopers.Where(x => x.IsTeammate && x.Id != _self.Id)
                                                    .Select(x => new Point(x.X, x.Y))
                                                    .ToList());
                if (path == null || path.Count == 0) return;
                var currentPoint = path[0];
                if (_world.IsVisible(_self.ShootingRange, currentPoint.X, currentPoint.Y, _self.Stance, enemy.X, enemy.Y,
                                     enemy.Stance))
                {
                    currentDamage = _self.GetDamage(_self.Stance)*
                                    ((_self.ActionPoints - _self.MoveCost())/_self.ShootCost);
                    if (enemy.Hitpoints <= currentDamage)
                    {
                        Action = AdditionalAction.MoveTo;
                        NextPoint = currentPoint;
                        return;
                    }
                }

                if (path.Count == 1) return;
                currentPoint = path[1];
                if (_world.IsVisible(_self.ShootingRange, currentPoint.X, currentPoint.Y, _self.Stance, enemy.X, enemy.Y,
                                     enemy.Stance))
                {
                    currentDamage = _self.GetDamage(_self.Stance) *
                                    ((_self.ActionPoints - _self.MoveCost()) / _self.ShootCost);
                    if (enemy.Hitpoints <= currentDamage)
                    {
                        Action = AdditionalAction.MoveTo;
                        NextPoint = currentPoint;
                    }
                }
            }
            
        }*/

        private int GetCurrentDamage()
        {
            return _self.GetDamage(_self.Stance)*(_self.ActionPoints/_self.ShootCost);
        }
    }

    public enum AdditionalAction
    {
        None,
        SetLowerStance,
        UseGrenade,
        UseFieldRation,
        MoveTo,
        MoveToTargetGlobalPoint,
        Request
    }
}