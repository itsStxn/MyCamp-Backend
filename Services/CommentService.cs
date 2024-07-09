using Server.Interfaces;
using Server.Models;
using Server.Utils;
using System.Data;
using Dapper;

namespace Server.Services;

public class CommentService(IDbConnection db) : ICommentService {
	private readonly IDbConnection Db = db;
	private readonly FormHelper Form = new();

	public Comment[] GetComments(int facilityID, int userID) {
		string selectQuery = SelectCommentsQuery();
		var comments = Db.Query<Comment>(selectQuery, new { facilityID }).ToArray();
		foreach (var comment in comments) {
			comment.LikeInfo = GetLikeInfo(comment.Id, userID);
			comment.Replies = GetReplies(comment.Id);
		}
		return comments;
	}
	public int AddComment(Comment comment, IDbTransaction? trans = null) {
		ValidateComment(comment);
		string insertQuery = InsertCommentQuery();
		var commentID = Db.QueryFirstOrDefault<string?>(insertQuery, comment, trans)
		?? throw new DataException("Failed to add comment");
		return int.Parse (commentID);
	}
	public void DeleteComment(int commentID, int userID) {
		if (Db.State == ConnectionState.Closed) Db.Open();
		var parameters = new { commentID, userID };
		var trans = Db.BeginTransaction();

		try {
			Db.Execute(DeleteLikeStatesQuery(), new { commentID }, trans);
			Db.Execute(DeleteReplyQuery(), parameters, trans);
			int res = Db.Execute(DeleteCommentQuery(), parameters, trans);
			if (res == 0) throw new DataException("Failed to delete comment");
			trans.Commit();
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
	public bool LikeComment(int commentID, int userID) {
		var likeState = GetLikeState(commentID, userID);
		if (likeState == null) return InsertLike(commentID, userID);
		if (likeState.Liked) return DeleteLikeState(commentID, userID);
		return UpdateLike(commentID, userID);
	}
	public bool DislikeComment(int commentID, int userID) {
		var likeState = GetLikeState(commentID, userID);
		if (likeState == null) return InsertDislike(commentID, userID);
		if (likeState.Disliked) return DeleteLikeState(commentID, userID);
		return UpdateDislike(commentID, userID);
	}
	public LikeInfo GetLikeInfo(int commentID, int userID) {
		var likeState = GetLikeState(commentID, userID)
		?? throw new KeyNotFoundException("Comment not found");
		return new() {
			Likes = CountLikes(commentID),
			Dislikes = CountDislikes(commentID),
			Liked = likeState?.Liked ?? false,
			Disliked = likeState?.Disliked ?? false
		};
	}
	public void AddReply(Reply reply) {
		ValidateReply(reply);
		if (Db.State == ConnectionState.Closed) Db.Open();
		var trans = Db.BeginTransaction();

		try {
			int commentID = AddComment(reply, trans);
			string insertQuery = InsertReplyQuery();
			var parameters = new { reply.Thread, reply.ReplyTo, commentID };
			int res = Db.Execute(insertQuery, parameters, trans);

			if (res == 0) throw new DataException("Failed to add reply");
			trans.Commit();
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
	public Reply[] GetReplies(int commentID) {
		string selectQuery = SelectRepliesQuery();
		var replies = Db.Query<Reply>(selectQuery, new { commentID });
		return replies.ToArray();
	}
	
	#region Aid Functions

	private int CountLikes(int commentID) {
		string countQuery = CountLikesQuery();
		return Db.ExecuteScalar<int>(countQuery, new { commentID });
	}
	private int CountDislikes(int commentID) {
		string countQuery = CountDislikesQuery();
		return Db.ExecuteScalar<int>(countQuery, new { commentID });
	}
	private bool InsertLike(int commentID, int userID) {
		string insertQuery = InsertLikeQuery();
		int res = Db.Execute(insertQuery, new { commentID, userID });
		return res > 0;
	}
	private bool InsertDislike(int commentID, int userID) {
		string insertQuery = InsertDislikeQuery();
		int res = Db.Execute(insertQuery, new { commentID, userID });
		return res > 0;
	}
	private bool UpdateLike(int commentID, int userID) {
		string updateQuery = UpdateLikeQuery();
		int res = Db.Execute(updateQuery, new { commentID, userID });
		return res > 0;
	}
	private bool UpdateDislike(int commentID, int userID) {
		string updateQuery = UpdateDislikeQuery();
		int res = Db.Execute(updateQuery, new { commentID, userID });
		return res > 0;
	}
	private bool DeleteLikeState(int commentID, int userID) {
		string deleteQuery = DeleteLikeStateQuery();
		int res = Db.Execute(deleteQuery, new { commentID, userID });
		return res > 0;
	}
	private LikeState? GetLikeState(int commentID, int userID) {
		string selectQuery = SelectLikeStateQuery();
		var likeState = Db.QueryFirstOrDefault<LikeState?>(selectQuery, new { commentID, userID });
		return likeState;
	}
	private void ValidateComment(Comment comment) {
		if (string.IsNullOrEmpty(comment.Content)) {
			throw new InvalidOperationException("Comment cannot be empty");
		}
		comment.Content = Form.EscapeQuotes(comment.Content);
	}
	private void ValidateReply(Reply reply) {
		ValidateComment(reply);
		_= GetComment(reply.Thread)
		?? throw new KeyNotFoundException("Author not found");
		_= GetComment(reply.ReplyTo)
		?? throw new KeyNotFoundException("ReplyTo not found");
	}
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
				SELECT thread FROM replies 
				WHERE thread = @commentID
			)
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
		return "DELETE FROM comments WHERE id = @commentID AND userID = @userID";
	}
	private static string DeleteReplyQuery() {
		return @"
			DELETE FROM replies 
			WHERE commentID IN (
				SELECT id FROM comments 
				WHERE id = @commentID
				AND userID = @userID
			)
		";
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
