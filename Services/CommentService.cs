using Server.Interfaces;
using Server.Models;
using Server.Utils;
using System.Data;
using Dapper;

namespace Server.Services;

public class CommentService(IDbConnection db, Lazy<IFacilityService> facilityService) : ICommentService {
	private readonly IDbConnection Db = db;
	private readonly Lazy<IFacilityService> FacilityService = facilityService;

/// <summary>
/// Retrieves all comments from a given facility, with the number of likes and dislikes from the given user.
/// </summary>
/// <param name="facilityID">The ID of the facility to retrieve comments from.</param>
/// <param name="userID">The ID of the user to retrieve like info for.</param>
/// <returns>An array of <see cref="Comment"/>s.</returns>
	public Comment[] GetComments(int facilityID, int userID) {
		string selectQuery = SelectCommentsQuery();
		var comments = Db.Query<Comment>(selectQuery, new { facilityID }).ToArray();
		foreach (var comment in comments) {
			comment.LikeInfo = GetLikeInfo(comment.Id, userID);
			comment.Replies = GetReplies(comment.Id);
		}
		return comments;
	}
/// <summary>
/// Adds a comment to the database.
/// </summary>
/// <param name="comment">The comment to add.</param>
/// <param name="transaction">The transaction to use for database operations, if any.</param>
/// <returns>The ID of the added comment.</returns>
/// <exception cref="DataException">Thrown when the comment fails to be added.</exception>
	public int AddComment(Comment comment, IDbTransaction? transaction = null) {
		ValidateComment(comment);
		string insertQuery = InsertCommentQuery();
		var commentID = Db.QueryFirstOrDefault<string?>(insertQuery, comment, transaction)
		?? throw new DataException("Failed to add comment");
		return int.Parse(commentID);
	}
/// <summary>
/// Deletes a comment from the database.
/// </summary>
/// <param name="commentID">The ID of the comment to delete.</param>
/// <param name="userID">The ID of the user who is deleting the comment.</param>
/// <exception cref="DataException">Thrown when there is an issue deleting the comment.</exception>
	public void DeleteComment(int commentID, int userID) {
		if (Db.State == ConnectionState.Closed) Db.Open();
		var parameters = new { commentID, userID };
		var transaction = Db.BeginTransaction();

		try {
			Db.Execute(DeleteLikeStatesQuery(), new { commentID }, transaction);
			int res = Db.Execute(DeleteCommentQuery(), parameters, transaction);
			if (res == 0) throw new DataException("Failed to delete comment");
			transaction.Commit();
		}
		catch (DataException e) {
			transaction.Rollback();
			throw new DataException(e.Message);
		}
		catch (Exception e) {
			transaction.Rollback();
			throw new Exception(e.Message);
		}
		finally {
			if (Db.State == ConnectionState.Open) Db.Close();
		}
	} 
/// <summary>
/// Likes a comment.
/// If the user has not liked/disliked the comment, inserts a new like state.
/// If the user has liked the comment, deletes the like state.
/// If the user has disliked the comment, updates the like state to liked.
/// </summary>
/// <param name="commentID">The ID of the comment to like.</param>
/// <param name="userID">The ID of the user who is liking the comment.</param>
/// <returns>True if the comment was liked successfully, otherwise false.</returns>
	public bool LikeComment(int commentID, int userID) {
		var likeState = GetLikeState(commentID, userID);
		if (likeState == null) return InsertLike(commentID, userID);
		if (likeState.Liked) return DeleteLikeState(commentID, userID);
		return UpdateLike(commentID, userID);
	}
/// <summary>
/// Dislikes a comment.
/// If the user has not liked/disliked the comment, inserts a new dislike state.
/// If the user has disliked the comment, deletes the dislike state.
/// If the user has liked the comment, updates the like state to disliked.
/// </summary>
/// <param name="commentID">The ID of the comment to dislike.</param>
/// <param name="userID">The ID of the user who is disliking the comment.</param>
/// <returns>True if the comment was disliked successfully, otherwise false.</returns>
	public bool DislikeComment(int commentID, int userID) {
		var likeState = GetLikeState(commentID, userID);
		if (likeState == null) return InsertDislike(commentID, userID);
		if (likeState.Disliked) return DeleteLikeState(commentID, userID);
		return UpdateDislike(commentID, userID);
	}
/// <summary>
/// Retrieves like information for a specific comment and user.
/// </summary>
/// <param name="commentID">The ID of the comment to retrieve like info for.</param>
/// <param name="userID">The ID of the user to retrieve like info for.</param>
/// <returns>A <see cref="LikeInfo"/> object containing the number of likes, dislikes, and the user's like/dislike state for the comment.</returns>
	public LikeInfo GetLikeInfo(int commentID, int userID) {
		var likeState = GetLikeState(commentID, userID);

		return new() {
			Likes = CountLikes(commentID),
			Dislikes = CountDislikes(commentID),
			Liked = likeState?.Liked ?? false,
			Disliked = likeState?.Disliked ?? false
		};
	}
/// <summary>
/// Adds a reply to a comment in the database.
/// The reply is validated before insertion. If the database connection is closed, it opens a new connection.
/// A transaction is started to ensure atomicity. The reply is added as a comment first, then inserted as a reply.
/// If the insertion fails, a DataException is thrown, and the transaction is rolled back.
/// If any exception occurs, the transaction is rolled back and the exception is rethrown.
/// Finally, the database connection is closed if it was previously opened.
/// </summary>
/// <param name="reply">The reply object containing the details of the reply to be added.</param>
/// <exception cref="DataException">Thrown when the reply could not be added to the database.</exception>
/// <exception cref="Exception">Thrown when any other error occurs during the operation.</exception>
	public void AddReply(Reply reply) {
		ValidateReply(reply);
		if (Db.State == ConnectionState.Closed) Db.Open();
		var transaction = Db.BeginTransaction();

		try {
			int commentID = AddComment(reply, transaction);
			string insertQuery = InsertReplyQuery();
			var parameters = new { reply.Thread, reply.ReplyTo, commentID };
			int res = Db.Execute(insertQuery, parameters, transaction);

			if (res == 0) throw new DataException("Failed to add reply");
			transaction.Commit();
		}
		catch (DataException e) {
			transaction.Rollback();
			throw new DataException(e.Message);
		}
		catch (Exception e) {
			transaction.Rollback();
			throw new Exception(e.Message);
		}
		finally {
			if (Db.State == ConnectionState.Open) Db.Close();
		}
	}
/// <summary>
/// Retrieves all replies for a given comment from the database.
/// </summary>
/// <param name="commentID">The ID of the comment to retrieve replies for.</param>
/// <returns>An array of <see cref="Reply"/>s associated with the specified comment.</returns>
	public Reply[] GetReplies(int commentID) {
		string selectQuery = SelectRepliesQuery();
		var replies = Db.Query<Reply>(selectQuery, new { commentID });
		return replies.ToArray();
	}
	
	#region Aid Functions

/// <summary>
/// Retrieves the number of likes a comment has from the database.
/// </summary>
/// <param name="commentID">The ID of the comment to retrieve the like count for.</param>
/// <returns>The number of likes the comment has.</returns>
	private int CountLikes(int commentID) {
		string countQuery = CountLikesQuery();
		return Db.ExecuteScalar<int>(countQuery, new { commentID });
	}
/// <summary>
/// Retrieves the number of dislikes a comment has from the database.
/// </summary>
/// <param name="commentID">The ID of the comment to retrieve the dislike count for.</param>
/// <returns>The number of dislikes the comment has.</returns>
	private int CountDislikes(int commentID) {
		string countQuery = CountDislikesQuery();
		return Db.ExecuteScalar<int>(countQuery, new { commentID });
	}
/// <summary>
/// Inserts a like for a given comment and user from the database.
/// </summary>
/// <param name="commentID">The ID of the comment to like.</param>
/// <param name="userID">The ID of the user performing the like.</param>
/// <returns>True if the like was successfully inserted, false otherwise.</returns>
	private bool InsertLike(int commentID, int userID) {
		string insertQuery = InsertLikeQuery();
		int res = Db.Execute(insertQuery, new { commentID, userID });
		return res > 0;
	}
/// <summary>
/// Inserts a dislike for a given comment and user from the database.
/// </summary>
/// <param name="commentID">The ID of the comment to dislike.</param>
/// <param name="userID">The ID of the user performing the dislike.</param>
/// <returns>True if the dislike was successfully inserted, false otherwise.</returns>
	private bool InsertDislike(int commentID, int userID) {
		string insertQuery = InsertDislikeQuery();
		int res = Db.Execute(insertQuery, new { commentID, userID });
		return res > 0;
	}
/// <summary>
/// Updates the like state of the given comment and user to a liked state.
/// </summary>
/// <param name="commentID">The ID of the comment to update the like state for.</param>
/// <param name="userID">The ID of the user who is liking the comment.</param>
/// <returns>True if the like state was successfully updated, false otherwise.</returns>
	private bool UpdateLike(int commentID, int userID) {
		string updateQuery = UpdateLikeQuery();
		int res = Db.Execute(updateQuery, new { commentID, userID });
		return res > 0;
	}
/// <summary>
/// Updates the dislike state of the given comment and user to a disliked state.
/// </summary>
/// <param name="commentID">The ID of the comment to update the dislike state for.</param>
/// <param name="userID">The ID of the user who is disliking the comment.</param>
/// <returns>True if the dislike state was successfully updated, false otherwise.</returns>
	private bool UpdateDislike(int commentID, int userID) {
		string updateQuery = UpdateDislikeQuery();
		int res = Db.Execute(updateQuery, new { commentID, userID });
		return res > 0;
	}
/// <summary>
/// Deletes the like state of the given comment and user.
/// </summary>
/// <param name="commentID">The ID of the comment to delete the like state for.</param>
/// <param name="userID">The ID of the user whose like state is to be deleted.</param>
/// <returns>True if the like state was deleted successfully, false otherwise.</returns>
	private bool DeleteLikeState(int commentID, int userID) {
		string deleteQuery = DeleteLikeStateQuery();
		int res = Db.Execute(deleteQuery, new { commentID, userID });
		return res > 0;
	}
/// <summary>
/// Retrieves the like state of the given comment and user from the database.
/// </summary>
/// <param name="commentID">The ID of the comment to retrieve the like state for.</param>
/// <param name="userID">The ID of the user whose like state is to be retrieved.</param>
/// <returns>The like state of the given comment and user, or null if it does not exist in the database.</returns>
	private LikeState? GetLikeState(int commentID, int userID) {
		string selectQuery = SelectLikeStateQuery();
		var likeState = Db.QueryFirstOrDefault<LikeState?>(selectQuery, new { commentID, userID });
		return likeState;
	}
/// <summary>
/// Validates a comment.
/// Checks that the comment content is not empty.
/// Checks that the facility ID of the comment corresponds to a valid facility.
/// </summary>
/// <param name="comment">The comment to validate.</param>
/// <exception cref="InvalidOperationException">Thrown if the comment content is empty.</exception>
/// <exception cref="KeyNotFoundException">Thrown if the facility ID of the comment does not correspond to a valid facility.</exception>
	private void ValidateComment(Comment comment) {
		if (string.IsNullOrEmpty(comment.Content)) {
			throw new InvalidOperationException("Comment cannot be empty");
		}

		comment.Content = FormHelper.EscapeQuotes(comment.Content);
		Console.WriteLine(comment.FacilityID);
		if (FacilityService.Value.GetFacility(comment.FacilityID) == null) {
			throw new KeyNotFoundException("Facility not found");
		}
	}
/// <summary>
/// Validates a reply.
/// Calls <see cref="ValidateComment(Comment)"/> to validate the reply content.
/// Checks that the author of the reply and the comment being replied to exist in the database.
/// </summary>
/// <param name="reply">The reply to validate.</param>
/// <exception cref="InvalidOperationException">Thrown if the reply content is empty.</exception>
/// <exception cref="KeyNotFoundException">Thrown if the author of the reply or the comment being replied to does not exist in the database.</exception>
	private void ValidateReply(Reply reply) {
		ValidateComment(reply);
		_= GetComment(reply.Thread)
		?? throw new KeyNotFoundException("Author not found");
		_= GetComment(reply.ReplyTo)
		?? throw new KeyNotFoundException("ReplyTo not found");
	}
/// <summary>
/// Retrieves a comment from the database by ID.
/// </summary>
/// <param name="commentID">The ID of the comment to retrieve.</param>
/// <returns>The comment with the given ID, or null if it does not exist in the database.</returns>
	private Comment? GetComment(int commentID) {
		string selectQuery = SelectCommentQuery();
		return Db.QueryFirstOrDefault<Comment?>(selectQuery, new { commentID });
	}

	#endregion

	#region Queries

	private static string SelectCommentQuery() {
		return "SELECT * FROM comments WHERE id = @commentID";
	}
	private static string SelectRepliesQuery() {
		return @"
			SELECT * FROM comments 
			WHERE id IN (
				SELECT commentID FROM replies 
				WHERE thread = @commentID
			) OR
			id IN (
				SELECT commentID FROM replies 
				WHERE replyTo = @commentID
			);
		";
	}
	private static string InsertReplyQuery() {
		return @"
			INSERT INTO replies 
			(thread, replyTo, commentID)
			VALUES (@thread, @replyTo, @commentID);
		";
	}
	private static string CountLikesQuery() {
		return "SELECT COUNT(*) FROM likestates WHERE commentID = @commentID AND liked = 1";
	}
	private static string CountDislikesQuery() {
		return "SELECT COUNT(*) FROM likestates WHERE commentID = @commentID AND disliked = 1";
	}
	private static string SelectLikeStateQuery() {
		return "SELECT * FROM likestates WHERE commentID = @commentID AND userID = @userID";
	}
	private static string DeleteLikeStateQuery() {
		return "DELETE FROM likestates WHERE commentID = @commentID AND userID = @userID";
	}
	private static string DeleteLikeStatesQuery() {
		return "DELETE FROM likestates WHERE commentID = @commentID";
	}
	private static string UpdateDislikeQuery() {
		return "UPDATE likestates SET disliked = 1, liked = 0 WHERE commentID = @commentID AND userID = @userID";
	}
	private static string UpdateLikeQuery() {
		return "UPDATE likestates SET liked = 1, disliked = 0 WHERE commentID = @commentID AND userID = @userID";
	}
	private static string InsertLikeQuery() {
		return "INSERT INTO likestates (liked, disliked, commentID, userID) VALUES (1, 0, @commentID, @userID)";
	}
	private static string InsertDislikeQuery() {
		return "INSERT INTO likestates (liked, disliked, commentID, userID) VALUES (0, 1, @commentID, @userID)";
	}
	private static string DeleteCommentQuery() {
		return "UPDATE comments SET content = 'This comment has been deleted' WHERE id = @commentID AND userID = @userID;";
	}
	private static string InsertCommentQuery() {
		string dtm = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		return @$"
			INSERT INTO comments (content, createdAt, facilityID, userID) 
			VALUES (@content, '{dtm}', @facilityID, @userID);
			SELECT LAST_INSERT_ID();
		";
	}
	private static string SelectCommentsQuery() {
		return @"
			SELECT * FROM comments 
			WHERE facilityID = @facilityID
			AND id NOT IN (
				SELECT commentID FROM replies
			)
		";
	}

	#endregion
}
