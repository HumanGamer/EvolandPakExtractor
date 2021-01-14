using System;
using System.Collections.Generic;
using System.Text;

namespace EvolandPakExtractor
{
	public static class Extensions
	{
		public static bool Matches(this byte[] self, byte[] other)
		{
			if (self.Length != other.Length)
				return false;

			for (int i = 0; i < self.Length; i++)
			{
				if (self[i] != other[i])
					return false;
			}

			return true;
		}
	}
}
