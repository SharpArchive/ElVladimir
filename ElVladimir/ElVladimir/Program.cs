using LeagueSharp.Common;


namespace ElVladimir
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Vladimir.Game_OnGameLoad;
        }
    }
}