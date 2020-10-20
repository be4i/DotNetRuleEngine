using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetRuleEngine.Interface;
using DotNetRuleEngine.Models;
using DotNetRuleEngine.Services;

namespace DotNetRuleEngine
{
    public abstract class RuleAsync<T> : IRuleAsync<T>
    {
        private IList<IRuleDefenition> Rules { get; } = new List<IRuleDefenition>();

        public T Model { get; set; }

        public bool IsParallel { get; set; }

        public IParallelConfiguration ParallelConfiguration { get; set; } = new ParallelConfiguration();

        public bool IsNested => Rules.Any();

        public bool IsReactive { get; set; }

        public bool IsProactive { get; set; }

        public bool IsExceptionHandler { get; set; }

        public bool IsGlobalExceptionHandler { get; set; }

        public Type ObservedRule { get; private set; }

        public Exception UnhandledException { get; set; }

        public IDependencyResolver Resolve { get; set; }

        public IConfiguration Configuration { get; set; } = new Configuration();
        object IGeneralRule.Model { get => Model; set => Model = (T)value; }

        public async Task<object> TryGetValueAsync(string key, int timeoutInMs = DataSharingService.DefaultTimeoutInMs) => 
            await DataSharingService.GetInstance().GetValueAsync(key, Configuration, timeoutInMs);

        public async Task TryAddAsync(string key, Task<object> value) => 
            await DataSharingService.GetInstance().AddOrUpdateAsync(key, value, Configuration);

        public IList<IRuleDefenition> GetRules() => Rules;

        public void ObserveRule<TK>() where TK : IRuleAsyncGeneral => ObservedRule = typeof(TK);        

        public virtual async Task InitializeAsync() => await Task.FromResult<object>(null);

        public virtual async Task BeforeInvokeAsync() => await Task.FromResult<object>(null);

        public virtual async Task AfterInvokeAsync() => await Task.FromResult<object>(null);

        public abstract Task<IRuleResult> InvokeAsync();

        public void AddRule(IRuleAsync<T> rule)
        {
            Rules.Add(new RuleDefenition(rule, Model));
        }

        public void AddRule<TK>() where TK : IRuleAsync<T>
        {
            Rules.Add(new RuleDefenition(typeof(TK), Model));
        }

        public void AddRule(IGeneralRule rule, object model)
        {
            Rules.Add(new RuleDefenition(rule, model));
        }

        public void AddRule<TK>(object model) where TK : IGeneralRule
        {
            Rules.Add(new RuleDefenition(typeof(TK), model));
        }
    }
}
