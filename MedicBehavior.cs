using System;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class MedicBehavior : DefaultBehavior
    {
        public MedicBehavior(World world, Trooper self, Game game)
            : base(world, self, game)
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
                    if (path.Count * Self.MoveCost() + Game.MedikitUseCost <= Self.ActionPoints)
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
                if (path.Count > Self.ActionPoints / Self.MoveCost())
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
            if (Info.Teammates.Count == 0 && Info.CanShoutedEnemiesImmediately.Count == 1 && Self.CanShout() && Info.VisibleEnemies.Count == 1)
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
}