using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using DotNetRuleEngine.Interface;
using DotNetRuleEngine.Models;
using DotNetRuleEngineTimeOutException = DotNetRuleEngine.Exceptions.TimeoutException;

namespace DotNetRuleEngine.Services
{
    internal sealed class DataSharingService
    {
        private static readonly Lazy<DataSharingService> DataManager =
            new Lazy<DataSharingService>(() => new DataSharingService(), true);

        private DataSharingService()
        {
        }

        private Lazy<ConcurrentDictionary<string, Task<object>>> AsyncData { get; } =
            new Lazy<ConcurrentDictionary<string, Task<object>>>(
                () => new ConcurrentDictionary<string, Task<object>>(), true);

        private Lazy<ConcurrentDictionary<string, object>> Data { get; } =
            new Lazy<ConcurrentDictionary<string, object>>(
                () => new ConcurrentDictionary<string, object>(), true);

        internal const int DefaultTimeoutInMs = 15000;

        public async Task AddOrUpdateAsync(string key, Task<object> value, IConfiguration configuration)
        {
            var ruleEngineId = GetRuleEngineId(configuration);
            var keyPair = BuildKey(key, ruleEngineId);

            await Task.FromResult(AsyncData.Value.AddOrUpdate(keyPair.First(), v => value, (k, v) => value));
        }

        public async Task<object> GetValueAsync(string key, IConfiguration configuration,
            int timeoutInMs = DefaultTimeoutInMs)
        {
            var timeout = DateTime.Now.AddMilliseconds(timeoutInMs);
            var ruleEngineId = GetRuleEngineId(configuration);
            var keyPair = BuildKey(key, ruleEngineId);

            while (DateTime.Now < timeout)
            {
                AsyncData.Value.TryGetValue(keyPair.First(), out var value);

                if (value != null) return await value;
            }

            throw new DotNetRuleEngineTimeOutException($"Unable to get {key}");
        }

        public void AddOrUpdate(string key, object value, IConfiguration configuration)
        {
            var ruleEngineId = GetRuleEngineId(configuration);
            var keyPair = BuildKey(key, ruleEngineId);

            Data.Value.AddOrUpdate(keyPair.First(), v => value, (k, v) => value);
        }

        public object GetValue(string key, IConfiguration configuration, int timeoutInMs = DefaultTimeoutInMs)
        {
            var timeout = DateTime.Now.AddMilliseconds(timeoutInMs);
            var ruleEngineId = GetRuleEngineId(configuration);
            var keyPair = BuildKey(key, ruleEngineId);

            while (DateTime.Now < timeout)
            {
                Data.Value.TryGetValue(keyPair.First(), out var value);

                if (value != null) return value;
            }

            throw new DotNetRuleEngineTimeOutException($"Unable to get {key}");
        }

        public static DataSharingService GetInstance() => DataManager.Value;

        private static string[] BuildKey(string key, string ruleEngineId) =>
            new[] {string.Join("_", ruleEngineId, key), key};

        private static string GetRuleEngineId(IConfiguration configuration) =>
            ((RuleEngineConfiguration) configuration).RuleEngineId.ToString();
    }
}