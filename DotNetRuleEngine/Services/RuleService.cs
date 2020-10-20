using System;
using System.Collections.Generic;
using System.Linq;
using DotNetRuleEngine.Interface;

namespace DotNetRuleEngine.Services
{
    internal class RuleService
    {
        private readonly IEnumerable<IRuleGeneral> _rules;
        private readonly IRuleEngineConfiguration _ruleEngineConfiguration;
        private readonly RxRuleService<IRuleGeneral> _rxRuleService;
        private readonly ICollection<IRuleResult> _ruleResults = new List<IRuleResult>();

        public RuleService(IEnumerable<IRuleGeneral> rules,
            IRuleEngineConfiguration ruleEngineConfiguration)
        {
            _rules = rules;
            _rxRuleService = new RxRuleService<IRuleGeneral>(_rules);
            _ruleEngineConfiguration = ruleEngineConfiguration;
        }

        public void Invoke() => Execute(_rxRuleService.FilterRxRules(_rules));

        public IEnumerable<IRuleResult> GetRuleResults() => _ruleResults;

        private void Execute(IEnumerable<IRuleGeneral> rules)
        {
            foreach (var rule in OrderByExecutionOrder(rules))
            {
                InvokeNestedRules(rule.Configuration.InvokeNestedRulesFirst, false, rule);

                if (rule.CanInvoke() && !_ruleEngineConfiguration.IsRuleEngineTerminated())
                {
                    InvokeNestedRules(rule.Configuration.InvokeNestedRulesFirst, true, rule);

                    InvokeProactiveRules(rule);

                    try
                    {
                        rule.BeforeInvoke();
                        var ruleResult = rule.Invoke();
                        rule.AfterInvoke();

                        AddToRuleResults(ruleResult, rule.GetType().Name);
                    }

                    catch (Exception exception)
                    {
                        rule.UnhandledException = exception;
                        if (_rxRuleService.GetExceptionRules().ContainsKey(rule.GetType()))
                        {
                            InvokeExceptionRules(rule);
                        }
                        else
                        {
                            var globalExceptionHandler = _rules.GetGlobalExceptionHandler();

                            if (globalExceptionHandler is IRuleGeneral)
                            {
                                globalExceptionHandler.UnhandledException = exception;
                                Execute(new List<IRuleGeneral> { (IRuleGeneral)globalExceptionHandler });
                            }
                            else
                            {
                                throw;
                            }                            
                        }
                    }

                    rule.UpdateRuleEngineConfiguration(_ruleEngineConfiguration);

                    InvokeReactiveRules(rule);

                    InvokeNestedRules(!rule.Configuration.InvokeNestedRulesFirst, true, rule);
                }

                InvokeNestedRules(!rule.Configuration.InvokeNestedRulesFirst, false, rule);
            }
        }

        private void InvokeReactiveRules(IGeneralRule rule)
        {
            if (_rxRuleService.GetReactiveRules().ContainsKey(rule.GetType()))
            {
                Execute(_rxRuleService.GetReactiveRules()[rule.GetType()]);
            }
        }

        private void InvokeProactiveRules(IRuleGeneral rule)
        {
            if (_rxRuleService.GetProactiveRules().ContainsKey(rule.GetType()))
            {
                Execute(_rxRuleService.GetProactiveRules()[rule.GetType()]);
            }
        }

        private void InvokeExceptionRules(IRuleGeneral rule)
        {
            var exceptionRules = _rxRuleService.GetExceptionRules()[rule.GetType()]
                .Select(r =>
                {
                    r.UnhandledException = rule.UnhandledException;
                    return r;
                });

            Execute(exceptionRules);
        }

        private void AddToRuleResults(IRuleResult ruleResult, string ruleName)
        {
            ruleResult.AssignRuleName(ruleName);
            if (ruleResult != null) _ruleResults.Add(ruleResult);
        }

        private void InvokeNestedRules(bool invokeNestedRules, bool invoked, IRuleGeneral rule)
        {
            if (invokeNestedRules && rule.IsNested)
            {
                Execute(
                    _rxRuleService.FilterRxRules(
                        OrderByExecutionOrder(
                            rule.GetRules()
                            .Select(x => x.Rule)
                            .OfType<IRuleGeneral>()
                            .Where(x => x.Configuration.InvokeOnlyIfParent == invoked)
                            .ToList()
                        )
                    )
                );
            }
        }

        private static IEnumerable<IRuleGeneral> OrderByExecutionOrder(IEnumerable<IRuleGeneral> rules)
        {
            return rules.GetRulesWithExecutionOrder().OfType<IRuleGeneral>()
                .Concat(rules.GetRulesWithoutExecutionOrder().OfType<IRuleGeneral>());
        }
    }
}
