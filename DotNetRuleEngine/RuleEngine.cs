using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetRuleEngine.Exceptions;
using DotNetRuleEngine.Interface;
using DotNetRuleEngine.Models;
using DotNetRuleEngine.Services;

namespace DotNetRuleEngine
{
    /// <summary>
    /// Rule Engine.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class RuleEngine
    {
        private IDependencyResolver _dependencyResolver;
        private RuleService _ruleService;
        private AsyncRuleService _asyncRuleService;
        private readonly List<IRuleDefenition> _rules = new List<IRuleDefenition>();
        private readonly Guid _ruleEngineId = Guid.NewGuid();
        private readonly RuleEngineConfiguration _ruleEngineConfiguration =
            new RuleEngineConfiguration(new Configuration());

        /// <summary>
        /// Rule engine ctor.
        /// </summary>
        private RuleEngine() { }

        /// <summary>
        /// Set dependency resolver
        /// </summary>
        /// <param name="dependencyResolver"></param>
        public void SetDependencyResolver(IDependencyResolver dependencyResolver) => _dependencyResolver = dependencyResolver;

        /// <summary>
        /// Get a new instance of RuleEngine
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="dependencyResolver"></param>
        /// <returns></returns>
        public static RuleEngine GetInstance(IDependencyResolver dependencyResolver = null) =>
            new RuleEngine
            {
                _dependencyResolver = dependencyResolver
            };

        /// <summary>
        /// Used to add rules to rule engine.
        /// </summary>
        /// <param name="rules">Rule(s) list.</param>
        public void AddRules(params IRuleDefenition[] rules) => _rules.AddRange(rules);

        /// <summary>
        /// Used to add rule to rule engine.
        /// </summary>
        /// <param name="rule">Rule(s) list.</param>
        public void AddRule(IRuleDefenition rule) => _rules.Add(rule);

        /// <summary>
        /// Used to add rule to rule engine.
        /// </summary>
        public void AddRule<TK>(object model) where TK: IGeneralRule => _rules.Add(new RuleDefenition(typeof(TK), model));


        /// <summary>
        /// Used to execute async rules.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<IRuleResult>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (!_rules.Any()) return Enumerable.Empty<IRuleResult>().ToArray();

            CancellationTokenSource source = null;

            if(cancellationToken == default)
            {
                source = new CancellationTokenSource();
                cancellationToken = source.Token;
            }

            try
            {
                var rules = await new BootstrapService(_ruleEngineId, _dependencyResolver)
                    .BootstrapAsync(_rules, cancellationToken);

                _asyncRuleService = new AsyncRuleService(rules, _ruleEngineConfiguration, cancellationToken);

                await _asyncRuleService.InvokeAsync();

                return await _asyncRuleService.GetAsyncRuleResultsAsync();
            }
            finally
            {
                source?.Dispose();
            }
        }

        /// <summary>
        /// Used to execute rules.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IRuleResult> Execute()
        {
            if (!_rules.Any()) return Enumerable.Empty<IRuleResult>().ToArray();

            var rules = new BootstrapService(_ruleEngineId, _dependencyResolver)
                .Bootstrap(_rules);

            _ruleService = new RuleService(rules, _ruleEngineConfiguration);

            _ruleService.Invoke();

            return _ruleService.GetRuleResults();
        }
    }
}