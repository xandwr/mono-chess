using MonoChess.Chess.Util;
using System.Collections.Generic;
using System;

namespace MonoChess.Chess.Core
{
    public static class MoveGenerator
    {
        // Precomputed attack masks
        private static readonly ulong[] KnightAttacks = new ulong[64];
        private static readonly ulong[] KingAttacks = new ulong[64];

        // Directions for sliding pieces
        private static readonly int[] RookDirections = { 8, -8, 1, -1 };
        private static readonly int[] BishopDirections = { 9, -9, 7, -7 };
        private static readonly int[] QueenDirections = { 8, -8, 1, -1, 9, -9, 7, -7 };

        static MoveGenerator()
        {
            for (int sq = 0; sq < 64; sq++)
            {
                KnightAttacks[sq] = GenKnightAttacks(sq);
                KingAttacks[sq] = GenKingAttacks(sq);
            }
        }

        private static ulong GenKnightAttacks(int sq)
        {
            ulong attacks = 0UL;
            int rank = sq / 8, file = sq % 8;
            int[] dr = { 2, 2, 1, 1, -1, -1, -2, -2 };
            int[] df = { 1, -1, 2, -2, 2, -2, 1, -1 };

            for (int i = 0; i < 8; i++)
            {
                int r = rank + dr[i], f = file + df[i];
                if (r >= 0 && r < 8 && f >= 0 && f < 8)
                    attacks |= 1UL << (r * 8 + f);
            }
            return attacks;
        }

        private static ulong GenKingAttacks(int sq)
        {
            ulong attacks = 0UL;
            int rank = sq / 8, file = sq % 8;
            for (int dr = -1; dr <= 1; dr++)
                for (int df = -1; df <= 1; df++)
                {
                    if (dr == 0 && df == 0) continue;
                    int r = rank + dr, f = file + df;
                    if (r >= 0 && r < 8 && f >= 0 && f < 8)
                        attacks |= 1UL << (r * 8 + f);
                }
            return attacks;
        }

        // Generic sliding generator
        private static void GenerateSliding(Board board, int side, int piece, ulong sliders, int[] directions, List<int> moves)
        {
            ulong myPieces = side == Piece.WHITE ? board.WhitePieces : board.BlackPieces;
            ulong theirPieces = side == Piece.WHITE ? board.BlackPieces : board.WhitePieces;

            while (sliders != 0)
            {
                int from = BitOps.PopLsb(ref sliders);
                int fromFile = from % 8;

                foreach (int dir in directions)
                {
                    int to = from;

                    while (true)
                    {
                        int next = to + dir;
                        if (next < 0 || next >= 64) break;

                        int nextFile = next % 8;

                        // edge guards: stop if we wrapped horizontally
                        if ((dir == 1 || dir == -7 || dir == 9) && nextFile == 0) break;   // moved right over h-file
                        if ((dir == -1 || dir == 7 || dir == -9) && nextFile == 7) break;  // moved left over a-file

                        to = next;
                        ulong toMask = 1UL << to;

                        if ((myPieces & toMask) != 0) break; // blocked by own piece

                        if ((theirPieces & toMask) != 0)
                        {
                            int captured = board.GetPieceAt(to);
                            moves.Add(Move.Encode(from, to, piece, captured));
                            break; // capture stops sliding
                        }

                        moves.Add(Move.Encode(from, to, piece));
                    }
                }
            }
        }

        private static void GenerateCastling(Board b, int side, List<int> moves)
        {
            int opp = 1 - side;

            if (side == Piece.WHITE)
            {
                int e1 = 4, f1 = 5, g1 = 6, d1 = 3, c1 = 2, b1 = 1;

                bool wk = (b.CastlingRights & 0b0010) != 0;
                bool wq = (b.CastlingRights & 0b0001) != 0;

                if (wk)
                {
                    bool empty = ((b.AllPieces & ((1UL << f1) | (1UL << g1))) == 0);
                    bool safe = !IsSquareAttacked(b, e1, opp) && !IsSquareAttacked(b, f1, opp) && !IsSquareAttacked(b, g1, opp);
                    if (empty && safe) moves.Add(Move.Encode(e1, g1, Piece.KING));
                }
                if (wq)
                {
                    bool empty = ((b.AllPieces & ((1UL << d1) | (1UL << c1) | (1UL << b1))) == 0);
                    bool safe = !IsSquareAttacked(b, e1, opp) && !IsSquareAttacked(b, d1, opp) && !IsSquareAttacked(b, c1, opp);
                    if (empty && safe) moves.Add(Move.Encode(e1, c1, Piece.KING));
                }
            }
            else
            {
                int e8 = 60, f8 = 61, g8 = 62, d8 = 59, c8 = 58, b8 = 57;

                bool bk = (b.CastlingRights & 0b1000) != 0;
                bool bq = (b.CastlingRights & 0b0100) != 0;

                if (bk)
                {
                    bool empty = ((b.AllPieces & ((1UL << f8) | (1UL << g8))) == 0);
                    bool safe = !IsSquareAttacked(b, e8, opp) && !IsSquareAttacked(b, f8, opp) && !IsSquareAttacked(b, g8, opp);
                    if (empty && safe) moves.Add(Move.Encode(e8, g8, Piece.KING));
                }
                if (bq)
                {
                    bool empty = ((b.AllPieces & ((1UL << d8) | (1UL << c8) | (1UL << b8))) == 0);
                    bool safe = !IsSquareAttacked(b, e8, opp) && !IsSquareAttacked(b, d8, opp) && !IsSquareAttacked(b, c8, opp);
                    if (empty && safe) moves.Add(Move.Encode(e8, c8, Piece.KING));
                }
            }
        }

        // Generate pseudo-legal moves
        public static List<int> Generate(Board board)
        {
            var moves = new List<int>(64);

            int side = board.SideToMove;
            ulong myPieces = side == Piece.WHITE ? board.WhitePieces : board.BlackPieces;
            ulong theirPieces = side == Piece.WHITE ? board.BlackPieces : board.WhitePieces;
            ulong occ = board.AllPieces;

            // Knights
            ulong knights = board.GetBitboard(Piece.KNIGHT, side);
            while (knights != 0)
            {
                int from = BitOps.PopLsb(ref knights);
                ulong attacks = KnightAttacks[from] & ~myPieces;
                ulong tmp = attacks;
                while (tmp != 0)
                {
                    int to = BitOps.PopLsb(ref tmp);
                    int captured = board.IsOccupied(to, 1 - side) ? board.GetPieceAt(to) : Piece.EMPTY;
                    moves.Add(Move.Encode(from, to, Piece.KNIGHT, captured));
                }
            }

            // King
            ulong king = board.GetBitboard(Piece.KING, side);
            if (king != 0)
            {
                int from = BitOps.BitScanForward(king);
                ulong attacks = KingAttacks[from] & ~myPieces;
                ulong tmp = attacks;
                while (tmp != 0)
                {
                    int to = BitOps.PopLsb(ref tmp);
                    int captured = board.IsOccupied(to, 1 - side) ? board.GetPieceAt(to) : Piece.EMPTY;
                    moves.Add(Move.Encode(from, to, Piece.KING, captured));
                }
            }

            GenerateCastling(board, side, moves);

            // Pawns
            ulong pawns = board.GetBitboard(Piece.PAWN, side);
            int forward = side == Piece.WHITE ? 8 : -8;
            ulong empty = ~occ;

            while (pawns != 0)
            {
                int from = BitOps.PopLsb(ref pawns);
                int rank = from / 8;

                // Single push
                int to = from + forward;
                bool isPromoPush = (side == Piece.WHITE && to / 8 == 7) || (side == Piece.BLACK && to / 8 == 0);
                if (to >= 0 && to < 64)
                {
                    ulong toMask = 1UL << to;
                    if ((occ & (1UL << to)) == 0)
                    {
                        if (isPromoPush)
                        {
                            moves.Add(Move.Encode(from, to, Piece.PAWN, Piece.EMPTY, Piece.QUEEN));
                            moves.Add(Move.Encode(from, to, Piece.PAWN, Piece.EMPTY, Piece.ROOK));
                            moves.Add(Move.Encode(from, to, Piece.PAWN, Piece.EMPTY, Piece.BISHOP));
                            moves.Add(Move.Encode(from, to, Piece.PAWN, Piece.EMPTY, Piece.KNIGHT));
                        }
                        else
                        {
                            moves.Add(Move.Encode(from, to, Piece.PAWN));

                            // double push (unchanged)
                            if (((side == Piece.WHITE && rank == 1) || (side == Piece.BLACK && rank == 6)))
                            {
                                int to2 = from + 2 * forward;
                                if (to2 >= 0 && to2 < 64 && ((occ & (1UL << to2)) == 0))
                                    moves.Add(Move.Encode(from, to2, Piece.PAWN));
                            }
                        }
                    }
                }

                // Captures
                int[] pawnCaps = side == Piece.WHITE ? new[] { 7, 9 } : new[] { -7, -9 };
                foreach (int cap in pawnCaps)
                {
                    int capSq = from + cap;
                    if (capSq < 0 || capSq >= 64) continue;

                    // File check (avoid wrapping around board edges)
                    int fromFile = from % 8;
                    int capFile = capSq % 8;
                    if (Math.Abs(fromFile - capFile) != 1) continue;

                    ulong capMask = 1UL << capSq;
                    if ((theirPieces & capMask) != 0)
                    {
                        int captured = board.GetPieceAt(capSq);
                        bool isPromoCap = (side == Piece.WHITE && capSq / 8 == 7) || (side == Piece.BLACK && capSq / 8 == 0);
                        if (isPromoCap)
                        {
                            moves.Add(Move.Encode(from, capSq, Piece.PAWN, captured, Piece.QUEEN));
                            moves.Add(Move.Encode(from, capSq, Piece.PAWN, captured, Piece.ROOK));
                            moves.Add(Move.Encode(from, capSq, Piece.PAWN, captured, Piece.BISHOP));
                            moves.Add(Move.Encode(from, capSq, Piece.PAWN, captured, Piece.KNIGHT));
                        }
                        else
                        {
                            moves.Add(Move.Encode(from, capSq, Piece.PAWN, captured));
                        }
                    }
                }

                if (board.EnPassantSquare >= 0)
                {
                    int ep = board.EnPassantSquare;
                    if (side == Piece.WHITE)
                    {
                        if (from % 8 > 0 && from + 7 == ep) moves.Add(Move.Encode(from, ep, Piece.PAWN, Piece.PAWN));
                        if (from % 8 < 7 && from + 9 == ep) moves.Add(Move.Encode(from, ep, Piece.PAWN, Piece.PAWN));
                    }
                    else
                    {
                        if (from % 8 > 0 && from - 9 == ep) moves.Add(Move.Encode(from, ep, Piece.PAWN, Piece.PAWN));
                        if (from % 8 < 7 && from - 7 == ep) moves.Add(Move.Encode(from, ep, Piece.PAWN, Piece.PAWN));
                    }
                }
            }

            // Bishops
            GenerateSliding(board, side, Piece.BISHOP, board.GetBitboard(Piece.BISHOP, side), BishopDirections, moves);

            // Rooks
            GenerateSliding(board, side, Piece.ROOK, board.GetBitboard(Piece.ROOK, side), RookDirections, moves);

            // Queens
            GenerateSliding(board, side, Piece.QUEEN, board.GetBitboard(Piece.QUEEN, side), QueenDirections, moves);

            return moves;
        }

        public static bool IsSquareAttacked(Board b, int sq, int bySide)
        {
            int file = sq % 8;

            // Pawns
            if (bySide == Piece.WHITE)
            {
                // white pawn could be on sq-7 or sq-9
                if (file > 0 && sq >= 9 && ((b.WhitePawns & (1UL << (sq - 9))) != 0)) return true;
                if (file < 7 && sq >= 7 && ((b.WhitePawns & (1UL << (sq - 7))) != 0)) return true;
            }
            else
            {
                // black pawn could be on sq+7 or sq+9
                if (file > 0 && sq <= 54 && ((b.BlackPawns & (1UL << (sq + 7))) != 0)) return true;
                if (file < 7 && sq <= 55 && ((b.BlackPawns & (1UL << (sq + 9))) != 0)) return true;
            }

            // Knights
            ulong theirKnights = (bySide == Piece.WHITE) ? b.WhiteKnights : b.BlackKnights;
            if ((theirKnights & KnightAttacks[sq]) != 0) return true;

            // King
            ulong theirKing = (bySide == Piece.WHITE) ? b.WhiteKings : b.BlackKings;
            if ((theirKing & KingAttacks[sq]) != 0) return true;

            // Sliding: bishops/queens on diagonals
            if (RayHitsPiece(b, sq, bySide, new int[] { 9, -9, 7, -7 }, diag: true)) return true;

            // Sliding: rooks/queens on ranks/files
            if (RayHitsPiece(b, sq, bySide, new int[] { 8, -8, 1, -1 }, diag: false)) return true;

            return false;
        }

        private static bool RayHitsPiece(Board b, int sq, int bySide, int[] dirs, bool diag)
        {
            ulong bishops = (bySide == Piece.WHITE) ? b.WhiteBishops : b.BlackBishops;
            ulong rooks = (bySide == Piece.WHITE) ? b.WhiteRooks : b.BlackRooks;
            ulong queens = (bySide == Piece.WHITE) ? b.WhiteQueens : b.BlackQueens;

            foreach (int dir in dirs)
            {
                int to = sq;
                while (true)
                {
                    int next = to + dir;
                    if (next < 0 || next >= 64) break;

                    int toFile = to % 8;
                    int nextFile = next % 8;
                    // file wrap guard
                    if ((dir == 1 || dir == -7 || dir == 9) && nextFile == 0) break;
                    if ((dir == -1 || dir == 7 || dir == -9) && nextFile == 7) break;

                    to = next;
                    ulong mask = 1UL << to;

                    if ((b.AllPieces & mask) != 0)
                    {
                        if (diag)
                        {
                            if (((bishops | queens) & mask) != 0) return true;
                        }
                        else
                        {
                            if (((rooks | queens) & mask) != 0) return true;
                        }
                        break; // blocked
                    }
                }
            }
            return false;
        }
    }
}
