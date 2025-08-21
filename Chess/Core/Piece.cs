namespace MonoChess.Chess.Core
{
    public static class Piece
    {
        public const int EMPTY = 0;

        // Types (lower 3 bits)
        public const int PAWN = 1;
        public const int KNIGHT = 2;
        public const int BISHOP = 3;
        public const int ROOK = 4;
        public const int QUEEN = 5;
        public const int KING = 6;

        // Color mask (bit 3)
        public const int WHITE = 0;   // 0000
        public const int BLACK = 8;   // 1000

        // Combine type + color → single encoded int
        public static int Make(int type, int color) => type | color;

        // Extract info
        public static bool IsEmpty(int piece) => piece == EMPTY;
        public static bool IsWhite(int piece) => piece != EMPTY && (piece & BLACK) == 0;
        public static bool IsBlack(int piece) => (piece & BLACK) != 0;
        public static int TypeOf(int piece) => piece & 7; // mask out color

        public static char ToChar(int piece)
        {
            if (piece == EMPTY) return '.';

            char c = TypeOf(piece) switch
            {
                PAWN => 'P',
                KNIGHT => 'N',
                BISHOP => 'B',
                ROOK => 'R',
                QUEEN => 'Q',
                KING => 'K',
                _ => '?'
            };

            return IsBlack(piece) ? char.ToLower(c) : c;
        }
    }
}
