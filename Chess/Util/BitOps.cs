namespace MonoChess.Chess.Util
{
    // Small bit hacks helper
    public static class BitOps
    {
        public static int BitScanForward(ulong bb) => System.Numerics.BitOperations.TrailingZeroCount(bb);

        public static int PopLsb(ref ulong bb)
        {
            int sq = BitScanForward(bb);
            bb &= bb - 1;
            return sq;
        }
    }
}