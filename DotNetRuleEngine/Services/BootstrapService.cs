using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetRuleEngine.Exceptions;
using DotNetRuleEngine.Interface;
using DotNetRuleEngine.Models;

namespace DotNetRuleEngine.Services
{
    internal sealed class BootstrapService
    {
        private readonly Guid _ruleEngineId;
        private readonly IDependencyResolver _dependencyResolver;

        public BootstrapService(Guid ruleEngineId, IDependencyResolver dependencyResolver)
        {
            _ruleEngineId = ruleEngineId;
            _dependencyResolver = dependencyResolver;
        }

        public IList<IRuleGeneral> Bootstrap(IList<IRuleDefenition> rules)
        {
            Initializer(rules);
            return 
                rules.Select(x => x.Rule)
                .OfType<IRuleGeneral>()
                .ToList();
        }

        public async Task<IList<IRuleAsyncGeneral>> BootstrapAsync(IList<IRuleDefenition> rules)
        {
            var initBag = new ConcurrentBag<Task>();
            InitializerAsync(rules, initBag);

            await Task.WhenAll(initBag);

            return 
                rules.Select(x => x.Rule)
                .OfType<IRuleAsyncGeneral>()
                .ToList();
        }

        private void Initializer(IList<IRuleDefenition> rules,
            IRuleGeneral nestingRule = null)
        {
            for (var i = 0; i < rules.Count; i++)
            {
                var def = rules[i];
                var rule = ResolveRule<IRuleGeneral>(def.Rule);

                def.Rule = rule;

                InitializeRule(rule, def.Model, nestingRule);

                rule.Initialize();

                if (rule.IsNested) Initializer(rule.GetRules(), rule);
            }
        }

        private void InitializerAsync(IList<IRuleDefenition> rules,
            ConcurrentBag<Task> initBag, IRuleAsyncGeneral nestingRule = null)
        {
            for (var i = 0; i < rules.Count(); i++)
            {
                var def = rules[i];
                var rule = ResolveRule<IRuleAsyncGeneral>(def.Rule);

                def.Rule = rule;

                InitializeRule(rule, def.Model, nestingRule);

                initBag.Add(rule.InitializeAsync());

                if (rule.IsNested) InitializerAsync(rule.GetRules(), initBag, rule);
            }
        }

        private void InitializeRule(IGeneralRule rule, object model, IGeneralRule nestingRule = null)
        {
            rule.Model = model;
            rule.Configuration = new RuleEngineConfiguration(rule.Configuration) { RuleEngineId = _ruleEngineId };

            if (nestingRule != null && nestingRule.Configuration.NestedRulesInheritConstraint)
            {
                rule.Configuration.Constraint = nestingRule.Configuration.Constraint;
                rule.Configuration.NestedRulesInheritConstraint = true;
            }

            if (rule is IRuleAsyncGeneral parallelRule && parallelRule.IsParallel &&
                nestingRule is IRuleAsyncGeneral nestingParallelRule)
            {
                if (nestingParallelRule.ParallelConfiguration != null &&
                    nestingParallelRule.ParallelConfiguration.NestedParallelRulesInherit)
                {
                    var cancellationTokenSource = parallelRule.ParallelConfiguration.CancellationTokenSource;
                    parallelRule.ParallelConfiguration = new ParallelConfiguration
                    {
                        NestedParallelRulesInherit = true,
                        CancellationTokenSource = cancellationTokenSource,
                        TaskCreationOptions = nestingParallelRule.ParallelConfiguration.TaskCreationOptions,
                        TaskScheduler = nestingParallelRule.ParallelConfiguration.TaskScheduler
                    };
                }
            }

            rule.Resolve = _dependencyResolver;
        }

        private TK ResolveRule<TK>(object ruleObject) where TK : class
        {
            var resolvedRule = default(TK);

            if (ruleObject is Type type)
            {
                resolvedRule = _dependencyResolver.GetService(type) as TK;

                if (resolvedRule == null) throw new UnsupportedRuleException(ruleObject.ToString());                
            }

            return (TK)(resolvedRule ?? ruleObject);
        }
    }
}