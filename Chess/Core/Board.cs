using System;
using System.Collections.Generic;
using Godot;
using MonoChess.Chess.Util;

namespace MonoChess.Chess.Core
{
    public class Board
    {
        // White bitboards
        public ulong WhitePawns = 0x000000000000FF00UL;
        public ulong WhiteKnights = 0x0000000000000042UL;
        public ulong WhiteBishops = 0x0000000000000024UL;
        public ulong WhiteRooks = 0x0000000000000081UL;
        public ulong WhiteQueens = 0x0000000000000008UL;
        public ulong WhiteKings = 0x0000000000000010UL;

        // Black bitboards
        public ulong BlackPawns = 0x00FF000000000000UL;
        public ulong BlackKnights = 0x4200000000000000UL;
        public ulong BlackBishops = 0x2400000000000000UL;
        public ulong BlackRooks = 0x8100000000000000UL;
        public ulong BlackQueens = 0x0800000000000000UL;
        public ulong BlackKings = 0x1000000000000000UL;

        public int SideToMove { get; private set; } = Piece.WHITE;

        public int CastlingRights { get; private set; } = 0b1111; // KQkq = 1111
        public int EnPassantSquare { get; private set; } = -1;

        // Occupancy helpers
        public ulong WhitePieces => WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKings;
        public ulong BlackPieces => BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKings;
        public ulong AllPieces => WhitePieces | BlackPieces;

        // Move history for undo
        public struct MoveState
        {
            public int Move;
            public int CapturedPiece;
            public int CastlingRights;
            public int EnPassantSquare;
        }

        private Stack<MoveState> history = new Stack<MoveState>();

        // Switch side
        public void SwitchSide() => SideToMove ^= 1;

        // Make move (returns false if illegal later on)
        public void MakeMove(int move)
        {
            int from = Move.From(move);
            int to = Move.To(move);
            int piece = Move.Piece(move);
            int captured = Move.Captured(move); // <-- use encoded captured, not board state

            ulong fromMask = 1UL << from;
            ulong toMask = 1UL << to;

            // save state BEFORE mutating
            history.Push(new MoveState
            {
                Move = move,
                CapturedPiece = captured,
                CastlingRights = CastlingRights,
                EnPassantSquare = EnPassantSquare
            });

            // EN PASSANT?
            bool isEp = piece == Piece.PAWN
                     && captured == Piece.PAWN
                     && to == EnPassantSquare
                     && (AllPieces & toMask) == 0;

            if (isEp)
            {
                int capSq = (SideToMove == Piece.WHITE) ? to - 8 : to + 8;
                ClearSquare(Piece.PAWN, 1 - SideToMove, 1UL << capSq);
            }
            else if (captured != Piece.EMPTY)
            {
                ClearSquare(captured, 1 - SideToMove, toMask);
            }

            // move piece
            ClearSquare(piece, SideToMove, fromMask);
            SetSquare(piece, SideToMove, toMask);

            // reset/set EP target
            EnPassantSquare = -1;
            if (piece == Piece.PAWN)
            {
                int fromRank = from / 8, toRank = to / 8;
                if (Math.Abs(toRank - fromRank) == 2)
                    EnPassantSquare = (from + to) / 2;
            }

            UpdateCastlingRights(piece, from, to);
            SideToMove ^= 1;
        }

        public void UnmakeMove()
        {
            var state = history.Pop();

            int move = state.Move;
            int from = Move.From(move);
            int to = Move.To(move);
            int piece = Move.Piece(move);
            int captured = state.CapturedPiece;

            // side that moved
            SideToMove ^= 1;

            ulong fromMask = 1UL << from;
            ulong toMask = 1UL << to;

            // remove from 'to' and put back on 'from'
            ClearSquare(piece, SideToMove, toMask);
            SetSquare(piece, SideToMove, fromMask);

            // restore captured
            if (captured != Piece.EMPTY)
            {
                bool wasEp = piece == Piece.PAWN
                          && captured == Piece.PAWN
                          && to == state.EnPassantSquare;
                if (wasEp)
                {
                    int capSq = (SideToMove == Piece.WHITE) ? to - 8 : to + 8;
                    SetSquare(Piece.PAWN, 1 - SideToMove, 1UL << capSq);
                }
                else
                {
                    SetSquare(captured, 1 - SideToMove, toMask);
                }
            }

            CastlingRights = state.CastlingRights;
            EnPassantSquare = state.EnPassantSquare;
        }

        private void UpdateCastlingRights(int piece, int from, int to)
        {
            // White king moved
            if (piece == Piece.KING && SideToMove == Piece.WHITE)
                CastlingRights &= 0b1100;
            // Black king moved
            if (piece == Piece.KING && SideToMove == Piece.BLACK)
                CastlingRights &= 0b0011;

            // White rooks
            if (piece == Piece.ROOK && SideToMove == Piece.WHITE)
            {
                if (from == 0) CastlingRights &= 0b1110;   // a1 rook moved
                if (from == 7) CastlingRights &= 0b1101;   // h1 rook moved
            }
            // Black rooks
            if (piece == Piece.ROOK && SideToMove == Piece.BLACK)
            {
                if (from == 56) CastlingRights &= 0b1011;  // a8 rook moved
                if (from == 63) CastlingRights &= 0b0111;  // h8 rook moved
            }

            // If capturing opponent’s rook on initial square → remove their right
            if (to == 0) CastlingRights &= 0b1110;
            if (to == 7) CastlingRights &= 0b1101;
            if (to == 56) CastlingRights &= 0b1011;
            if (to == 63) CastlingRights &= 0b0111;
        }

        // Utilities
        private void ClearSquare(int piece, int color, ulong mask)
        {
            ref ulong bb = ref GetRef(piece, color);
            bb &= ~mask;
        }

        private void SetSquare(int piece, int color, ulong mask)
        {
            ref ulong bb = ref GetRef(piece, color);
            bb |= mask;
        }

        private ref ulong GetRef(int piece, int color)
        {
            if (color == Piece.WHITE)
            {
                if (piece == Piece.PAWN) return ref WhitePawns;
                if (piece == Piece.KNIGHT) return ref WhiteKnights;
                if (piece == Piece.BISHOP) return ref WhiteBishops;
                if (piece == Piece.ROOK) return ref WhiteRooks;
                if (piece == Piece.QUEEN) return ref WhiteQueens;
                if (piece == Piece.KING) return ref WhiteKings;
                throw new ArgumentException("Invalid piece");
            }
            else
            {
                if (piece == Piece.PAWN) return ref BlackPawns;
                if (piece == Piece.KNIGHT) return ref BlackKnights;
                if (piece == Piece.BISHOP) return ref BlackBishops;
                if (piece == Piece.ROOK) return ref BlackRooks;
                if (piece == Piece.QUEEN) return ref BlackQueens;
                if (piece == Piece.KING) return ref BlackKings;
                throw new ArgumentException("Invalid piece");
            }
        }

        // Get piece code on a square
        public int GetPieceAt(int sq)
        {
            ulong mask = 1UL << sq;
            if ((WhitePawns & mask) != 0) return Piece.PAWN;
            if ((WhiteKnights & mask) != 0) return Piece.KNIGHT;
            if ((WhiteBishops & mask) != 0) return Piece.BISHOP;
            if ((WhiteRooks & mask) != 0) return Piece.ROOK;
            if ((WhiteQueens & mask) != 0) return Piece.QUEEN;
            if ((WhiteKings & mask) != 0) return Piece.KING;
            if ((BlackPawns & mask) != 0) return Piece.PAWN;
            if ((BlackKnights & mask) != 0) return Piece.KNIGHT;
            if ((BlackBishops & mask) != 0) return Piece.BISHOP;
            if ((BlackRooks & mask) != 0) return Piece.ROOK;
            if ((BlackQueens & mask) != 0) return Piece.QUEEN;
            if ((BlackKings & mask) != 0) return Piece.KING;
            return Piece.EMPTY;
        }

        public int KingSquare(int side)
        {
            ulong k = (side == Piece.WHITE) ? WhiteKings : BlackKings;
            return k != 0 ? BitOps.BitScanForward(k) : -1;
        }

        // Get the bitboard for a given piece + color
        public ulong GetBitboard(int piece, int color)
        {
            if (color == Piece.WHITE)
            {
                return piece switch
                {
                    Piece.PAWN => WhitePawns,
                    Piece.KNIGHT => WhiteKnights,
                    Piece.BISHOP => WhiteBishops,
                    Piece.ROOK => WhiteRooks,
                    Piece.QUEEN => WhiteQueens,
                    Piece.KING => WhiteKings,
                    _ => 0UL
                };
            }
            else
            {
                return piece switch
                {
                    Piece.PAWN => BlackPawns,
                    Piece.KNIGHT => BlackKnights,
                    Piece.BISHOP => BlackBishops,
                    Piece.ROOK => BlackRooks,
                    Piece.QUEEN => BlackQueens,
                    Piece.KING => BlackKings,
                    _ => 0UL
                };
            }
        }

        // Is a square occupied by this color?
        public bool IsOccupied(int square, int color)
        {
            ulong mask = 1UL << square;
            return (color == Piece.WHITE)
                ? (WhitePieces & mask) != 0
                : (BlackPieces & mask) != 0;
        }

        // Print board
        public void Print()
        {
            for (int rank = 7; rank >= 0; rank--)
            {
                string line = (rank + 1).ToString() + "  ";
                for (int file = 0; file < 8; file++)
                {
                    int sq = rank * 8 + file;
                    ulong mask = 1UL << sq;

                    string symbol = ".";
                    if ((WhitePawns & mask) != 0) symbol = "P";
                    else if ((WhiteKnights & mask) != 0) symbol = "N";
                    else if ((WhiteBishops & mask) != 0) symbol = "B";
                    else if ((WhiteRooks & mask) != 0) symbol = "R";
                    else if ((WhiteQueens & mask) != 0) symbol = "Q";
                    else if ((WhiteKings & mask) != 0) symbol = "K";
                    else if ((BlackPawns & mask) != 0) symbol = "p";
                    else if ((BlackKnights & mask) != 0) symbol = "n";
                    else if ((BlackBishops & mask) != 0) symbol = "b";
                    else if ((BlackRooks & mask) != 0) symbol = "r";
                    else if ((BlackQueens & mask) != 0) symbol = "q";
                    else if ((BlackKings & mask) != 0) symbol = "k";

                    line += symbol + " ";
                }
                GD.Print(line);
            }
            GD.Print("\n   a b c d e f g h\n");
        }
    }
}
