using Godot;
using MonoChess.Chess.Core;

namespace MonoChess.Chess.Scripts
{
    public partial class Main : Node
    {
        private Board board;

        public override void _Ready()
        {
            board = new Board();
            board.SetStartPosition();

            // Just prints the board contents for now
            for (int rank = 7; rank >= 0; rank--)
            {
                string line = "";
                for (int file = 0; file < 8; file++)
                {
                    int sq = Square.Index(file, rank);
                    int piece = board.Squares[sq];

                    if (Piece.IsEmpty(piece)) line += ".";
                    else line += Piece.ToChar(piece);
                }
                GD.Print(line);
            }
        }
    }
}
