using Godot;
using MonoChess.Chess.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoChess.Chess.AI
{
    public static class SimpleAI
    {
        private static Random random = new Random();

        /// <summary>
        /// Gets a random legal move for the current position
        /// </summary>
        /// <param name="board">The current board state</param>
        /// <returns>A legal move, or -1 if no legal moves available</returns>
        public static int GetRandomMove(Board board)
        {
            var legalMoves = GetLegalMoves(board);

            if (legalMoves.Count == 0)
                return -1; // No legal moves (checkmate or stalemate)

            int randomIndex = random.Next(legalMoves.Count);
            return legalMoves[randomIndex];
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