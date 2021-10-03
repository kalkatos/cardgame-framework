namespace CardgameCore
{
	public static class UsefulExtensions
	{
		public static T[] SubArray<T> (this T[] array, int offset)
		{
			int length = array.Length - offset;
			T[] result = new T[length];
			System.Array.Copy(array, offset, result, 0, length);
			return result;
		}

		public static bool Contains<T> (this T[] array, T value)
		{
			return System.Array.IndexOf(array, value) >= 0;
		}
	}
}