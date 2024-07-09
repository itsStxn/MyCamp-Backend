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

	public Campsite[] GetCampsites(int facilityID) {
		string selectQuery = SelectCampsitesByFacilityQuery();
		var camps = Db.Query<Campsite>(selectQuery, new { facilityID }).ToArray();
		foreach (var campsite in camps) {
			campsite.Attributes = AttributeService.GetAttributesByCampsite(campsite.Id);
			campsite.Equipment = EquipmentService.GetEquipmentByCampsite(campsite.Id);
		}
		return camps;
	}
	public Campsite? GetCampsite(int campsiteID) {
		string selectQuery = SelectCampsiteQuery();
		return Db.QueryFirstOrDefault<Campsite?>(selectQuery, new { campsiteID });
	}
	public bool AddCampsite(Campsite campsite, CampAttribute[] attributes, Equipment[] equipment) {
		Facility? facility = FacilityService.Value.GetFacility(campsite.FacilityID)
		?? throw new KeyNotFoundException("Facility not found");

		Campsite? foundCampsite = GetCampsite(facility.Id, campsite.Loop, campsite.Name);
		if (foundCampsite != null) {
			throw new InvalidOperationException("Campsite already exists");
		}
		return InsertCampAndAttrsAndEquip(campsite, attributes, equipment);
	}
	public bool DeleteCampsite(int campsiteID) {
		string deleteQuery = DeleteCampsiteQuery();
		return DeleteReservationsAndRunQuery(deleteQuery, new { campsiteID });
	}
	public Dictionary<string, bool> GetAvailabilities(int campsiteID) {
		Campsite? campsite = GetCampsite(campsiteID)
		??throw new KeyNotFoundException("Campsite not found");
		
		Dictionary<string, bool> availabilities = [];
		for (DateTime date = DateTime.Today; date <= DateTime.Today.AddMonths(2); date = date.AddDays(1)) {
			availabilities.Add(date.ToString("yyyy-MM-dd"), CampIsAvailabile(campsite, date));
		}
		return availabilities;
	}
	public bool EnableCampsite(int campsiteID) {
		string updateQuery = EnableCampsiteQuery();
		return Db.Execute(updateQuery, new { campsiteID }) > 0;
	}
	public bool DisableCampsite(int campsiteID) {
		string updateQuery = DisableCampsiteQuery();
		return DeleteReservationsAndRunQuery(updateQuery, new { campsiteID });
	}
	public bool UpdateCapacity(int campsiteID, int capacity) {
		if (Db.State == ConnectionState.Closed) Db.Open();
		using var trans = Db.BeginTransaction();

		try {
			Reservation[] reservations = GetReservations(campsiteID);
			HashSet<DateTime> visitedDates = [];
			foreach (Reservation re in reservations) {
				for(DateTime date = re.CheckIn; date <= re.CheckOut; date = date.AddDays(1)) {
					if (visitedDates.Add(date)) {
						CutExtraReservedSpots(campsiteID, date, capacity, trans);
					}}}

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

	private Reservation[] GetReservations(int campsiteID) {
		Reservation[] reservations = ReservationService.Value.GetReservationsByCampsite(campsiteID);
		if (reservations.Length == 0) throw new KeyNotFoundException("Campsite not found");
		return reservations;
	}
	private void CutExtraReservedSpots(int campsiteID, DateTime date, int capacity, IDbTransaction trans) {
		int difference = GetCampsiteTakenSpots(campsiteID, date) - capacity;
		if (difference > 0) {
			string selectQuery = CampAvailabilityQuery().Replace("COUNT(*)", "id");
			string deleteQuery = DeleteReservationsByCapacityQuery(selectQuery);
			int res = Db.Execute(deleteQuery, new { campsiteID, date, difference }, trans);
			if (res == 0) throw new DataException("Failed to delete extra reservations");
		}
	}
	private bool DeleteReservationsAndRunQuery(string query, object parameters) {
		var campsiteIdProp = parameters.GetType().GetProperty("campsiteID");
		if (Db.State == ConnectionState.Closed) Db.Open();
		using var trans = Db.BeginTransaction();

		try {
			var campsiteID = campsiteIdProp?.GetValue(parameters)
			?? throw new InvalidOperationException("Invalid query parameters");
			if (!DeleteReservations((int)campsiteID, trans)) {
				throw new KeyNotFoundException("Campsite not found");
			};

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
	private bool DeleteReservations(int campsiteID, IDbTransaction? trans = null) {
		return ReservationService.Value.DeleteReservations(campsiteID, trans);
	}
	private Campsite? GetCampsite(int facilityID, string loop, string name) {
		string selectQuery = SelectCampsiteAtFacilityQuery();
		var campsite = Db.QueryFirstOrDefault<Campsite?>(selectQuery, new {facilityID, loop, name });
		return campsite;
	}
	private bool CampIsAvailabile(Campsite campsite, DateTime date) {
		return GetCampsiteTakenSpots(campsite.Id, date) < campsite.Capacity;
	}
	private int GetCampsiteTakenSpots(int campsiteID, DateTime date) {
		string selectQuery = CampAvailabilityQuery();
		int takenSpots = Db.QueryFirstOrDefault<int>(selectQuery, new { campsiteID, date });
		return takenSpots;
	}
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
	private int GetAttributeId(string name) {
		string selectQuery = SelectAttributeIdQuery();
		var attributeID = Db.QueryFirstOrDefault<string?>(selectQuery, new { name })
		?? throw new InvalidOperationException("Invalid attribute name");
		return int.Parse(attributeID);
	}
	private int GetEquipmentId(string name) {
		string selectQuery = SelectEquipmentIdQuery();
		var equipmentID = Db.QueryFirstOrDefault<string?>(selectQuery, new { name })
		?? throw new InvalidOperationException("Invalid equipment name");
		return int.Parse(equipmentID);
	}
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
	private object GetEquipmentParams(Equipment[] equipment, int campsiteID) {
		return equipment.Select(equip => {
			int equipmentID = GetEquipmentId(equip.Name);
			return new { campsiteID, equipmentID };
		});
	}
	private int InsertCampsite(Campsite campsite, IDbTransaction trans) {
		string insertQuery = InsertCampsiteQuery();
		var campsiteID = Db.QueryFirstOrDefault<string?>(insertQuery, campsite, trans)
		?? throw new DataException("Failed to insert campsite");
		return int.Parse(campsiteID);
	}
	private void InsertAttributes(int campsiteID, CampAttribute[] attributes, IDbTransaction trans) {
		string insertQuery = InsertAttributeQuery();
		var parameters = GetAttributeParams(attributes, campsiteID);
		int res = Db.Execute(insertQuery, parameters, trans);
		if (res == 0) throw new DataException("Failed to insert attributes");
	}
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
