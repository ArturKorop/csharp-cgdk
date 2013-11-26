using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
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
                       ? self.ActionPoints >= _game.StandingMoveCost + self.InitialActionPoints / 2
                       : self.Stance == TrooperStance.Prone
                             ? self.ActionPoints >= _game.ProneMoveCost + self.InitialActionPoints / 2
                             : self.ActionPoints >= _game.KneelingMoveCost + self.InitialActionPoints / 2;
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

        public static Point ToPoint(this Unit self)
        {
            return new Point(self.X, self.Y);
        }

        public static List<Point> ToPointList(this IEnumerable<Unit> enumerable)
        {
            return enumerable.Select(x => x.ToPoint()).ToList();
        } 
    }
}