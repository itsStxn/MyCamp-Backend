using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;
using Server.Utils;
using System.Data;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController(ICommentService commentService, RequestHelper requestHelper) : ControllerBase{
	private readonly ICommentService CommentService = commentService;
	private readonly RequestHelper RequestHelper = requestHelper;

	[HttpPost("add")]
	[ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
	public IActionResult AddComment([FromBody] Comment comment) {
		try {
			comment.UserID = int.Parse(RequestHelper.GetNameIdentifier(User));
			CommentService.AddComment(comment);
			return Ok("Comment added successfully");
		}
		catch (DataException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		}
		catch (InvalidOperationException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues adding comment: {e.Message}");
		}
	}

	[HttpDelete("delete")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult DeleteComment([FromQuery] int commentID) {
		try {
			int userID = int.Parse(RequestHelper.GetNameIdentifier(User));
			CommentService.DeleteComment(commentID, userID);
			return Ok("Comment deleted successfully");
		}
		catch (DataException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues deleting comment: {e.Message}");
		}
	}

	[HttpPost("like")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult LikeComment([FromQuery] int commentID) {
		try {
			int userID = int.Parse(RequestHelper.GetNameIdentifier(User));
			bool liked = CommentService.LikeComment(commentID, userID);
			if (!liked) return BadRequest("Failed to like comment");
			return Ok("Comment liked successfully");
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues liking comment: {e.Message}");
		}
	}

	[HttpPost("dislike")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult DislikeComment([FromQuery] int commentID) {
		try {
			int userID = int.Parse(RequestHelper.GetNameIdentifier(User));
			bool disliked = CommentService.DislikeComment(commentID, userID);
			if (!disliked) return BadRequest("Failed to dislike comment");
			return Ok("Comment disliked successfully");
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues disliking comment: {e.Message}");
		}
	}

	[HttpGet("likeInfo")]
	[ProducesResponseType(typeof(LikeInfo), StatusCodes.Status200OK)]
	public IActionResult GetLikeInfo([FromQuery] int commentID) {
		try {
			int userID = int.Parse(RequestHelper.GetNameIdentifier(User));
			LikeInfo likeInfo = CommentService.GetLikeInfo(commentID, userID);
			return Ok(likeInfo);
		}
		catch (KeyNotFoundException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues getting like info: {e.Message}");
		}
	}

	[HttpPost("reply")]
	[ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
	public IActionResult AddReply([FromBody] Reply reply) {
		try {
			reply.UserID = int.Parse(RequestHelper.GetNameIdentifier(User));
			CommentService.AddReply(reply);
			return Ok("Reply added successfully");
		}
		catch (KeyNotFoundException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		}
		catch (DataException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		}
		catch (InvalidOperationException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues adding reply: {e.Message}");
		}
	}
}
