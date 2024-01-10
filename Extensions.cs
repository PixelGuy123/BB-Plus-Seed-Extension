using System;

namespace BBSeedsExtended.Patches
{
	public static class Extensions
	{
		public static int RoundLongVal(this long lres, int minoffset = 0, int maxoffset = 0) =>
			(int)(lres > 0 ? lres % int.MaxValue + maxoffset : lres % int.MinValue + minoffset);

		public static long NextInt64(this Random rnd)
		{
			var buffer = new byte[sizeof(long)];
			rnd.NextBytes(buffer);
			return BitConverter.ToInt64(buffer, 0);
		}
	}
}
