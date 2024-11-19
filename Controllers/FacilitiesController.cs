using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;
using Server.Utils;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FacilitiesController(IFacilityService facilityService) : ControllerBase {
	private readonly IFacilityService FacilityService = facilityService;

/// <summary>
/// Retrieves facilities based on the given activities, states, and rating.
/// </summary>
/// <param name="activities">The activities to filter by.</param>
/// <param name="states">The states to filter by.</param>
/// <param name="rating">The rating to filter by.</param>
/// <returns>An array of facilities that match the given filters.</returns>
	[Authorize]
	[HttpGet]
	[ProducesResponseType(typeof(Facility[]), StatusCodes.Status200OK)]
	public IActionResult GetFacilities([FromQuery] string[] activities, [FromQuery] string[] states, [FromQuery] string? rating) {
		try {
			Facility[] facilities = FacilityService.GetFacilities(activities, states, rating);
			return Ok(facilities);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching facilities: {e.Message}");
		}
	}
	
/// <summary>
/// Retrieves a facility by its unique identifier.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve.</param>
/// <returns>The facility with the specified ID, or a not found response if the facility does not exist.
/// Returns an internal server error response in case of an exception.</returns>
	[Authorize]
	[HttpGet("{facilityID}")]
	[ProducesResponseType(typeof(Facility), StatusCodes.Status200OK)]
	public IActionResult GetFacility(int facilityID) {
		try {
			Facility? facility = FacilityService.GetFacility(facilityID);
			if (facility == null) return NotFound("Facility not found");
			return Ok(facility);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching facility: {e.Message}");
		}
	}
	
/// <summary>
/// Retrieves all addresses associated with a specific facility.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve addresses for.</param>
/// <returns>An array of addresses related to the specified facility, or a not found response if the facility does not exist.
/// Returns an internal server error response in case of an exception.</returns>
	[Authorize]
	[HttpGet("{facilityID}/addresses")]
	[ProducesResponseType(typeof(Address[]), StatusCodes.Status200OK)]
	public IActionResult GetAddresses(int facilityID) {
		try {
			Address[] addresses = FacilityService.GetAddresses(facilityID);
			if (addresses.Length == 0) return NotFound("Addresses not found");
			return Ok(addresses);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching addresses: {e.Message}");
		}
	}    
	
/// <summary>
/// Retrieves all campsites associated with a specific facility.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve campsites for.</param>
/// <returns>An array of campsites related to the specified facility, or a not found response if the facility does not exist.
/// Returns an internal server error response in case of an exception.</returns>
	[Authorize]
	[HttpGet("{facilityID}/campsites")]
	[ProducesResponseType(typeof(Campsite[]), StatusCodes.Status200OK)]
	public IActionResult GetCampsites(int facilityID) {
		try {
			Campsite[] camps = FacilityService.GetCampsites(facilityID);
			return Ok(camps);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching camps: {e.Message}");
		}
	}
	
/// <summary>
/// Retrieves all activities associated with a specific facility.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve activities for.</param>
/// <returns>An array of activities related to the specified facility, or an internal server error response in case of an exception.</returns>
	[Authorize]
	[HttpGet("{facilityID}/activities")]
	[ProducesResponseType(typeof(Activity[]), StatusCodes.Status200OK)]
	public IActionResult GetActivities(int facilityID) {
		try {
			Activity[] activities = FacilityService.GetActivities(facilityID);
			return Ok(activities);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching activities: {e.Message}");
		}
	}
	
/// <summary>
/// Retrieves all media associated with a specific facility.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve media for.</param>
/// <returns>An array of media related to the specified facility, or a not found response if the facility does not exist.
/// Returns an internal server error response in case of an exception.</returns>
	[Authorize]
	[HttpGet("{facilityID}/media")]
	[ProducesResponseType(typeof(Media), StatusCodes.Status200OK)]
	public IActionResult GetMedia(int facilityID) {
		try {
			Media[] media = FacilityService.GetMedia(facilityID);
			if (media.Length == 0) return NotFound("Media not found");
			return Ok(media);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching media: {e.Message}");
		}
	}
	
/// <summary>
/// Retrieves the average rating for a specific facility.
/// </summary>
/// <param name="facilityID">The ID of the facility whose rating is to be retrieved.</param>
/// <returns>The average rating of the facility, or an internal server error response in case of an exception.</returns>
	[Authorize]
	[HttpGet("{facilityID}/rating")]
	[ProducesResponseType(typeof(FacilityRating), StatusCodes.Status200OK)]
	public IActionResult GetRating(int facilityID) {
		try {
			FacilityRating? rating = FacilityService.GetFacilityRating(facilityID);
			return Ok(rating);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching rating: {e.Message}");
		}
	}

/// <summary>
/// Retrieves all facilities that have been saved by the authenticated user.
/// </summary>
/// <returns>An array of facilities saved by the user, or a bad request response if the user ID could not be retrieved.
/// Returns an internal server error response in case of an exception.</returns>
	[Authorize]
	[HttpGet("saved")]
	[ProducesResponseType(typeof(Facility[]), StatusCodes.Status200OK)]
	public IActionResult GetSavedFacilities() {
		try {
			var userIdStr = RequestHelper.GetNameIdentifier(User);
			int userID = int.Parse(userIdStr);
			return Ok(FacilityService.GetSavedFacilities(userID));
		} 
		catch (BadHttpRequestException e) {
			Console.WriteLine(e);
			return BadRequest(e.Message);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching saved facilities: {e.Message}");
		}
	}

/// <summary>
/// Saves or un-saves a facility for the authenticated user.
/// </summary>
/// <param name="facilityID">The ID of the facility to save or un-save.</param>
/// <returns>A success message if the facility was saved or un-saved, or a bad request response in case of an invalid facility ID.
/// Returns an internal server error response in case of an exception.</returns>
	[Authorize]
	[HttpPost("save")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult SaveFacility([FromQuery] int facilityID) {
		try {
			var userIdStr = RequestHelper.GetNameIdentifier(User);
			int userID = int.Parse(userIdStr);
			bool saved = FacilityService.SaveFacility(facilityID, userID);
			if (!saved) return BadRequest("Failed to save/unsave facility");
			return Ok("Facility saved/unsaved successfully");
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues saving/unsaving facility: {e.Message}");
		}
	}

/// <summary>
/// Rates a facility. If the user has already rated the facility, their existing rating will be updated.
/// If the user is attempting to rate the facility the same as their existing rating, their rating will be deleted.
/// </summary>
/// <param name="scoring">The score to rate the facility.</param>
/// <returns>A success message if the facility was rated, or a bad request response if the facility does not exist.
/// Returns an internal server error response in case of an exception.</returns>
	[Authorize]
	[HttpPost("rate")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult RateFacility([FromBody] FacilityScore scoring) {
		try {
			var userIdStr = RequestHelper.GetNameIdentifier(User);
			int userID = int.Parse(userIdStr);
			bool rated = FacilityService.RateFacility(scoring.Score, scoring.FacilityID, userID);
			if (!rated) return BadRequest("Failed to rate facility");
			return Ok("Facility rated successfully");
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues rating facility: {e.Message}");
		}
	}

/// <summary>
/// Retrieves all comments from a given facility, along with like information for the authenticated user.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve comments from.</param>
/// <returns>An array of comments associated with the specified facility, or an internal server error response if an exception occurs.</returns>
	[Authorize]
	[HttpGet("{facilityID}/comments")]
	[ProducesResponseType(typeof(Comment[]), StatusCodes.Status200OK)]
	public IActionResult GetComments(int facilityID) {
		try {
			int userID = int.Parse(RequestHelper.GetNameIdentifier(User));
			Comment[] comments = FacilityService.GetComments(facilityID, userID);
			return Ok(comments);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching comments: {e.Message}");
		}
	}
}
