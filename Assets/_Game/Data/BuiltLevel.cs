using GoldenMend.Core;

namespace GoldenMend.Data
{
    /// <summary>A level ready to play: the rules definition plus where things sit on screen.</summary>
    public sealed class BuiltLevel
    {
        public LevelSpec Spec { get; }
        public LevelDef Def { get; }

        public int Columns { get; }
        public int Rows { get; }
        public int[] SocketColumn { get; }
        public int[] SocketRow { get; }

        public PileGrid Pile => Def.Pile;

        public SolveResult Solution { get; }

        /// <summary>The deal attempt that was accepted; the catalog stores it as <see cref="LevelSpec.Deal"/>.</summary>
        public int Deal { get; }

        public BuiltLevel(LevelSpec spec, LevelDef def, int columns, int rows, int[] socketColumn, int[] socketRow,
            SolveResult solution, int deal = 0)
        {
            Spec = spec;
            Def = def;
            Columns = columns;
            Rows = rows;
            SocketColumn = socketColumn;
            SocketRow = socketRow;
            Solution = solution;
            Deal = deal;
        }
    }
}
