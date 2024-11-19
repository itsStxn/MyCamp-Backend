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
	private readonly ICommentService CommentService = commentService;
	private readonly Lazy<ICampsiteService> CampsiteService = campsiteService;

/// <summary>
/// Retrieves facilities based on the given activities, states, and rating.
/// </summary>
/// <param name="activities">The activities to filter by.</param>
/// <param name="states">The states to filter by.</param>
/// <param name="rating">The rating to filter by.</param>
/// <returns>An array of facilities that match the given filters.</returns>
	public Facility[] GetFacilities(string[]? activities = null, string[]? states = null, string? rating = null) {
		states = ToStateCodes(states);
		var parameters = BuildFiltersParams(activities, states, rating);
		string selectQuery = SelectFacilitiesQuery(activities, states, rating);
		var facilities = Db.Query<Facility>(selectQuery, parameters);
		return facilities.ToArray();
	}
/// <summary>
/// Retrieves a facility by its unique identifier.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve.</param>
/// <returns>The facility with the specified ID, or null if not found.</returns>
	public Facility? GetFacility(int facilityID) {
		string selectQuery = SelectFacilityQuery();
		var facility = Db.QueryFirstOrDefault<Facility>(selectQuery, new { facilityID });
		return facility;
	}
/// <summary>
/// Retrieves all addresses associated with a specific facility.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve addresses for.</param>
/// <returns>An array of addresses related to the specified facility.</returns>
	public Address[] GetAddresses(int facilityID) {
		string selectQuery = SelectFacilityAddressesQuery();
		var addresses = Db.Query<Address>(selectQuery, new { facilityID });
		return addresses.ToArray();
	} 
/// <summary>
/// Retrieves all activities associated with a specific facility.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve activities for.</param>
/// <returns>An array of activities related to the specified facility.</returns>
	public Activity[] GetActivities(int facilityID) {
		string selectQuery = SelectActivitiesQuery();
		var activities = Db.Query<Activity>(selectQuery, new { facilityID });
		return activities.ToArray();
	}
/// <summary>
/// Retrieves all media associated with a specific facility.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve media for.</param>
/// <returns>An array of media related to the specified facility.</returns>
	public Media[] GetMedia(int facilityID) {
		string selectQuery = SelectFacilityMediaQuery();
		var medias = Db.Query<Media>(selectQuery, new { facilityID });
		return medias.ToArray();
	}
/// <summary>
/// Retrieves all campsites associated with a specific facility.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve campsites for.</param>
/// <returns>An array of campsites related to the specified facility.</returns>
	public Campsite[] GetCampsites(int facilityID) {
		return CampsiteService.Value.GetCampsites(facilityID);
	}
/// <summary>
/// Retrieves all facilities that have been saved by a specific user.
/// </summary>
/// <param name="userID">The ID of the user whose saved facilities are to be retrieved.</param>
/// <returns>An array of facilities saved by the specified user.</returns>
	public Facility[] GetSavedFacilities(int userID) {
		string selectQuery = SelectSavedFacilitiesQuery();
		var facilities = Db.Query<Facility>(selectQuery, new { userID });
		return facilities.ToArray();
	}
/// <summary>
/// Saves or un-saves a facility for a specific user.
/// </summary>
/// <param name="facilityID">The ID of the facility to save or un-save.</param>
/// <param name="userID">The ID of the user to save or un-save the facility for.</param>
/// <returns>True if the facility was saved or un-saved, false if the facility does not exist.</returns>
	public bool SaveFacility(int facilityID, int userID) {
		var savedFacility = GetSavedFacility(userID, facilityID);
		if (savedFacility != null) return DeleteSavedFacility(userID, facilityID);
		return InsertSavedFacility(userID, facilityID);
	}
/// <summary>
/// Rates a facility. If the user has already rated the facility, their existing rating will be updated.
/// If the user is attempting to rate the facility the same as their existing rating, their rating will be deleted.
/// </summary>
/// <param name="score">The score to rate the facility.</param>
/// <param name="facilityID">The ID of the facility to rate.</param>
/// <param name="userID">The ID of the user rating the facility.</param>
/// <returns>True if the facility was rated, false if the facility does not exist.</returns>
	public bool RateFacility(int score, int facilityID, int userID) {
		var facilityScore = GetUserRating(userID, facilityID);
		if (facilityScore != null) {
			if (facilityScore.Score == score) return DeleteUserRating(userID, facilityID);
			return UpdateUserRating(userID, facilityID, score);
		}
		return InsertUserRating(userID, facilityID, score);
	}
/// <summary>
/// Retrieves the rating given to a facility by a specific user.
/// </summary>
/// <param name="userID">The ID of the user whose rating is to be retrieved.</param>
/// <param name="facilityID">The ID of the facility whose rating is to be retrieved.</param>
/// <returns>The rating given by the user to the facility, or null if it does not exist.</returns>
	public FacilityScore? GetUserRating(int userID, int facilityID) {
		string selectQuery = SelectUserRatingQuery();
		var facilityScore = Db.QueryFirstOrDefault<FacilityScore>(selectQuery, new { userID, facilityID });
		return facilityScore;
	}
/// <summary>
/// Retrieves the average rating given to a facility.
/// </summary>
/// <param name="facilityID">The ID of the facility whose average rating is to be retrieved.</param>
/// <returns>The average rating given to the facility, or null if the facility does not exist.</returns>
	public FacilityRating GetFacilityRating(int facilityID) {
		string selectQuery = FacilityAvgRatingQuery();
		var facilityRating = Db.QueryFirstOrDefault<float?>(selectQuery, new { facilityID });
		return new FacilityRating {	Rating = facilityRating };
	}
/// <summary>
/// Retrieves comments for a specified facility along with like information for a given user.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve comments for.</param>
/// <param name="userID">The ID of the user to retrieve like information for.</param>
/// <returns>An array of <see cref="Comment"/> objects associated with the specified facility.</returns>
	public Comment[] GetComments(int facilityID, int userID) {
		Console.WriteLine($"facilityID: {facilityID}, userID: {userID}");
		return CommentService.GetComments(facilityID, userID);
	}

	#region Aid Functions

/// <summary>
/// Inserts a user rating for a specified facility.
/// </summary>
/// <param name="userID">The ID of the user providing the rating.</param>
/// <param name="facilityID">The ID of the facility being rated.</param>
/// <param name="score">The rating score given by the user.</param>
/// <returns>True if the rating was successfully inserted, false otherwise.</returns>
	private bool InsertUserRating(int userID, int facilityID, int score) {
		string insertQuery = InsertUserRatingQuery();
		int res = Db.Execute(insertQuery, new { userID, facilityID, score });
		return res > 0;
	}
/// <summary>
/// Updates a user's rating for a specified facility.
/// </summary>
/// <param name="userID">The ID of the user whose rating is to be updated.</param>
/// <param name="facilityID">The ID of the facility whose rating is to be updated.</param>
/// <param name="score">The new score to be assigned to the user's rating.</param>
/// <returns>True if the rating was successfully updated, false otherwise.</returns>
	private bool UpdateUserRating(int userID, int facilityID, int score) {
		string updateQuery = UpdateUserRatingQuery();
		int res = Db.Execute(updateQuery, new { userID, facilityID, score });
		return res > 0;
	}
/// <summary>
/// Deletes a user's rating for a specified facility from the database.
/// </summary>
/// <param name="userID">The ID of the user whose rating is to be deleted.</param>
/// <param name="facilityID">The ID of the facility for which the rating is to be deleted.</param>
/// <returns>True if the rating was successfully deleted, false otherwise.</returns>
	private bool DeleteUserRating(int userID, int facilityID) {
			string deleteQuery = DeleteUserRatingQuery();
			int res = Db.Execute(deleteQuery, new { userID, facilityID });
			return res > 0;
	}
/// <summary>
/// Inserts a saved facility for the given user and facility.
/// </summary>
/// <param name="userID">The ID of the user to save the facility for.</param>
/// <param name="facilityID">The ID of the facility to save.</param>
/// <returns>True if the saved facility was successfully inserted, false otherwise.</returns>
	private bool InsertSavedFacility(int userID, int facilityID) {
		string insertQuery = InsertSavedFacilityQuery();
		int res = Db.Execute(insertQuery, new { userID, facilityID });
		return res > 0;
	}
/// <summary>
/// Deletes a saved facility for the given user and facility.
/// </summary>
/// <param name="userID">The ID of the user to delete the saved facility for.</param>
/// <param name="facilityID">The ID of the facility to delete the saved facility for.</param>
/// <returns>True if the saved facility was successfully deleted, false otherwise.</returns>
	private bool DeleteSavedFacility(int userID, int facilityID) {
		string deleteQuery = DeleteSavedFacilityQuery();
		int res = Db.Execute(deleteQuery, new { userID, facilityID });
		return res > 0;
	}
/// <summary>
/// Retrieves a saved facility for the given user and facility.
/// </summary>
/// <param name="userID">The ID of the user to retrieve the saved facility for.</param>
/// <param name="facilityID">The ID of the facility to retrieve the saved facility for.</param>
/// <returns>The saved facility if found, otherwise null.</returns>
	private Facility? GetSavedFacility(int userID, int facilityID) {
		string selectQuery = SelectSavedFacilityQuery();
		var savedFacility = Db.QueryFirstOrDefault<Facility?>(selectQuery, new { userID, facilityID });
		return savedFacility;
	}
/// <summary>
/// Builds a DynamicParameters object based on the given activity and state filters and rating filter.
/// The DynamicParameters object is used to pass parameters to the database queries.
/// The parameters are added if the corresponding filter is not null or empty.
/// </summary>
/// <param name="activities">The activities to filter by.</param>
/// <param name="states">The states to filter by.</param>
/// <param name="rating">The rating to filter by.</param>
/// <returns>The DynamicParameters object to be used in database queries.</returns>
	private static DynamicParameters BuildFiltersParams(string[]? activities, string[]? states, string? rating) {
		var parameters = new DynamicParameters();
		if (activities != null) parameters.Add("activities", activities);
		if (states != null) parameters.Add("states", states);
		if (rating != null) parameters.Add("rating", int.Parse(rating));
		return parameters;
	}
/// <summary>
/// Converts an array of state names to an array of state codes.
/// If any of the state names are invalid, the function will return an array containing a single string "nothing found".
/// </summary>
/// <param name="states">The array of state names to convert.</param>
/// <returns>The array of state codes, or an array containing a single string "nothing found" if any of the state names are invalid.</returns>
	private static string[]? ToStateCodes(string[]? states) {
		if (states == null || states.Length == 0) return null;
		
		for(int i = 0; i < states.Length; i++) {
			string? stateCode = FormHelper.GetStateCode(states[i]);
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
