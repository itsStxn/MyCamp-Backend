using Server.Interfaces;
using Server.Models;
using Server.Utils;
using System.Data;
using Dapper;

namespace Server.Services;

public class FacilityService(
	IDbConnection db, 
	Lazy<ICampsiteService> campsiteService,
	ICommentService commentService
) : IFacilityService {
	private readonly IDbConnection Db = db;
	private readonly FormHelper Form = new();
	private readonly ICommentService CommentService = commentService;
	private readonly Lazy<ICampsiteService> CampsiteService = campsiteService;

	public Facility[] GetFacilities(string[]? activities = null, string[]? states = null, string? rating = null) {
		states = ToStateCodes(states);
		var parameters = BuildFiltersParams(activities, states, rating);
		string selectQuery = SelectFacilitiesQuery(activities, states, rating);
		var facilities = Db.Query<Facility>(selectQuery, parameters);
		return facilities.ToArray();
	}
	public Facility? GetFacility(int facilityID) {
		string selectQuery = SelectFacilityQuery();
		var facility = Db.QueryFirstOrDefault<Facility>(selectQuery, new { facilityID });
		return facility;
	}
	public Address[] GetAddresses(int facilityID) {
		string selectQuery = SelectFacilityAddressesQuery();
		var addresses = Db.Query<Address>(selectQuery, new { facilityID });
		return addresses.ToArray();
	} 
	public Activity[] GetActivities(int facilityID) {
		string selectQuery = SelectActivitiesQuery();
		var activities = Db.Query<Activity>(selectQuery, new { facilityID });
		return activities.ToArray();
	}
	public Media[] GetMedia(int facilityID) {
		string selectQuery = SelectFacilityMediaQuery();
		var medias = Db.Query<Media>(selectQuery, new { facilityID });
		return medias.ToArray();
	}
	public Campsite[] GetCampsites(int facilityID) {
		return CampsiteService.Value.GetCampsites(facilityID);
	}
	public Facility[] GetSavedFacilities(int userID) {
		string selectQuery = SelectSavedFacilitiesQuery();
		var facilities = Db.Query<Facility>(selectQuery, new { userID });
		return facilities.ToArray();
	}
	public bool SaveFacility(int facilityID, int userID) {
		var savedFacility = GetSavedFacility(userID, facilityID);
		if (savedFacility != null) return DeleteSavedFacility(userID, facilityID);
		return InsertSavedFacility(userID, facilityID);
	}
	public bool RateFacility(int score, int facilityID, int userID) {
		var facilityScore = GetUserRating(userID, facilityID);
		if (facilityScore != null) {
			if (facilityScore.Score == score) return DeleteUserRating(userID, facilityID);
			return UpdateUserRating(userID, facilityID, score);
		}
		return InsertUserRating(userID, facilityID, score);
	}
	public FacilityScore? GetUserRating(int userID, int facilityID) {
		string selectQuery = SelectUserRatingQuery();
		var facilityScore = Db.QueryFirstOrDefault<FacilityScore>(selectQuery, new { userID, facilityID });
		return facilityScore;
	}
	public FacilityRating GetFacilityRating(int facilityID) {
		string selectQuery = FacilityAvgRatingQuery();
		var facilityRating = Db.QueryFirstOrDefault<float?>(selectQuery, new { facilityID });
		return new FacilityRating {	Rating = facilityRating };
	}
	public Comment[] GetComments(int facilityID, int userID) {
		return CommentService.GetComments(facilityID, userID);
	}

	#region Aid Functions

	private bool InsertUserRating(int userID, int facilityID, int score) {
		string insertQuery = InsertUserRatingQuery();
		int res = Db.Execute(insertQuery, new { userID, facilityID, score });
		return res > 0;
	}
	private bool UpdateUserRating(int userID, int facilityID, int score) {
		string updateQuery = UpdateUserRatingQuery();
		int res = Db.Execute(updateQuery, new { userID, facilityID, score });
		return res > 0;
	}
	private bool DeleteUserRating(int userID, int facilityID) {
			string deleteQuery = DeleteUserRatingQuery();
			int res = Db.Execute(deleteQuery, new { userID, facilityID });
			return res > 0;
	}
	private bool InsertSavedFacility(int userID, int facilityID) {
		string insertQuery = InsertSavedFacilityQuery();
		int res = Db.Execute(insertQuery, new { userID, facilityID });
		return res > 0;
	}
	private bool DeleteSavedFacility(int userID, int facilityID) {
		string deleteQuery = DeleteSavedFacilityQuery();
		int res = Db.Execute(deleteQuery, new { userID, facilityID });
		return res > 0;
	}
	private Facility? GetSavedFacility(int userID, int facilityID) {
		string selectQuery = SelectSavedFacilityQuery();
		var savedFacility = Db.QueryFirstOrDefault<Facility?>(selectQuery, new { userID, facilityID });
		return savedFacility;
	}
	private static DynamicParameters BuildFiltersParams(string[]? activities, string[]? states, string? rating) {
		var parameters = new DynamicParameters();
		if (activities != null) parameters.Add("activities", activities);
		if (states != null) parameters.Add("states", states);
		if (rating != null) parameters.Add("rating", int.Parse(rating));
		return parameters;
	}
	private string[]? ToStateCodes(string[]? states) {
		if (states == null || states.Length == 0) return null;
		
		for(int i = 0; i < states.Length; i++) {
			string? stateCode = Form.GetStateCode(states[i]);
			if (stateCode == null) {
				states = ["nothing found"];
				break;
			}
			states[i] = stateCode;
		}
		return states;
	}

	#endregion

	#region Queries

	private static string SelectFacilitiesQuery(string[]? activities, string[]? states, string? rating) {
		return @$"
			SELECT * FROM facilities WHERE
			{FilterByActivityQueryPart(activities)} AND 
			{FilterByStateQueryPart(states)} AND
			{FilterByRatingQueryPart(rating)}
		";
	}
	private static string SelectFacilityQuery() {
		return "SELECT * FROM facilities WHERE id = @facilityID";
	}
	private static string SelectFacilityAddressesQuery() {
		return "SELECT * FROM addresses WHERE facilityID = @facilityID";
	}
	private static string SelectActivitiesQuery() {
		return @"
			SELECT a.* FROM facilities_activities as fa
			INNER JOIN facilities as f ON fa.facilityID = f.id 
			INNER JOIN activities as a ON fa.activityID = a.id 
			WHERE fa.facilityID = @facilityID
		";
	}
	private static string SelectFacilityMediaQuery() {
		return @"
			SELECT m.* FROM facilities_media fm
			INNER JOIN facilities as f ON fm.facilityID = f.id 
			INNER JOIN media as m ON fm.mediaID = m.id 
			WHERE fm.facilityID = @facilityID
		";
	}
	private static string SelectSavedFacilitiesQuery() {
		return @"
			SELECT * FROM facilities 
			WHERE id IN (
				SELECT facilityID FROM savedFacilities 
				WHERE userID = @userID
			)
		";
	}
	private static string SelectUserRatingQuery() {
		return "SELECT * FROM ratings WHERE userID = @userID AND facilityID = @facilityID";
	}
	private static string FacilityAvgRatingQuery() {
		return "SELECT AVG(score) FROM ratings WHERE facilityID = @facilityID";
	}
	private static string InsertUserRatingQuery() {
		return "INSERT INTO ratings (userID, facilityID, score) VALUES (@userID, @facilityID, @score)";
	}
	private static string UpdateUserRatingQuery() {
		return "UPDATE ratings SET score = @score WHERE userID = @userID AND facilityID = @facilityID";
	}
	private static string DeleteUserRatingQuery() {
			return "DELETE FROM ratings WHERE userID = @userID AND facilityID = @facilityID";
	}
	private static string InsertSavedFacilityQuery() {
		return "INSERT INTO savedFacilities (userID, facilityID) VALUES (@userID, @facilityID)";
	}
	private static string DeleteSavedFacilityQuery() {
		return "DELETE FROM savedFacilities WHERE userID = @userID AND facilityID = @facilityID";
	}
	private static string SelectSavedFacilityQuery() {
		return "SELECT * FROM savedFacilities WHERE userID = @userID AND facilityID = @facilityID";
	}
	private static string FilterByStateQueryPart(string[]? filters) {
		if (filters == null || filters.Length == 0) return "true";
		return @$"
		id IN (
			SELECT facilityID FROM addresses
			WHERE stateCode IN @states
		)";
	}
	private static string FilterByActivityQueryPart(string[]? filters) {
		if (filters == null || filters.Length == 0) return "true";
		return @$"
		id IN (
			SELECT facilityID FROM facilities_activities WHERE 
			activityID IN ( SELECT id FROM activities WHERE name IN @activities )
		)";
	}
	private static string FilterByRatingQueryPart(string? filter) {
		if (filter == null) return "true";
		return @$"
		id IN (
			SELECT facilityID FROM ratings
			GROUP BY facilityID 
			HAVING AVG(score) >= @rating
		)";
	}

	#endregion
}
