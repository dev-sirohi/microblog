using static Microblog.Api.Utils.AppConstants;

namespace Microblog.Api.Services.BackgroundProcesses
{
    /* For security - Only BackgrounSyncService should generate this token and it is required to run SyncWorkers - If there's any error in SyncWorker and any other utility uses it without proper exception handling, the entire app will crash */
    internal sealed class BackgroundSyncService : BackgroundService
    {
        internal sealed class BackgroundSyncToken
        {
            internal Guid Secret { get; }
            private BackgroundSyncToken(Guid secret)
            {
                Secret = secret;
            }
            internal static BackgroundSyncToken Create(Guid secret) => new BackgroundSyncToken(secret);
        }
        private static readonly Guid _tokenSecret = Guid.NewGuid();
        private readonly BackgroundSyncToken _token = BackgroundSyncToken.Create(_tokenSecret);
        private readonly IDatabase _inMemoryDb;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISubscriber _subscriber;
        private readonly ILogger<BackgroundSyncService> _logger;
        private readonly TimeSpan _pollDelay = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _maxBatchDelay = TimeSpan.FromSeconds(2);
        private Dictionary<InMemoryOperationType, SemaphoreSlim> _operationLocks = new Dictionary<InMemoryOperationType, SemaphoreSlim>();
        private long _batchSize = 1000;
        private AppDbContext? _dbContext;
        private IUserService? _userService;
        private IUserLikeService? _userLikeService;
        private IPostService? _postService;
        private ICommentService? _commentService;
        private IAuthService? _authService;
        private IUserFollowService? _userFollowService;

        public BackgroundSyncService(IConnectionMultiplexer _connectionMultiplexer, ILogger<BackgroundSyncService> logger, IServiceProvider serviceProvider)
        {
            _inMemoryDb = _connectionMultiplexer.GetDatabase();
            _subscriber = _connectionMultiplexer.GetSubscriber();
            _logger = logger;
            _serviceProvider = serviceProvider;

            _operationLocks = Enum.GetValues(typeof(AppConstants.InMemoryOperationType))
                .Cast<AppConstants.InMemoryOperationType>()
                .ToDictionary(x => x, y => new SemaphoreSlim(1, 1));
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BackgroundSyncService started");

            try
            {
                await _subscriber.SubscribeAsync(InMemoryUtils.ConvertToInMemoryChannel(AppConstants.InMemoryPubSubChannels.FlushAndClearQueueOverflow), async (ch, val) => await BeginFlush(ch, val));

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
            using (var scope = _serviceProvider.CreateScope())
            {
                _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                _userLikeService = scope.ServiceProvider.GetService<IUserLikeService>();
                bool clearQueueOverflow = false;

                List<InMemoryOperationType> operationsToRun = new List<InMemoryOperationType>();
                if (value.HasValue)
                {
                    clearQueueOverflow = true;
                    if (Enum.TryParse(Convert.ToString(value), out InMemoryOperationType operationType))
                    {
                        operationsToRun.Add(operationType);
                    }
                }
                else
                {
                    operationsToRun.AddRange(Enum.GetValues(typeof(InMemoryOperationType)).Cast<InMemoryOperationType>().ToList());
                }

                IEnumerable<Task> task = operationsToRun.Select(op => RunOperationAsync(op, clearQueueOverflow));
                await Task.WhenAll(task);
            }
        }

        private async Task RunOperationAsync(InMemoryOperationType operationType, bool clearQueueOverflow = false)
        {
            SemaphoreSlim semaphore = _operationLocks[operationType];
            if (!await semaphore.WaitAsync(0))
            {
                _logger.LogInformation($"Skipping concurrent run of {Convert.ToString(operationType)}");
                return;
            }

            try
            {
                _batchSize = AppConstants.CacheConfigDict[operationType].CacheTrimBatch;

                if (_batchSize <= 0)
                {
                    _batchSize = 1000;
                }

                switch (operationType)
                {
                    case InMemoryOperationType.LIKE_EVENT_FOR_BACKGROUND_SYNC_QUEUE_ADD:
                        SyncWorkerService syncWorkerService = new SyncWorkerService(_token, _tokenSecret ,_inMemoryDb, _dbContext!, _batchSize, userLikeService: _userLikeService);
                        await syncWorkerService.SyncPostLikes();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in background operation {Convert.ToString(operationType)}");
            }
            finally
            {
                try
                {
                    if (clearQueueOverflow)
                    {
                        await new InMemoryUtils(_inMemoryDb).ClearQueueOverflow(operationType);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in clearing queue for {operationType}");
                }

                semaphore.Release();
            }
        }
    }
}
