namespace MonoChess.Chess.Core
{
    public static class Move
    {
        public static int Encode(int from, int to, int piece, int captured = 0)
            => from | (to << 6) | (piece << 12) | (captured << 16);

        public static int From(int move) => move & 0x3F;
        public static int To(int move) => (move >> 6) & 0x3F;
        public static int Piece(int move) => (move >> 12) & 0xF;
        public static int Captured(int move) => (move >> 16) & 0xF;
    }
}