namespace MonoChess.Chess.Core
{
    public static class Square
    {
        public static int FileOf(int square) => square & 7; // 0 = A, 7 = H
        public static int RankOf(int square) => square >> 3; // 0 = rank 1, 7 = rank 8
        public static int Index(int file, int rank) => (rank << 3) | file;

        public static string ToString(int square)
        {
            char fileChar = (char)('a' + FileOf(square));
            char rankChar = (char)('1' + RankOf(square));
            return $"{fileChar}{rankChar}";
        }
    }
}
