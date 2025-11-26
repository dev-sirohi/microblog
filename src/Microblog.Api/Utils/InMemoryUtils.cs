namespace Microblog.Api.Utils
{
    internal sealed class InMemoryUtils
    {
        private readonly IDatabase _inMemoryDb;

        internal InMemoryUtils(IDatabase inMemoryDb)
        {
            _inMemoryDb = inMemoryDb;
        }

        internal static string GetKey(AppConstants.InMemoryOperationType operationType, object? entity1 = null, object? entity2 = null)
        {
            string key = string.Empty;

            switch (operationType)
            {
                case AppConstants.InMemoryOperationType.USER_FOLLOWING:
                    key = $"user:{Convert.ToString(entity1)}:following";
                    break;
                case AppConstants.InMemoryOperationType.USER_FOLLOWED_BY:
                    key = $"user:{Convert.ToString(entity1)}:followedby";
                    break;
                case AppConstants.InMemoryOperationType.USER_FOLLOWING_COUNT:
                    key = $"user:{Convert.ToString(entity1)}:followingCount";
                    break;
                case AppConstants.InMemoryOperationType.USER_FOLLOWER_COUNT:
                    key = $"user:{Convert.ToString(entity1)}:followerCount";
                    break;
                case AppConstants.InMemoryOperationType.LIKE_EVENT_QUEUE_REMOVE:
                case AppConstants.InMemoryOperationType.LIKE_EVENT_FOR_BACKGROUND_SYNC_QUEUE_ADD:
                    key = $"userLikes:eventsQueue";
                    break;
                case AppConstants.InMemoryOperationType.ADD_MEDIA_FILE_PATH:
                    key = $"mediaFilePath:{Convert.ToString(entity1)}:{Convert.ToString(entity2)}";
                    break;
                default:
                    throw new AppException("Invalid Operation");
            }

            return key;
        }

        internal static RedisValue ConvertToInMemoryValue(object value)
        {
            if (value.GetType() == typeof(string) || value.GetType() == typeof(long) || value.GetType() == typeof(double) || value.GetType() == typeof(int) || value.GetType() == typeof(bool) || value.GetType() == typeof(float))
            {
                return (RedisValue)value;
            }

            return CommonUtils.TransformTo<RedisValue>(value);
        }

        internal static RedisChannel ConvertToInMemoryChannel(object value)
        {
            return new RedisChannel(Convert.ToString(value) ?? throw new Exception("Invalid value conversion to Redis Channel"), RedisChannel.PatternMode.Auto);
        }

        internal async Task<List<T>> GetSetMembersAsync<T>(string key)
        {
            RedisValue[] valueList = await _inMemoryDb.SetMembersAsync(key);

            if (valueList != null)
            {
                if (typeof(T) == typeof(long))
                {
                    return valueList.Select(value => (T)(object)Convert.ToInt64(value)).ToList();
                }
                else if (typeof(T) == typeof(string))
                {
                    return valueList.Select(value => (T)(object)Convert.ToString(value)).ToList();
                }
            }

            return new List<T>();
        }

        internal static List<T> GetMembersFromValueListAs<T>(RedisValue[] valueList)
        {
            if (valueList != null)
            {
                if (typeof(T) == typeof(long))
                {
                    return valueList.Select(value => (T)(object)Convert.ToInt64(value)).ToList();
                }
                else if (typeof(T) == typeof(string))
                {
                    return valueList.Select(value => (T)(object)Convert.ToString(value)).ToList();
                }
            }

            return CommonUtils.TransformTo<List<T>>(valueList) ?? throw new AppException("Invalid conversion of value list");
        }

        internal static double GetUniqueRank(DateTime? timeEntity = null)
        {
            DateTime _timeEntity = DateTime.UtcNow;
            if (timeEntity != null)
            {
                _timeEntity = timeEntity.Value;
            }
            return _timeEntity.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMicroseconds;
        }

        internal async Task FlushAndClearQueueOverflow(AppConstants.InMemoryOperationType operationType)
        {
            await _inMemoryDb.Multiplexer
                    .GetSubscriber()
                    .PublishAsync(ConvertToInMemoryChannel(AppConstants.InMemoryPubSubChannels.FlushAndClearQueueOverflow), ConvertToInMemoryValue(operationType));
        }

        internal async Task ClearQueueOverflow(AppConstants.InMemoryOperationType operationType)
        {
            string key = string.Empty;
            AppConstants.CacheConfig cacheConfig = new AppConstants.CacheConfig();

            switch (operationType)
            {
                case AppConstants.InMemoryOperationType.LIKE_EVENT_FOR_BACKGROUND_SYNC_QUEUE_ADD:
                    key = InMemoryUtils.GetKey(operationType);
                    cacheConfig = AppConstants.CacheConfigDict[operationType];
                    break;
            }

            long count = await _inMemoryDb.SortedSetLengthAsync(key);
            if (count >= (cacheConfig.CacheMemoryLimit - cacheConfig.CacheTrimBatch))
            {
                await _inMemoryDb.SortedSetRemoveRangeByRankAsync(key, 0, cacheConfig.CacheTrimBatch - 1);
            }
        }
    }
}
