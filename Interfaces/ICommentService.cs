using System.Data;
using Server.Models;

namespace Server.Interfaces;

public interface ICommentService {
	Comment[] GetComments(int facilityID, int userID);
	int AddComment(Comment comment, IDbTransaction? transaction = null);
	void DeleteComment(int commentID, int userID);
	bool LikeComment(int commentID, int userID);
	bool DislikeComment(int commentID, int userID);
	LikeInfo GetLikeInfo(int commentID, int userID);
	void AddReply(Reply reply);
	Reply[] GetReplies(int commentID);
}
