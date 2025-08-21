namespace MonoChess.Chess.Core
{
    public class Board
    {
        // Piece array for quick lookups
        public int[] Squares = new int[64];

        public int SideToMove { get; private set; } = Piece.WHITE;
        public int CastlingRights { get; private set; } = 0b1111; // KQkq
        public int EnPassantSquare { get; private set; } = -1;

        // TODO later: zobrist key, halfmove clock, etc.

        public void SetStartPosition()
        {
            // Pawns
            for (int f = 0; f < 8; f++)
            {
                Squares[Square.Index(f, 1)] = Piece.Make(Piece.PAWN, Piece.WHITE);
                Squares[Square.Index(f, 6)] = Piece.Make(Piece.PAWN, Piece.BLACK);
            }

            // Rooks
            Squares[Square.Index(0, 0)] = Piece.Make(Piece.ROOK, Piece.WHITE);
            Squares[Square.Index(7, 0)] = Piece.Make(Piece.ROOK, Piece.WHITE);
            Squares[Square.Index(0, 7)] = Piece.Make(Piece.ROOK, Piece.BLACK);
            Squares[Square.Index(7, 7)] = Piece.Make(Piece.ROOK, Piece.BLACK);

            // Knights
            Squares[Square.Index(1, 0)] = Piece.Make(Piece.KNIGHT, Piece.WHITE);
            Squares[Square.Index(6, 0)] = Piece.Make(Piece.KNIGHT, Piece.WHITE);
            Squares[Square.Index(1, 7)] = Piece.Make(Piece.KNIGHT, Piece.BLACK);
            Squares[Square.Index(6, 7)] = Piece.Make(Piece.KNIGHT, Piece.BLACK);

            // Bishops
            Squares[Square.Index(2, 0)] = Piece.Make(Piece.BISHOP, Piece.WHITE);
            Squares[Square.Index(5, 0)] = Piece.Make(Piece.BISHOP, Piece.WHITE);
            Squares[Square.Index(2, 7)] = Piece.Make(Piece.BISHOP, Piece.BLACK);
            Squares[Square.Index(5, 7)] = Piece.Make(Piece.BISHOP, Piece.BLACK);

            // Queens
            Squares[Square.Index(3, 0)] = Piece.Make(Piece.QUEEN, Piece.WHITE);
            Squares[Square.Index(3, 7)] = Piece.Make(Piece.QUEEN, Piece.BLACK);

            // Kings
            Squares[Square.Index(4, 0)] = Piece.Make(Piece.KING, Piece.WHITE);
            Squares[Square.Index(4, 7)] = Piece.Make(Piece.KING, Piece.BLACK);
        }
    }
}
