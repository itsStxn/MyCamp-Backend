using Server.Interfaces;
using Server.Models;
using System.Data;
using Dapper;

namespace Server.Services;

public class AttributeService(IDbConnection db) : IAttributeService {
	private readonly IDbConnection Db = db;

	public CampAttribute[] GetAttributes() {
		string selectQuery = SelectAttributesQuery();
		var attributes = Db.Query<CampAttribute>(selectQuery);
		return attributes.ToArray();
	}
	public CampAttribute[] GetAttributesByCampsite(int campsiteID) {
		string selectQuery = SelectAttributesByCampsiteQuery();
		var attributes = Db.Query<CampAttribute>(selectQuery, new { campsiteID });
		return attributes.ToArray();
	}
	
	#region Queries

	private static string SelectAttributesQuery() {
		return "SELECT *, CONCAT('') AS value FROM Attributes";
	}
	private static string SelectAttributesByCampsiteQuery() {
		return @"
			SELECT * FROM campsites_attributes AS ca
			INNER JOIN attributes AS a ON ca.attributeID = a.id 
			WHERE ca.campsiteID = @campsiteID
		";
	}

	#endregion
}
