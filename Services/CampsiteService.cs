using Server.Interfaces;
using Server.Models;
using System.Data;
using Dapper;

namespace Server.Services;

public class CampsiteService(
	IDbConnection db, 
	Lazy<IFacilityService> facilityService, 
	Lazy<IReservationService> reservationService,
	IAttributeService attributeService,
	IEquipmentService equipmentService
) : ICampsiteService {
	private readonly IDbConnection Db = db;
	private readonly IAttributeService AttributeService = attributeService;
	private readonly IEquipmentService EquipmentService = equipmentService;
	private readonly Lazy<IFacilityService> FacilityService = facilityService;
	private readonly Lazy<IReservationService> ReservationService = reservationService;

/// <summary>
/// Gets all the campsites for a given facility
/// </summary>
/// <param name="facilityID">The id of the facility to get the campsites for</param>
/// <returns>The campsites for the given facility</returns>
	public Campsite[] GetCampsites(int facilityID) {
		string selectQuery = SelectCampsitesByFacilityQuery();
		var camps = Db.Query<Campsite>(selectQuery, new { facilityID }).ToArray();
		foreach (var campsite in camps) {
			campsite.Attributes = AttributeService.GetAttributesByCampsite(campsite.Id);
			campsite.Equipment = EquipmentService.GetEquipmentByCampsite(campsite.Id);
		}
		return camps;
	}
/// <summary>
/// Retrieves a campsite by its unique identifier.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to retrieve.</param>
/// <returns>The campsite with the specified ID, or null if not found.</returns>
	public Campsite? GetCampsite(int campsiteID) {
		string selectQuery = SelectCampsiteQuery();
		return Db.QueryFirstOrDefault<Campsite?>(selectQuery, new { campsiteID });
	}
/// <summary>
/// Adds a new campsite to the database. If the campsite already exists,
/// then this will throw an exception. This will also add the attributes and
/// equipment associated with the campsite.
/// </summary>
/// <param name="campsite">The campsite to add.</param>
/// <param name="attributes">The attributes of the campsite.</param>
/// <param name="equipment">The equipment of the campsite.</param>
/// <returns>True if the campsite was added, false otherwise.</returns>
/// <exception cref="KeyNotFoundException">If the facilityID of the campsite does not exist.</exception>
/// <exception cref="InvalidOperationException">If the campsite already exists.</exception>
	public bool AddCampsite(Campsite campsite, CampAttribute[] attributes, Equipment[] equipment) {
		Facility? facility = FacilityService.Value.GetFacility(campsite.FacilityID)
		?? throw new KeyNotFoundException("Facility not found");

		Campsite? foundCampsite = GetCampsite(facility.Id, campsite.Loop, campsite.Name);
		if (foundCampsite != null) {
			throw new InvalidOperationException("Campsite already exists");
		}
		return InsertCampAndAttrsAndEquip(campsite, attributes, equipment);
	}
/// <summary>
/// Deletes a campsite from the database, as well as its associated attributes and equipment.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to delete.</param>
/// <returns>True if the campsite was deleted, false otherwise.</returns>
/// <exception cref="KeyNotFoundException">If the campsite does not exist.</exception>
/// <exception cref="InvalidOperationException">If a reservation exists for the campsite.</exception>
	public bool DeleteCampsite(int campsiteID) {
		string deleteQuery = DeleteCampsiteQuery();
		return DeleteConstraintsAndRunQuery(deleteQuery, new { campsiteID }, hardDelete: true);
	}
/// <summary>
/// Gets the availability of a given campsite for the next two months.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to get the availability of.</param>
/// <returns>A dictionary mapping dates in the format "yyyy-MM-dd" to a boolean indicating whether the campsite is available on the given date.</returns>
/// <exception cref="KeyNotFoundException">If the campsite does not exist.</exception>
	public Dictionary<string, bool> GetAvailabilities(int campsiteID) {
		Campsite? campsite = GetCampsite(campsiteID)
		??throw new KeyNotFoundException("Campsite not found");
		
		Dictionary<string, bool> availabilities = [];
		for (DateTime date = DateTime.Today; date <= DateTime.Today.AddMonths(2); date = date.AddDays(1)) {
			availabilities.Add(date.ToString("yyyy-MM-dd"), CampIsAvailabile(campsite, date));
		}
		return availabilities;
	}
/// <summary>
/// Enables a campsite in the database. This sets the campsite's 'isEnabled' field to true.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to enable.</param>
/// <returns>True if the campsite was enabled, false if the campsite does not exist.</returns>
	public bool EnableCampsite(int campsiteID) {
		string updateQuery = EnableCampsiteQuery();
		return Db.Execute(updateQuery, new { campsiteID }) > 0;
	}
/// <summary>
/// Disables a campsite in the database. This sets the campsite's 'isEnabled' field to false.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to disable.</param>
/// <returns>True if the campsite was disabled, false if the campsite does not exist.</returns>
	public bool DisableCampsite(int campsiteID) {
		string updateQuery = DisableCampsiteQuery();
		return DeleteConstraintsAndRunQuery(updateQuery, new { campsiteID });
	}
/// <summary>
/// Updates the capacity of a specified campsite. If there are existing reservations,
/// it handles them to ensure they conform to the new capacity. If the capacity update
/// fails, it rolls back the transaction.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to update the capacity for.</param>
/// <param name="capacity">The new capacity to set for the campsite.</param>
/// <returns>True if the capacity was successfully updated, false otherwise.</returns>
/// <exception cref="KeyNotFoundException">Thrown when the campsite does not exist.</exception>
/// <exception cref="DataException">Thrown when there is an issue with database operations.</exception>
/// <exception cref="Exception">Thrown for any other general exceptions.</exception>
	public bool UpdateCapacity(int campsiteID, int capacity) {
		if (Db.State == ConnectionState.Closed) Db.Open();
		using var trans = Db.BeginTransaction();

		try {
			Reservation[] reservations = GetReservations(campsiteID);
			if (reservations.Length > 0) {
				HandleReservations(reservations, campsiteID, capacity, trans);
			}

			string updateQuery = UpdateCapacityQuery();
			int res = Db.Execute(updateQuery, new { campsiteID, capacity }, trans);
			if (res == 0) {
				trans.Rollback();
				return false;
			}
			trans.Commit();
			return true;
		}
		catch (KeyNotFoundException e) {
			trans.Rollback();
			throw new KeyNotFoundException(e.Message);
		}
		catch (DataException e) {
			trans.Rollback();
			throw new DataException(e.Message);
		}
		catch (Exception e) {
			trans.Rollback();
			throw new Exception(e.Message);
		}
		finally {
			if (Db.State == ConnectionState.Open) Db.Close();
		}
	}

	#region Aid Functions

	/// <summary>
	/// Handles reservations by cutting down extra reserved spots for a given campsite on certain dates.
	/// </summary>
	/// <param name="reservations">The reservations to handle.</param>
	/// <param name="campsiteID">The ID of the campsite.</param>
	/// <param name="capacity">The capacity of the campsite.</param>
	/// <param name="trans">The transaction to use for database operations.</param>
	private void HandleReservations(Reservation[] reservations, int campsiteID, int capacity, IDbTransaction trans) {
		HashSet<DateTime> visitedDates = [];
		foreach (Reservation re in reservations) {
			for(DateTime date = re.CheckIn; date <= re.CheckOut; date = date.AddDays(1)) {
				if (visitedDates.Add(date)) {
					CutExtraReservedSpots(campsiteID, date, capacity, trans);
				}
			}
		}
	}
/// <summary>
/// Gets all reservations for a given campsite.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to get reservations for.</param>
/// <returns>An array of reservations for the given campsite.</returns>
	private Reservation[] GetReservations(int campsiteID) {
		Reservation[] reservations = ReservationService.Value.GetReservationsByCampsite(campsiteID);
		return reservations;
	}
/// <summary>
/// Cuts down extra reservations for a given campsite on certain dates.
/// It will delete reservations starting from the ones with the lowest IDs.
/// </summary>
/// <param name="campsiteID">The ID of the campsite.</param>
/// <param name="date">The date to cut down reservations for.</param>
/// <param name="capacity">The capacity of the campsite.</param>
/// <param name="trans">The transaction to use for database operations.</param>
	private void CutExtraReservedSpots(int campsiteID, DateTime date, int capacity, IDbTransaction trans) {
		int difference = GetCampsiteTakenSpots(campsiteID, date) - capacity;
		if (difference > 0) {
			string selectQuery = CampAvailabilityQuery().Replace("COUNT(*)", "id");
			string deleteQuery = DeleteReservationsByCapacityQuery(selectQuery);
			int res = Db.Execute(deleteQuery, new { campsiteID, date, difference }, trans);
			if (res == 0) throw new DataException("Failed to delete extra reservations");
		}
	}
/// <summary>
/// Deletes constraints and runs the given query.
/// The function deletes all reservations and, if hardDelete is true, all attributes and equipment for the given campsite.
/// It then runs the given query and returns true if the result is not 0, false otherwise.
/// If an exception is thrown during the transaction, it rolls back the transaction and throws the exception.
/// </summary>
/// <param name="query">The query to run.</param>
/// <param name="parameters">The parameters to pass to the query.</param>
/// <param name="hardDelete">Whether to delete attributes and equipment as well as reservations.</param>
/// <returns>True if the query was executed successfully, false otherwise.</returns>
/// <exception cref="InvalidOperationException">Thrown when the parameters do not contain a campsiteID property.</exception>
/// <exception cref="KeyNotFoundException">Thrown when the campsite does not exist.</exception>
/// <exception cref="Exception">Thrown for any other general exceptions.</exception>
	private bool DeleteConstraintsAndRunQuery(string query, object parameters, bool hardDelete = false) {
		var campsiteIdProp = parameters.GetType().GetProperty("campsiteID");
		if (Db.State == ConnectionState.Closed) Db.Open();
		using var trans = Db.BeginTransaction();

		try {
			var campsiteID = campsiteIdProp?.GetValue(parameters)
			?? throw new InvalidOperationException("Invalid query parameters");
			
			DeleteReservations((int)campsiteID, trans);

			if (hardDelete) {
				DeleteAttributes((int)campsiteID, trans);
				DeleteEquipment((int)campsiteID, trans);
			}

			int res = Db.Execute(query, parameters, trans);
			if (res == 0) {
				trans.Rollback();
				return false;
			}
			trans.Commit();
			return true;
		}
		catch (InvalidOperationException e) {
			trans.Rollback();
			throw new InvalidOperationException(e.Message);
		}
		catch (KeyNotFoundException e) {
			trans.Rollback();
			throw new KeyNotFoundException(e.Message);
		}
		catch (Exception e) {
			trans.Rollback();
			throw new Exception(e.Message);
		}
		finally {
			if (Db.State == ConnectionState.Open) Db.Close();
		}
	}
/// <summary>
/// Deletes all attributes for the given campsite.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to delete attributes for.</param>
/// <param name="trans">The transaction to use for database operations.</param>
	private void DeleteAttributes(int campsiteID, IDbTransaction? trans = null) {
		string deleteQuery = DeleteAttributesQuery();
		Db.Execute(deleteQuery, new { campsiteID }, trans);
	}
/// <summary>
/// Deletes all equipment for the given campsite.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to delete equipment for.</param>
/// <param name="trans">The transaction to use for database operations.</param>
	private void DeleteEquipment(int campsiteID, IDbTransaction? trans = null) {
		string deleteQuery = DeleteEquipmentQuery();
		Db.Execute(deleteQuery, new { campsiteID }, trans);
	}
/// <summary>
/// Deletes all reservations for the given campsite.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to delete reservations for.</param>
/// <param name="trans">The transaction to use for database operations.</param>
	private void DeleteReservations(int campsiteID, IDbTransaction? trans = null) {
		ReservationService.Value.DeleteReservations(campsiteID, trans);
	}
/// <summary>
/// Retrieves a campsite based on the facility ID, loop, and name.
/// </summary>
/// <param name="facilityID">The ID of the facility where the campsite is located.</param>
/// <param name="loop">The loop identifier within the facility.</param>
/// <param name="name">The name of the campsite.</param>
/// <returns>The campsite if found, otherwise null.</returns>
	private Campsite? GetCampsite(int facilityID, string loop, string name) {
		string selectQuery = SelectCampsiteAtFacilityQuery();
		var campsite = Db.QueryFirstOrDefault<Campsite?>(selectQuery, new {facilityID, loop, name });
		return campsite;
	}
/// <summary>
/// Checks if a campsite has available spots for a given date.
/// </summary>
/// <param name="campsite">The campsite to check.</param>
/// <param name="date">The date to check availability for.</param>
/// <returns>True if the campsite has available spots, false otherwise.</returns>
	private bool CampIsAvailabile(Campsite campsite, DateTime date) {
		return GetCampsiteTakenSpots(campsite.Id, date) < campsite.Capacity;
	}
/// <summary>
/// Retrieves the number of taken spots for the given campsite on a given date.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to check availability for.</param>
/// <param name="date">The date to check availability for.</param>
/// <returns>The number of taken spots for the given campsite on the given date.</returns>
	private int GetCampsiteTakenSpots(int campsiteID, DateTime date) {
		string selectQuery = CampAvailabilityQuery();
		int takenSpots = Db.QueryFirstOrDefault<int>(selectQuery, new { campsiteID, date });
		return takenSpots;
	}
/// <summary>
/// Adds a new campsite to the database. If the campsite already exists,
/// then this will throw an exception. This will also add the attributes and
/// equipment associated with the campsite.
/// </summary>
/// <param name="campsite">The campsite to add.</param>
/// <param name="attributes">The attributes of the campsite.</param>
/// <param name="equipment">The equipment of the campsite.</param>
/// <returns>True if the campsite was added, false otherwise.</returns>
/// <exception cref="InvalidOperationException">If the facilityID of the campsite does not exist.</exception>
/// <exception cref="DataException">If the campsite already exists.</exception>
/// <exception cref="Exception">Thrown for any other general exceptions.</exception>
	private bool InsertCampAndAttrsAndEquip(Campsite campsite, CampAttribute[] attributes, Equipment[] equipment) {
		if (Db.State == ConnectionState.Closed) Db.Open();
		using var trans = Db.BeginTransaction();

		try {
			int campsiteID = InsertCampsite(campsite, trans);
			InsertAttributes(campsiteID, attributes, trans);
			InsertEquipment(campsiteID, equipment, trans);
			trans.Commit();
			return true;
		}
		catch (InvalidOperationException e) {
			trans.Rollback();
			throw new InvalidOperationException(e.Message);
		}
		catch (DataException e) {
			trans.Rollback();
			throw new DataException(e.Message);
		}
		catch (Exception e) {
			trans.Rollback();
			throw new Exception(e.Message);
		}
		finally {
			if (Db.State == ConnectionState.Open) Db.Close();
		}
	}
/// <summary>
/// Gets the ID of the given attribute name.
/// </summary>
/// <param name="name">The name of the attribute.</param>
/// <returns>The ID of the attribute.</returns>
/// <exception cref="InvalidOperationException">Thrown when the attribute name is invalid.</exception>
	private int GetAttributeId(string name) {
		string selectQuery = SelectAttributeIdQuery();
		var attributeID = Db.QueryFirstOrDefault<string?>(selectQuery, new { name })
		?? throw new InvalidOperationException("Invalid attribute name");
		return int.Parse(attributeID);
	}
/// <summary>
/// Gets the ID of the given equipment name.
/// </summary>
/// <param name="name">The name of the equipment.</param>
/// <returns>The ID of the equipment.</returns>
/// <exception cref="InvalidOperationException">Thrown when the equipment name is invalid.</exception>
	private int GetEquipmentId(string name) {
		string selectQuery = SelectEquipmentIdQuery();
		var equipmentID = Db.QueryFirstOrDefault<string?>(selectQuery, new { name })
		?? throw new InvalidOperationException("Invalid equipment name");
		return int.Parse(equipmentID);
	}
/// <summary>
/// Gets the parameters for the attributes for a given campsite.
/// </summary>
/// <param name="attributes">The attributes of the campsite.</param>
/// <param name="campsiteID">The ID of the campsite.</param>
/// <returns>The parameters for the attributes.</returns>
/// <exception cref="InvalidOperationException">Thrown when the attribute value is invalid.</exception>
	private object GetAttributeParams(CampAttribute[] attributes, int campsiteID) {
		return attributes.Select(attr => {
			int attributeID = GetAttributeId(attr.Name);
			string value = attr.Value;
			if (string.IsNullOrEmpty(value)) {
				throw new InvalidOperationException("Invalid attribute value");
			}
			return new { campsiteID, attributeID, value };
		});
	}
/// <summary>
/// Gets the parameters for the equipment for a given campsite.
/// </summary>
/// <param name="equipment">The equipment of the campsite.</param>
/// <param name="campsiteID">The ID of the campsite.</param>
/// <returns>The parameters for the equipment.</returns>
/// <exception cref="InvalidOperationException">Thrown when the equipment name is invalid.</exception>
	private object GetEquipmentParams(Equipment[] equipment, int campsiteID) {
		return equipment.Select(equip => {
			int equipmentID = GetEquipmentId(equip.Name);
			return new { campsiteID, equipmentID };
		});
	}
/// <summary>
/// Inserts a new campsite into the database.
/// </summary>
/// <param name="campsite">The campsite to insert.</param>
/// <param name="trans">The transaction to use for database operations.</param>
/// <returns>The ID of the inserted campsite.</returns>
/// <exception cref="DataException">Thrown when there is an issue with database operations.</exception>
	private int InsertCampsite(Campsite campsite, IDbTransaction trans) {
		string insertQuery = InsertCampsiteQuery();
		var campsiteID = Db.QueryFirstOrDefault<string?>(insertQuery, campsite, trans)
		?? throw new DataException("Failed to insert campsite");
		return int.Parse(campsiteID);
	}
/// <summary>
/// Inserts the given attributes for a specific campsite into the database.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to insert attributes for.</param>
/// <param name="attributes">An array of attributes to be inserted for the campsite.</param>
/// <param name="trans">The transaction to use for database operations.</param>
/// <exception cref="DataException">Thrown when the attributes fail to be inserted into the database.</exception>
	private void InsertAttributes(int campsiteID, CampAttribute[] attributes, IDbTransaction trans) {
		string insertQuery = InsertAttributeQuery();
		var parameters = GetAttributeParams(attributes, campsiteID);
		int res = Db.Execute(insertQuery, parameters, trans);
		if (res == 0) throw new DataException("Failed to insert attributes");
	}
/// <summary>
/// Inserts the given equipment for a specific campsite into the database.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to insert equipment for.</param>
/// <param name="equipment">An array of equipment to be inserted for the campsite.</param>
/// <param name="trans">The transaction to use for database operations.</param>
/// <exception cref="DataException">Thrown when the equipment fails to be inserted into the database.</exception>
	private void InsertEquipment(int campsiteID, Equipment[] equipment, IDbTransaction trans) {
		string insertQuery = InsertEquipmentQuery();
		var parameters = GetEquipmentParams(equipment, campsiteID);
		int res = Db.Execute(insertQuery, parameters, trans);
		if (res == 0) throw new DataException("Failed to insert equipment");
	}

	#endregion

	#region Queries
	
	private static string SelectCampsitesByFacilityQuery() {
		return @"SELECT * FROM campsites WHERE facilityID = @facilityID AND active = 1";
	}
	private static string DeleteReservationsByCapacityQuery(string subqueryPart) {
		return @$"
			DELETE FROM reservations
			WHERE id IN (
				SELECT id FROM (
					{subqueryPart}
					ORDER BY createdAt
					DESC
					LIMIT @difference
				) AS subquery
			);
		";
	}
	private static string SelectCampsiteQuery() {
		return "SELECT * FROM campsites WHERE id = @campsiteID AND active = 1;";
	}
	private static string UpdateCapacityQuery() {
		return "UPDATE campsites SET capacity = @capacity WHERE id = @campsiteID AND active = 1;";
	}
	private static string EnableCampsiteQuery() {
		return "UPDATE campsites SET active = 1 WHERE id = @campsiteID AND active = 0;";
	}
	private static string DisableCampsiteQuery() {
		return "UPDATE campsites SET active = 0 WHERE id = @campsiteID AND active = 1;";
	}
	private static string DeleteCampsiteQuery() {
		return "DELETE FROM campsites WHERE id = @campsiteID;";
	}
	private static string SelectEquipmentIdQuery() {
		return "SELECT id FROM equipment WHERE name = @name;";
	}
	private static string SelectAttributeIdQuery() {
		return "SELECT id FROM attributes WHERE name = @name;";
	}
	private static string InsertEquipmentQuery() {
		return "INSERT INTO campsites_equipment (campsiteID, equipmentID) VALUES (@campsiteID, @equipmentID);";
	}
	private static string InsertAttributeQuery() {
		return "INSERT INTO campsites_attributes (campsiteID, attributeID, value) VALUES (@campsiteID, @attributeID, @value);";
	}
	private static string DeleteAttributesQuery() {
		return "DELETE FROM campsites_attributes WHERE campsiteID = @campsiteID;";
	}
	private static string DeleteEquipmentQuery() {
		return "DELETE FROM campsites_equipment WHERE campsiteID = @campsiteID;";
	}
	private static string CampAvailabilityQuery() {
		return "SELECT COUNT(*) FROM reservations WHERE campsiteID = @campsiteID AND (checkIn <= @date AND @date <= checkOut)";
	}
	private static string SelectCampsiteAtFacilityQuery() {
		return  @"
			SELECT * FROM campsites 
			WHERE facilityID = @facilityID 
			AND `loop` = @loop 
			AND name = @name 
			AND active = 1;";
	}
	private static string InsertCampsiteQuery() {
		return  "INSERT INTO campsites (`loop`, name, facilityID) VALUES (@loop, @name, @facilityID); SELECT LAST_INSERT_ID();";
	}

	#endregion
}
