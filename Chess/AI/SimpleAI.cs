using MonoChess.Chess.Core;
using System;
using System.Collections.Generic;

namespace MonoChess.Chess.AI
{
    public static class SimpleAI
    {
        private static readonly Random random = new();

        /// <summary>
        /// Gets a random weighted legal move for the current position
        /// </summary>
        /// <param name="board">The current board state</param>
        /// <returns>A legal move, or -1 if no legal moves available</returns>
        public static int GetWeightedMove(Board board)
        {
            var legalMoves = GetLegalMoves(board);
            if (legalMoves.Count == 0)
                return -1;

            // Weight each move
            var weightedMoves = new List<(int move, int weight)>();

            foreach (int move in legalMoves)
            {
                int weight = 1; // baseline

                // Example heuristics:
                if (board.IsCapture(move)) weight += 3;
                if (board.GivesCheck(move) && !board.IsCapture(move))
                    weight += 1; // prefer capture-checks, but not spam pointless checks

                int piece = board.GetMovingPiece(move);
                int targetSquare = board.GetTargetSquare(move);

                // Knights and bishops developing early
                if (piece == Piece.KNIGHT || piece == Piece.BISHOP)
                {
                    int rank = targetSquare / 8;
                    int file = targetSquare % 8;
                    if (file >= 2 && file <= 5 && rank >= 2 && rank <= 5)
                        weight += 2; // towards center
                }

                // Pawn moves towards center
                if (piece == Piece.PAWN)
                {
                    int rank = targetSquare / 8;
                    int file = targetSquare % 8;

                    // Encourage opening pawns in rank 2/7
                    if ((board.SideToMove == Piece.WHITE && rank == 3) ||
                        (board.SideToMove == Piece.BLACK && rank == 4))
                        weight += 1;

                    // Extra for central files
                    if (file == 3 || file == 4)
                        weight += 1;
                }

                weightedMoves.Add((move, weight));
            }

            // Pick move by weighted random
            int totalWeight = 0;
            foreach (var wm in weightedMoves)
                totalWeight += wm.weight;

            int roll = random.Next(totalWeight);
            foreach (var wm in weightedMoves)
            {
                if (roll < wm.weight)
                    return wm.move;
                roll -= wm.weight;
            }

            // Fallback
            return legalMoves[random.Next(legalMoves.Count)];
        }

        /// <summary>
        /// Gets all legal moves for the current position
        /// </summary>
        /// <param name="board">The current board state</param>
        /// <returns>List of legal moves</returns>
        private static List<int> GetLegalMoves(Board board)
        {
            var legalMoves = new List<int>();
            var pseudoLegalMoves = MoveGenerator.Generate(board);

            foreach (int move in pseudoLegalMoves)
            {
                // Test if move is legal by making it and checking if king is in check
                board.MakeMove(move);

                int mover = board.SideToMove ^ 1; // Side that just moved
                int kingSquare = board.KingSquare(mover);
                bool isLegal = kingSquare >= 0 && !MoveGenerator.IsSquareAttacked(board, kingSquare, board.SideToMove);

                board.UnmakeMove();

                if (isLegal)
                {
                    legalMoves.Add(move);
                }
            }

            return legalMoves;
        }

        /// <summary>
        /// Checks if the current position is checkmate or stalemate
        /// </summary>
        /// <param name="board">The current board state</param>
        /// <returns>True if game is over (no legal moves)</returns>
        public static bool IsGameOver(Board board)
        {
            return GetLegalMoves(board).Count == 0;
        }

        /// <summary>
        /// Checks if the current position is checkmate
        /// </summary>
        /// <param name="board">The current board state</param>
        /// <returns>True if current side is in checkmate</returns>
        public static bool IsCheckmate(Board board)
        {
            if (!IsGameOver(board))
                return false;

            int kingSquare = board.KingSquare(board.SideToMove);
            return kingSquare >= 0 && MoveGenerator.IsSquareAttacked(board, kingSquare, 1 - board.SideToMove);
        }

        /// <summary>
        /// Checks if the current position is stalemate
        /// </summary>
        /// <param name="board">The current board state</param>
        /// <returns>True if current side is in stalemate</returns>
        public static bool IsStalemate(Board board)
        {
            if (!IsGameOver(board))
                return false;

            int kingSquare = board.KingSquare(board.SideToMove);
            return kingSquare >= 0 && !MoveGenerator.IsSquareAttacked(board, kingSquare, 1 - board.SideToMove);
        }
    }
}