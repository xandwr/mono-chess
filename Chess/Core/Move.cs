namespace MonoChess.Chess.Core
{
    public static class Move
    {
        public const int FLAG_NONE = 0;
        public const int FLAG_PROMO = 1;
        public const int FLAG_EP = 2;
        public const int FLAG_CASTLE = 4;

        public static int Encode(int from, int to, int pieceType, int capturedType, int promoType, int flags)
        {
            return from
                 | (to << 6)
                 | (pieceType << 12)
                 | (capturedType << 15)
                 | (promoType << 18)
                 | (flags << 21);
        }

        public static int From(int move) => move & 0x3F;
        public static int To(int move) => (move >> 6) & 0x3F;
        public static int Piece(int move) => (move >> 12) & 7;
        public static int Captured(int move) => (move >> 15) & 7;
        public static int Promotion(int move) => (move >> 18) & 7;
        public static int Flags(int move) => (move >> 21) & 7;
    }
}
