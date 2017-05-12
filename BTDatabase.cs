﻿using System.IO;
using System.Data;
using TShockAPI;
using TShockAPI.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using Terraria;
using System.Linq;

namespace BindTools
{
	public class BTDatabase
	{
		private static IDbConnection db;

		public static void DBConnect()
		{
			switch (TShock.Config.StorageType.ToLower())
			{
				case "mysql":
					string[] dbHost = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection()
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
							dbHost[0],
							dbHost.Length == 1 ? "3306" : dbHost[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword)

					};
					break;

				case "sqlite":
					string sql = Path.Combine(TShock.SavePath, "BindTools.sqlite");
					db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;
			}

			SqlTableCreator sqlcreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			sqlcreator.EnsureTableStructure(new SqlTable("BindTools",
				new SqlColumn("UserID", MySqlDbType.Int32),
				new SqlColumn("ItemID", MySqlDbType.Int32),
				new SqlColumn("Commands", MySqlDbType.Text),
				new SqlColumn("Awaiting", MySqlDbType.Int32),
				new SqlColumn("Looping", MySqlDbType.Int32),
				new SqlColumn("Slot", MySqlDbType.Int32),
				new SqlColumn("Prefix", MySqlDbType.Int32)));
		}

		public static void BTAdd(int UserID, BindTool BTItem)
		{
			db.Query("INSERT INTO BindTools (UserID, ItemID, Commands, Awaiting, Looping, Slot, Prefix) " +
				"VALUES (@0, @1, @2, @3, @4, @5, @6);", UserID, BTItem.item, string.Join("~;*;~", BTItem.commands),
					(BTItem.awaiting ? 1 : 0), (BTItem.looping ? 1 : 0), BTItem.slot, BTItem.prefix);
		}

		public static void BTDelete(int UserID, int ItemID)
		{
			db.Query("DELETE FROM BindTools WHERE UserID=@0 AND ItemID=@1;", UserID, ItemID);
		}

		public static void BTDelete(int UserID, int ItemID, int SlotOrPrefix, bool Slot = true)
		{
			db.Query("DELETE FROM BindTools WHERE UserID=@0 AND ItemID=@1 AND " + (Slot ? "Slot" : "Prefix") + "=@2;", UserID, ItemID, SlotOrPrefix);
		}

		public static void BTDelete(int UserID, int ItemID, int Slot, int Prefix)
		{
			db.Query("DELETE FROM BindTools WHERE UserID=@0 AND ItemID=@1 AND Slot=@2 AND Prefix=@3;", UserID, ItemID, Slot, Prefix);
		}

		public static List<BindTool> BTGet(int UserID)
		{
			List<BindTool> BTools = new List<BindTool>();

			using (QueryResult reader = db.QueryReader("SELECT * FROM BindTools WHERE UserID=@0;", UserID))
			{
				while (reader.Read())
				{
					Item Item = TShock.Utils.GetItemById(reader.Get<int>("ItemID"));
					IEnumerable<string> commands = reader.Get<string>("Commands").Split('~', ';', '*', ';', '~');
					List<string> Commands = (from string c in commands where (c != "") select c).ToList();
					bool Awaiting = (reader.Get<int>("Awaiting") == 1);
					int Slot = reader.Get<int>("Slot");
					int Prefix = reader.Get<int>("Prefix");
					bool Looping = (reader.Get<int>("Looping") == 1);

					BTools.Add(new BindTool(Item.netID, Slot, Commands, Awaiting, Looping, Prefix, true));
				}
			}

			return BTools;
		}
	}
}