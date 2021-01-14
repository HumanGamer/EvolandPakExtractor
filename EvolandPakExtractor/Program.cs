using System;
using System.IO;

namespace EvolandPakExtractor
{
	public class Program
	{
		public static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Usage: EvolandPakExtractor <pakfile>");
				Console.WriteLine("Usage: EvolandPakExtractor --pack <sourceDir> <pakfile>");
				return;
			}

			if (args.Length > 2)
			{
				string option = args[0];
				if (option == "-p" || option == "--pack")
				{
					string inputDir = args[1];
					string outputFile = args[2];

					PakFile.Pack(inputDir, outputFile);
				}
			} else
			{
				string inputFile = args[0];
				string outputPath = Path.ChangeExtension(inputFile, null) + "_output";

				PakFile pak = PakFile.ReadPakFile(inputFile);

				pak.ExtractAll(outputPath);
			}

			Console.WriteLine("Done!");
			Console.ReadKey();
		}
	}
}
