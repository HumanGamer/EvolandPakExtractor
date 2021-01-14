using ICSharpCode.SharpZipLib.Checksums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EvolandPakExtractor
{
	public class PakFile
	{
		private List<PakEntry> _entries;

		private string _path;

		public PakFile(string path)
		{
			_entries = new List<PakEntry>();

			_path = path;
		}

		public static PakFile ReadPakFile(string path)
		{
			PakFile file = new PakFile(path);
			file.Read();

			return file;
		}

		private void Read()
		{
			using (Stream s = File.OpenRead(_path))
			using (BinaryReader br = new BinaryReader(s))
			{
				string magic1 = Encoding.ASCII.GetString(br.ReadBytes(3));
				if (magic1 != "PAK")
					throw new IOException("Invalid PAK Header!");
				byte version = br.ReadByte();

				uint headerLength = br.ReadUInt32();
				uint dataLength = br.ReadUInt32();

				// Root Entry
				ReadEntry(br, headerLength, 1, null);

				string magic2 = Encoding.ASCII.GetString(br.ReadBytes(4));
				if (magic2 != "DATA")
					throw new IOException("Invalid DATA Header");
			}
		}

		private bool ReadEntry(BinaryReader br, uint headerLength, int entryCount, string currentPath)
		{
			for (int i = 0; i < entryCount; i++)
			{
				PakEntry entry = new PakEntry();

				entry.Name = br.ReadString();
				if (currentPath != null)
					entry.Name = Path.Combine(currentPath, entry.Name);
				bool folder = br.ReadBoolean();

				if (folder)
				{
					int subEntryCount = br.ReadInt32();
					ReadEntry(br, headerLength, subEntryCount, entry.Name);
				} else
				{
					entry.Offset = headerLength + br.ReadUInt32();
					entry.Size = br.ReadUInt32();
					entry.Checksum = br.ReadUInt32();
					_entries.Add(entry);
				}
			}

			return true;
		}

		private void Extract(int index, string path)
		{
			var entry = _entries[index];
			Console.WriteLine("Extracting: " + entry.Name);
			string outPath = Path.Combine(path, entry.Name);
			string dirPath = Path.GetDirectoryName(outPath);
			if (!Directory.Exists(dirPath))
				Directory.CreateDirectory(dirPath);

			using (Stream s = File.OpenRead(_path))
			using (BinaryReader br = new BinaryReader(s))
			{
				s.Position = entry.Offset;
				byte[] data = br.ReadBytes((int)entry.Size);

				using (Stream s2 = File.Open(outPath, FileMode.Create))
				using (BinaryWriter bw = new BinaryWriter(s2))
				{
					bw.Write(data);
					bw.Flush();
				}


				var checksum = Utils.Adler32Checksum(outPath);
				if (checksum != entry.Checksum)
					Console.WriteLine($"Failed to verify checksum for: {entry.Name}. Got {checksum} but expected {entry.Checksum}");
			}
		}

		public void ExtractAll(string path)
		{
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			for (int i = 0; i < _entries.Count; i++)
				Extract(i, path);
		}

		public static void Pack(string sourceDir, string targetFile)
		{
			//List<string[]> filePaths = new List<string[]>();
			PakFilePackEntry rootEntry = new PakFilePackEntry("");

			string[] fileList = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
			//Array.Sort(fileList);

			for (int i = 0; i < fileList.Length; i++)
			{
				string file = fileList[i];
				file = file.Replace('\\', '/');
				file = file.Substring(file.IndexOf('/') + 1);

				Console.WriteLine("Found: " + file);
				//filePaths.Add(file.Split('/'));

				PakFilePackEntry currentEntry = rootEntry;

				if (file.Contains('/'))
				{
					string[] parts = file.Split('/');

					for (int j = 0; j < parts.Length; j++)
					//foreach (string part in parts)
					{
						string part = parts[j];
						if (!currentEntry.Entries.ContainsKey(part))
						{
							var entry = new PakFilePackEntry(part);
							currentEntry.Entries.Add(part, entry);
							currentEntry = entry;
						}
						else
						{
							currentEntry = currentEntry.Entries[part];
						}
					}

				} else
				{
					currentEntry.Entries.Add(file, new PakFilePackEntry(file));
				}
			}

			/*List<string> rootFiles = new List<string>();

			foreach (var path in filePaths)
			{
				string rootPath = path[0];
				if (!rootFiles.Contains(rootPath))
					rootFiles.Add(rootPath);
			}*/

			Console.WriteLine("Writing Pak File...");

			using (Stream s = File.Open(targetFile, FileMode.Create))
			using (BinaryWriter bw = new BinaryWriter(s))
			{
				bw.Write(Encoding.ASCII.GetBytes("PAK"));
				bw.Write((byte)0); // version

				long headerLengthOffset = bw.BaseStream.Position;
				bw.Write((int)0);
				long dataLengthOffset = bw.BaseStream.Position;
				bw.Write((int)0);

				using (MemoryStream ms = new MemoryStream())
				using (BinaryWriter dw = new BinaryWriter(ms))
				{
					WriteEntry(bw, dw, rootEntry, sourceDir);

					dw.Flush();

					bw.Write(Encoding.ASCII.GetBytes("DATA"));

					long dataOff = bw.BaseStream.Position;
					bw.BaseStream.Position = headerLengthOffset;
					bw.Write((int)dataOff);
					bw.BaseStream.Position = dataOff;

					bw.Write(ms.ToArray());

					long fileLen = bw.BaseStream.Position;
					bw.BaseStream.Position = dataLengthOffset;
					bw.Write((int)fileLen);
					bw.BaseStream.Position = fileLen;
				}
			}
		}

		private static void WriteEntry(BinaryWriter bw, BinaryWriter dw, PakFilePackEntry entry, string path)
		{
			bw.Write(entry.Name);

			bool folder = entry.Entries.Count > 0;
			bw.Write(folder);

			if (folder)
			{
				bw.Write((int)entry.Entries.Count);
				foreach (var pair in entry.Entries)
				{
					string folderPath = pair.Key;
					if (path != "")
						folderPath = Path.Combine(path, pair.Key);
					WriteEntry(bw, dw, pair.Value, folderPath);
				}
			} else
			{
				byte[] data = null;
				using (Stream s = File.OpenRead(path))
				using (BinaryReader br = new BinaryReader(s))
				{
					data = br.ReadBytes((int)br.BaseStream.Length);
				}


				long pos = dw.BaseStream.Position;
				dw.Write(data);
				int size = data.Length;
				uint checksum = data.Adler32Checksum();

				bw.Write((int)pos);
				bw.Write((int)size);
				bw.Write((uint)checksum);
			}
		}

		private class PakFilePackEntry
		{
			public string Name { get; set; }
			public Dictionary<string, PakFilePackEntry> Entries { get; set; }

			public PakFilePackEntry(string name)
			{
				Name = name;
				Entries = new Dictionary<string, PakFilePackEntry>();
			}
		}

		private class PakEntry
		{
			public string Name { get; set; }
			public uint Offset { get; set; }
			public uint Size { get; set; }

			/// <summary>
			/// Adler32 checksum
			/// </summary>
			public uint Checksum { get; set; }

			public override string ToString()
			{
				return Name;
			}
		}
	}
}
