using Conference.Domain.Services;
using StackExchange.Redis;

namespace Conference.Service.Implementation;

internal sealed class RedisConferenceStateStore(IConnectionMultiplexer redis) : IConferenceStateStore
{
    private static readonly TimeSpan StateTtl = TimeSpan.FromHours(1);
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task AddConnectionAsync(Guid conferenceId, Guid userId, string connectionId)
    {
        var key = GetConnectionsKey(conferenceId);
        var transaction = _database.CreateTransaction();
        _ = transaction.HashSetAsync(key, connectionId, userId.ToString());
        _ = transaction.KeyExpireAsync(key, StateTtl);

        if (!await transaction.ExecuteAsync())
        {
            throw new InvalidOperationException("Failed to update conference presence state.");
        }
    }

    public async Task<bool> RemoveConnectionAsync(Guid conferenceId, Guid userId, string connectionId)
    {
        const string script = """
            local removed = redis.call('HDEL', KEYS[1], ARGV[1])
            if removed == 0 then
                return 1
            end

            local users = redis.call('HVALS', KEYS[1])
            for _, currentUser in ipairs(users) do
                if currentUser == ARGV[2] then
                    return 0
                end
            end

            return 1
            """;

        var result = await _database.ScriptEvaluateAsync(
            script,
            [GetConnectionsKey(conferenceId)],
            [connectionId, userId.ToString()]);

        return (int)result == 1;
    }

    public Task SetCurrentTimeAsync(Guid conferenceId, double time)
    {
        ValidateTime(time);
        return _database.StringSetAsync(GetCurrentTimeKey(conferenceId), time, StateTtl);
    }

    public async Task SetCurrentTimeIfGreaterAsync(Guid conferenceId, double time)
    {
        ValidateTime(time);
        const string script = """
            local current = tonumber(redis.call('GET', KEYS[1]) or '0')
            local candidate = tonumber(ARGV[1])
            if candidate > current then
                redis.call('SET', KEYS[1], ARGV[1], 'EX', ARGV[2])
            else
                redis.call('EXPIRE', KEYS[1], ARGV[2])
            end
            return 1
            """;

        await _database.ScriptEvaluateAsync(
            script,
            [GetCurrentTimeKey(conferenceId)],
            [time, (long)StateTtl.TotalSeconds]);
    }

    private static void ValidateTime(double time)
    {
        if (!double.IsFinite(time) || time < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(time));
        }
    }

    private static RedisKey GetConnectionsKey(Guid conferenceId) => $"Conference:Connections:{conferenceId}";
    private static RedisKey GetCurrentTimeKey(Guid conferenceId) => $"Conference:CurrentTime:{conferenceId}";
}
