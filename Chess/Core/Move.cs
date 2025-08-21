namespace MonoChess.Chess.Core
{
    public static class Move
    {
        // bit layout: 0..5 from, 6..11 to, 12..14 piece, 15..17 captured, 18..20 promo
        private const int FROM_SHIFT = 0;
        private const int TO_SHIFT = 6;
        private const int PIECE_SHIFT = 12;
        private const int CAPT_SHIFT = 15;
        private const int PROMO_SHIFT = 18;

        public static int Encode(int from, int to, int piece, int captured = Core.Piece.EMPTY, int promo = Core.Piece.EMPTY)
            => (from & 63) << FROM_SHIFT
              | (to & 63) << TO_SHIFT
              | (piece & 7) << PIECE_SHIFT
              | (captured & 7) << CAPT_SHIFT
              | (promo & 7) << PROMO_SHIFT;

        public static int From(int m) => (m >> FROM_SHIFT) & 63;
        public static int To(int m) => (m >> TO_SHIFT) & 63;
        public static int Piece(int m) => (m >> PIECE_SHIFT) & 7;
        public static int Captured(int m) => (m >> CAPT_SHIFT) & 7;
        public static int Promo(int m) => (m >> PROMO_SHIFT) & 7;

        public static bool IsPromotion(int m) => Promo(m) != Core.Piece.EMPTY;
    }
}