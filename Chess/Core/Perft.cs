using Godot;

namespace MonoChess.Chess.Core
{
    public static class Perft
    {
        public static long Run(Board b, int depth)
        {
            if (depth == 0) return 1;

            long nodes = 0;
            var moves = MoveGenerator.Generate(b); // pseudo-legal

            foreach (int m in moves)
            {
                b.MakeMove(m);

                int mover = b.SideToMove ^ 1; // side that just moved
                int ksq = b.KingSquare(mover);
                bool illegal = (ksq == -1) || MoveGenerator.IsSquareAttacked(b, ksq, b.SideToMove);
                if (!illegal)
                {
                    nodes += Run(b, depth - 1);
                }

                b.UnmakeMove();
            }
            return nodes;
        }

        public static void Divide(Board board, int depth)
        {
            long total = 0;
            var moves = MoveGenerator.Generate(board);

            foreach (var move in moves)
            {
                board.MakeMove(move);
                long count = Run(board, depth - 1);
                board.UnmakeMove();
                total += count;
                GD.Print($"{MoveToString(move)}: {count}");
            }

            GD.Print($"Total: {total}");
        }

        // Very naive SAN-ish string
        private static string MoveToString(int move)
        {
            int from = Move.From(move), to = Move.To(move);
            string sqStr(int s) => $"{(char)('a' + (s % 8))}{1 + (s / 8)}";
            return $"{sqStr(from)}{sqStr(to)}";
        }
    }
}
