namespace GearCrossroads.Api.DTOs
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; }
        public int SetupId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsSetupOwner { get; set; }
    }

    public class CreateCommentDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public class UpdateCommentDto
    {
        public string Content { get; set; } = string.Empty;
    }
}
