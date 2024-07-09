using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;
using Server.Utils;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FacilitiesController(IFacilityService facilityService, RequestHelper requestHelper) : ControllerBase {
    private readonly IFacilityService FacilityService = facilityService;
    private readonly RequestHelper ReqHelper = requestHelper;

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

    [Authorize]
    [HttpGet("saved")]
    [ProducesResponseType(typeof(Facility[]), StatusCodes.Status200OK)]
    public IActionResult GetSavedFacilities() {
        try {
            var userIdStr = ReqHelper.GetNameIdentifier(User);
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

    [Authorize]
    [HttpPost("save")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult SaveFacility([FromQuery] int facilityID) {
        try {
            var userIdStr = ReqHelper.GetNameIdentifier(User);
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

    [Authorize]
    [HttpPost("rate")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult RateFacility([FromBody] FacilityScore scoring) {
        try {
            var userIdStr = ReqHelper.GetNameIdentifier(User);
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

    [Authorize]
    [HttpGet("{facilityID}/comments")]
    [ProducesResponseType(typeof(Comment[]), StatusCodes.Status200OK)]
    public IActionResult GetComments(int facilityID) {
        try {
            int userID = int.Parse(ReqHelper.GetNameIdentifier(User));
            Comment[] comments = FacilityService.GetComments(facilityID, userID);
            return Ok(comments);
        } 
        catch (Exception e) {
            Console.WriteLine(e);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching comments: {e.Message}");
        }
    }
}
