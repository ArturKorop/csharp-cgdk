using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public static class GlobalManager
    {
        private static World _world;
        private static Trooper _self;
        private static Game _game;

        public static List<Trooper> Teammates = new List<Trooper>();
        public static List<Trooper> WoundedTeammates = new List<Trooper>(); 
        public static List<Trooper> VisibleEnemies = new List<Trooper>();
        public static List<Trooper> HiddenEnemies = new List<Trooper>();
        public static List<Trooper> FightingEnemies = new List<Trooper>();
        public static List<Trooper> CanKilledEnemies = new List<Trooper>();
        public static List<Point> Fortifies = new List<Point>(); 
 
        public static void Update(World world, Trooper self, Game game)
        {
            _world = world;
            _self = self;
            _game = game;

            CheckingRun();
        }

        private static void CheckingRun()
        {
            CheckVisibleEnemies();
            CheckWoundedTeammates();
        }

        private static void CheckVisibleEnemies()
        {
            VisibleEnemies = _world.Troopers.Where(x => !x.IsTeammate).ToList();
        }

        private static void CheckWoundedTeammates()
        {
            WoundedTeammates = _world.Troopers.Where(x => x.IsTeammate && x.Hitpoints < x.MaximalHitpoints).ToList();
        }


    }
}