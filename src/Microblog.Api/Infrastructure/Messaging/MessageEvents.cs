namespace Microblog.Api.Infrastructure.Messaging;

public sealed record PostCreatedEvent(long PostId, long UserId, string Content, DateTime CreatedAt);

public sealed record PostLikedEvent(long PostId, long UserId, bool IsLike, DateTime OccurredAt);

public sealed record UserFollowedEvent(long FollowerId, long FolloweeId, bool IsFollow, DateTime OccurredAt);
