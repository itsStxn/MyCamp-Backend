using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;
using Server.Utils;
using System.Data;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController(ICommentService commentService) : ControllerBase {
	private readonly ICommentService CommentService = commentService;

/// <summary>
/// Adds a new comment to the database.
/// </summary>
/// <param name="comment">The comment to add, including its content, facility ID, and user ID.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <exception cref="DataException">Thrown when there is an issue adding the comment.</exception>
/// <exception cref="KeyNotFoundException">Thrown when the facility ID of the comment does not correspond to a valid facility.</exception>
/// <exception cref="InvalidOperationException">Thrown when the comment content is empty.</exception>
/// <exception cref="Exception">Thrown for any other general exceptions.</exception>
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
		catch (KeyNotFoundException e) {
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

/// <summary>
/// Deletes a comment from the database.
/// </summary>
/// <param name="commentID">The ID of the comment to delete.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <exception cref="DataException">Thrown when there is an issue deleting the comment.</exception>
/// <exception cref="Exception">Thrown for any other general exceptions.</exception>
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

/// <summary>
/// Likes a comment in the database for the current user.
/// </summary>
/// <param name="commentID">The ID of the comment to like.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <exception cref="Exception">Thrown for any general exceptions occurring during the process.</exception>
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

/// <summary>
/// Dislikes a comment in the database for the current user.
/// </summary>
/// <param name="commentID">The ID of the comment to dislike.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <exception cref="Exception">Thrown for any general exceptions occurring during the process.</exception>
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

/// <summary>
/// Retrieves like information for the current user for a given comment.
/// </summary>
/// <param name="commentID">The ID of the comment to retrieve like info for.</param>
/// <returns>A <see cref="LikeInfo"/> containing the number of likes, dislikes, and the user's like/dislike state for the given comment.</returns>
/// <exception cref="Exception">Thrown for any general exceptions occurring during the process.</exception>
	[HttpGet("likeInfo")]
	[ProducesResponseType(typeof(LikeInfo), StatusCodes.Status200OK)]
	public IActionResult GetLikeInfo([FromQuery] int commentID) {
		try {
			int userID = int.Parse(RequestHelper.GetNameIdentifier(User));
			LikeInfo likeInfo = CommentService.GetLikeInfo(commentID, userID);
			return Ok(likeInfo);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues getting like info: {e.Message}");
		}
	}

/// <summary>
/// Adds a new reply to a comment in the database.
/// </summary>
/// <param name="reply">The reply to add, including its content, comment ID, and user ID.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <exception cref="KeyNotFoundException">Thrown when the comment ID of the reply does not correspond to a valid comment.</exception>
/// <exception cref="DataException">Thrown when there is an issue adding the reply.</exception>
/// <exception cref="InvalidOperationException">Thrown when the reply content is empty.</exception>
/// <exception cref="Exception">Thrown for any other general exceptions.</exception>
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
