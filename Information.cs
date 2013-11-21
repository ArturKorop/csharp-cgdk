﻿using System;
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
        public List<Trooper> CanShoutedEnemiesImmediately { get; private set; }
        public List<Trooper> CanKilledEnemiesImmediately { get; private set; }
        public List<Trooper> CanKilledEnemiesAfterMoving { get; private set; }
        public List<Trooper> CanUseGrenadeEnemiesImmediately { get;private set; } 
        public List<Trooper> Teammates { get; private set; }
        public List<Trooper> WoundedTeammates { get; private set; }
        public List<Bonus> AvaliableBonuses { get; private set; }

        public AdditionalAction Action = AdditionalAction.None;
        public Point NextPoint { get; set; }

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
            CheckNeedingMoveToAnotherPoint();
            CheckPrepareStanceToMaxDamage();
            CheckCanUseGrenadeEnemiesImmediately();
            CheckFightingEnemies();
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
            if (VisibleEnemies.Count > 0) return;

            var troopers = _world.Troopers.Where(x => x.Id != _self.Id);
            AvaliableBonuses =
                _world.Bonuses.Where(x => ((x.Type == BonusType.FieldRation && !_self.IsHoldingFieldRation) ||
                                           (x.Type == BonusType.Medikit && !_self.IsHoldingMedikit) ||
                                           (x.Type == BonusType.Grenade && !_self.IsHoldingGrenade)) &&
                                          troopers.All(y => y.X != x.X && y.Y != x.Y)).ToList();
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

        private void CheckPrepareStanceToMaxDamage()
        {
            if (CanShoutedEnemiesImmediately.Count == 0 || _self.Stance == TrooperStance.Prone || !_self.CanChangeStance() || Action != AdditionalAction.None || _self.Type == TrooperType.FieldMedic)
                return;

            int currentDamage = GetCurrentDamage();
            TrooperStance lowerStance = _self.Stance == TrooperStance.Standing ? TrooperStance.Kneeling : TrooperStance.Prone;
            if (!_world.IsVisible(_self.ShootingRange, _self.X, _self.Y, lowerStance, CanShoutedEnemiesImmediately[0].X,
                                  CanShoutedEnemiesImmediately[0].Y, CanShoutedEnemiesImmediately[0].Stance)) return;
            
            var lowerStanceDamage = _self.GetDamage(lowerStance)*
                                    ((_self.ActionPoints - _game.StanceChangeCost)/_self.ShootCost);
            if (lowerStanceDamage >= currentDamage)
                Action = AdditionalAction.SetLowerStance;
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
            if(VisibleEnemies == null || Teammates == null) return;
            foreach (var teammate in Teammates)
            {
                foreach (var visibleEnemy in VisibleEnemies)
                {
                    if(_world.IsVisible(teammate.ShootingRange, teammate.X, teammate.Y, teammate.Stance, visibleEnemy.X, visibleEnemy.Y, visibleEnemy.Stance))
                        FightingEnemies.Add(visibleEnemy);
                    else if (_world.IsVisible(visibleEnemy.ShootingRange, visibleEnemy.X, visibleEnemy.Y, visibleEnemy.Stance, teammate.X, teammate.Y, teammate.Stance))
                        FightingEnemies.Add(visibleEnemy);
                }
            }
        }

        private void CheckNeedingMoveToAnotherPoint()
        {
            /*if (CanKilledEnemiesImmediately.Count > 0 || CanShoutedEnemiesImmediately.Count == 0 || _self.Type != TrooperType.Soldier) return;

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
            NextPoint = nextPoint;*/
        }

        private void CheckCanKilledEnemiesAfterMoving()
        {
            if(_self.ActionPoints < _self.ShootCost + _self.MoveCost()) return;

            var currentDamage = GetCurrentDamage();
            var allCanKilledEnemies = _world.Troopers.Where(x => !x.IsTeammate && x.Hitpoints <= currentDamage).ToList();
            if(!allCanKilledEnemies.Any()) return;

            var pathFinder = new PathFinder(_world.Cells);
            foreach (var enemy in allCanKilledEnemies)
            {
                var path = pathFinder.GetPath(new Point(enemy.X, enemy.Y), new Point(_self.X, _self.Y),
                                              _world.Troopers.Where(x => x.IsTeammate && x.Id != _self.Id)
                                                    .Select(x => new Point(x.X, x.Y))
                                                    .ToList());
                var currentPosition = path[0];
            }
            
        }

        private int GetCurrentDamage()
        {
            return _self.GetDamage(_self.Stance) * (_self.ActionPoints / _self.ShootCost);
        }
    }

    public enum AdditionalAction
    {
        None,
        SetLowerStance,
        UseGrenade,
        UseFieldRation,
        MoveTo,
        MoveToTargetGlobalPoint
    }
}