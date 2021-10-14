﻿using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using LibellusLibrary.IO;

namespace LibellusLibrary.PMD
{

	public class PmdFile : FileBase
	{
		//Header
		[JsonIgnore] public byte FileType;
		[JsonIgnore] public byte FileFormat;
		[JsonIgnore] public short UserID;
		[JsonIgnore] public int FileSize => getFileSize();
		public char[] MagicCode;//PMD1/PMD2/PMD3
		[JsonIgnore] public int ExpandSize;
		[JsonIgnore] public int TypeTableCount => TypeTable.Count;//Expands to get{return TypeTable.Count}
		public int Version;
		[JsonIgnore] public int Reserve2;
		[JsonIgnore] public int Reserve3;

		public List<Types.TypeTable> TypeTable;

		public PmdFile() { }
		public PmdFile(string path) { Open(path); }
		public PmdFile(Stream stream, bool leaveOpen = false) { Open(stream, leaveOpen); }
		public PmdFile(BinaryReader reader) { Open(reader); }

		internal int getFileSize()
		{
			int fileSize = 0x20;//Header Size
			fileSize = fileSize + (0x10 * TypeTableCount);//TypeTableSize

			foreach (Types.TypeTable typeTableEntry in TypeTable)
			{
				fileSize += typeTableEntry.ItemSize * typeTableEntry.ItemCount;
			}
			return fileSize;
		}

		internal override void Read(BinaryReader reader)
		{
			FileType = reader.ReadByte();
			FileFormat = reader.ReadByte();
			UserID = reader.ReadInt16();
			//FileSize = reader.ReadInt32();
			reader.FSkip(4);//FileSize is set using a setter
			MagicCode = reader.ReadChars(4);
			//Console.WriteLine(MagicCode);
			ExpandSize = reader.ReadInt32();
			int typeTablecnt = reader.ReadInt32();
			Version = reader.ReadInt32();

			//Console.WriteLine("PMD File Ver.{0}", Version);

			Reserve2 = reader.ReadInt32();
			Reserve3 = reader.ReadInt32();
			TypeTable = new List<Types.TypeTable>();

			for (int i = 0; i < typeTablecnt; i++)
			{
				TypeTable.Add(new Types.TypeTable(reader));
			}

			return;

		}

		internal override void Write(BinaryWriter writer)
		{
			writer.Write(FileType);
			writer.Write(FileFormat);
			writer.Write(UserID);
			writer.Write(FileSize);
			writer.Write(MagicCode);
			writer.Write(ExpandSize);
			writer.Write(TypeTableCount);
			writer.Write(Version);
			writer.Write(Reserve2);
			writer.Write(Reserve3);

			//First we write while adjusting the data offsets
			long dataStart = writer.FTell() + (0x10 * TypeTableCount);
			long dataCurrent = dataStart;
			foreach (Types.TypeTable typeTableEntry in TypeTable)
			{
				typeTableEntry.ItemAddress = (int)dataCurrent;
				typeTableEntry.Write(writer);
				dataCurrent += typeTableEntry.ItemSize * typeTableEntry.ItemCount;
			}
			writer.FSeek(dataStart);
			//Now we write the data
			foreach (Types.TypeTable typeTableEntry in TypeTable)
			{
				foreach (Types.DataType dataTypeEntry in typeTableEntry.DataTable)
				{
					dataTypeEntry.Write(writer);
				}
			}
		}

		public string ToJson()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings
			{
				//TypeNameHandling = TypeNameHandling.Auto

			});
		}

		public static PmdFile FromJson(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<PmdFile>(json, new JsonSerializerSettings

			{
				TypeNameHandling = TypeNameHandling.Auto

			});
		}

	}
}