using System;
using System.Collections.Generic;
using System.Linq;
using DotNetRuleEngine.Interface;
using DotNetRuleEngine.Models;
using DotNetRuleEngine.Services;

namespace DotNetRuleEngine
{
    public abstract class Rule<T> : IRule<T>
    {
        private IList<IRuleDefenition> Rules { get; } = new List<IRuleDefenition>();

        public T Model { get; set; }

        public bool IsNested => Rules.Any();

        public bool IsReactive { get; set; }

        public bool IsProactive { get; set; }

        public void ObserveRule<TK>() where TK: IRuleGeneral => ObservedRule = typeof(TK);

        public bool IsExceptionHandler { get; set; }

        public bool IsGlobalExceptionHandler { get; set; }

        public Type ObservedRule { get; private set; }

        public Exception UnhandledException { get; set; }

        public IDependencyResolver Resolve { get; set; }

        public IConfiguration Configuration { get; set; } = new Configuration();
        object IGeneralRule.Model { get => Model ; set => Model = (T)value; }

        public object TryGetValue(string key, int timeoutInMs = DataSharingService.DefaultTimeoutInMs) =>
            DataSharingService.GetInstance().GetValue(key, Configuration);

        public void TryAdd(string key, object value) =>
            DataSharingService.GetInstance().AddOrUpdate(key, value, Configuration);

        public IList<IRuleDefenition> GetRules() => Rules;

        public virtual void Initialize() { }

        public virtual void BeforeInvoke() { }

        public virtual void AfterInvoke() { }

        public abstract IRuleResult Invoke();

        public void AddRule(IRule<T> rule)
        {
            Rules.Add(new RuleDefenition(rule, Model));
        }

        public void AddRule<TK>() where TK : IRule<T>
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

        public TValue TryGetValue<TValue>(string key)
        {
            var value = TryGetValue(key);

            return (TValue)value;
        }

        public void AddRuleIfInvoke<TRule>(int? executionOrder = null)
            where TRule : IRule<T>
        {
            var rule = Resolve.GetService<TRule>();

            this.AddRuleIfInvoke(rule, Model, executionOrder);
        }

        public void AddRule<TRule>(int? executionOrder)
            where TRule : IRule<T>
        {
            this.AddRule<TRule, T>(Model, executionOrder);
        }
    }
}
