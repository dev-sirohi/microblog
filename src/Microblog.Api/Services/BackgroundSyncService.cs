using static Microblog.Api.Utils.AppConstants;

namespace Microblog.Api.Services.BackgroundProcesses;

/*
 * For security - Only BackgroundSyncService should generate this token and it is required to run SyncWorkers -
 * If there's any error in SyncWorker and any other utility uses it without proper exception handling, the entire app will crash
 */
internal sealed class BackgroundSyncService : BackgroundService
{
    private static readonly Guid _tokenSecret = Guid.NewGuid();
    private readonly IDatabase _inMemoryDb;
    private readonly ILogger<BackgroundSyncService> _logger;
    private readonly TimeSpan _maxBatchDelay = TimeSpan.FromSeconds(2);
    private readonly Dictionary<InMemoryOperationType, SemaphoreSlim> _operationLocks = new();
    private readonly TimeSpan _pollDelay = TimeSpan.FromSeconds(1);
    private readonly IServiceProvider _serviceProvider;
    private readonly ISubscriber _subscriber;
    private readonly BackgroundSyncToken _token = BackgroundSyncToken.Create(_tokenSecret);
    private IAuthService? _authService;
    private long _batchSize = 1000;
    private ICommentService? _commentService;
    private AppDbContext? _dbContext;
    private IPostService? _postService;
    private IUserFollowService? _userFollowService;
    private IUserLikeService? _userLikeService;
    private IUserService? _userService;

    public BackgroundSyncService(IConnectionMultiplexer _connectionMultiplexer, ILogger<BackgroundSyncService> logger,
        IServiceProvider serviceProvider)
    {
        _inMemoryDb = _connectionMultiplexer.GetDatabase();
        _subscriber = _connectionMultiplexer.GetSubscriber();
        _logger = logger;
        _serviceProvider = serviceProvider;

        _operationLocks = Enum.GetValues<InMemoryOperationType>()
            .ToDictionary(x => x, y => new SemaphoreSlim(1, 1));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackgroundSyncService started");

        try
        {
            await _subscriber.SubscribeAsync(
                InMemoryUtils.ConvertToInMemoryChannel(InMemoryPubSubChannels.FlushAndClearQueueOverflow),
                async (ch, val) => await BeginFlush(ch, val));

            while (!cancellationToken.IsCancellationRequested)
            {
                await BeginFlush();
                await Task.Delay(_pollDelay, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background sync services crashed");
        }
    }

    private async Task BeginFlush(RedisChannel? channel = null, RedisValue? value = null)
    {
        using var scope = _serviceProvider.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _userLikeService = scope.ServiceProvider.GetService<IUserLikeService>();
        bool clearQueueOverflow = false;

        List<InMemoryOperationType> operationsToRun = [];
        if (value.HasValue)
        {
            clearQueueOverflow = true;
            if (Enum.TryParse(Convert.ToString(value), out InMemoryOperationType operationType))
                operationsToRun.Add(operationType);
        }
        else
        {
            operationsToRun.AddRange(Enum.GetValues<InMemoryOperationType>().ToList());
        }

        var task = operationsToRun.Select(op => RunOperationAsync(op, clearQueueOverflow));
        await Task.WhenAll(task);
    }

    private async Task RunOperationAsync(InMemoryOperationType operationType, bool clearQueueOverflow = false)
    {
        var semaphore = _operationLocks[operationType];
        if (!await semaphore.WaitAsync(0))
        {
            _logger.LogInformation("Skipping concurrent run of {ToString}", Convert.ToString(operationType));
            return;
        }

        try
        {
            _batchSize = CacheConfigDict[operationType].CacheTrimBatch;

            if (_batchSize <= 0) _batchSize = 1000;

            switch (operationType)
            {
                case InMemoryOperationType.LIKE_EVENT_FOR_BACKGROUND_SYNC_QUEUE_ADD:
                    var syncWorkerService = new SyncWorkerService(_token, _tokenSecret, _inMemoryDb, _dbContext!,
                        _batchSize, userLikeService: _userLikeService);
                    await syncWorkerService.SyncPostLikes();
                    break;
                case InMemoryOperationType.USER_FOLLOWING:
                    break;
                case InMemoryOperationType.USER_FOLLOWED_BY:
                    break;
                case InMemoryOperationType.USER_FOLLOWING_COUNT:
                    break;
                case InMemoryOperationType.USER_FOLLOWER_COUNT:
                    break;
                case InMemoryOperationType.POST_LIKES_INCREASED_BY_USER_ID:
                    break;
                case InMemoryOperationType.UNLIKE_POST:
                    break;
                case InMemoryOperationType.LIKE_EVENT_QUEUE_REMOVE:
                    break;
                case InMemoryOperationType.ADD_MEDIA_FILE_PATH:
                    break;
                case InMemoryOperationType.USER_RECENTLY_LIKED_POST:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background operation {ToString}", Convert.ToString(operationType));
        }
        finally
        {
            try
            {
                if (clearQueueOverflow) await new InMemoryUtils(_inMemoryDb).ClearQueueOverflow(operationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in clearing queue for {InMemoryOperationType}", operationType);
            }

            semaphore.Release();
        }
    }

    internal sealed class BackgroundSyncToken
    {
        private BackgroundSyncToken(Guid secret)
        {
            Secret = secret;
        }

        internal Guid Secret { get; }

        internal static BackgroundSyncToken Create(Guid secret)
        {
            return new BackgroundSyncToken(secret);
        }
    }
}