using Server.Interfaces;
using Server.Models;
using System.Data;
using Dapper;

namespace Server.Services;

public class EquipmentService(IDbConnection db) : IEquipmentService {
	private readonly IDbConnection Db = db;

/// <summary>
/// Retrieves all equipment in the database.
/// </summary>
/// <returns>An array of <see cref="Equipment"/> objects.</returns>
	public Equipment[] GetEquipment() {
		string selectQuery = SelectEquipmentQuery();
		var equipment = Db.Query<Equipment>(selectQuery);
		return equipment.ToArray();
	}
/// <summary>
/// Retrieves all equipment for a given campsite.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to get the equipment for.</param>
/// <returns>An array of <see cref="Equipment"/> objects.</returns>
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
