using Server.Interfaces;
using Server.Models;
using System.Data;
using Dapper;

namespace Server.Services;

public class EquipmentService(IDbConnection db) : IEquipmentService {
	private readonly IDbConnection Db = db;

	public Equipment[] GetEquipment() {
		string selectQuery = SelectEquipmentQuery();
		var equipment = Db.Query<Equipment>(selectQuery);
		return equipment.ToArray();
	}
	public Equipment[] GetEquipmentByCampsite(int campsiteID) {
		string selectQuery = SelectEquipmentByCampsiteQuery();
		var equipment = Db.Query<Equipment>(selectQuery, new { campsiteID });
		return equipment.ToArray();
	}
	
	#region Queries

	private static string SelectEquipmentQuery() {
		return "SELECT * FROM equipment";
	}
	private static string SelectEquipmentByCampsiteQuery() {
		return @"
			SELECT * FROM campsites_equipment AS ce
			INNER JOIN equipment AS e ON ce.equipmentID = e.id 
			WHERE ce.campsiteID = @campsiteID
		";
	}

	#endregion
}
