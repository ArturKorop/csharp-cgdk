using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public interface IBehavior
    {
        void Run(Move move);
        string Step { get; }
    }
}