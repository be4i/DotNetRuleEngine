using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetRuleEngine.Interface;

namespace DotNetRuleEngine.Services
{
    internal sealed class AsyncRuleService
    {
        private readonly IEnumerable<IRuleAsyncGeneral> _rules;
        private readonly IRuleEngineConfiguration _ruleEngineConfiguration;
        private readonly CancellationToken _cancellationToken;
        private readonly RxRuleService<IRuleAsyncGeneral> _rxRuleService;
        private readonly ConcurrentBag<IRuleResult> _asyncRuleResults = new ConcurrentBag<IRuleResult>();
        private readonly ConcurrentBag<Task<IRuleResult>> _parallelRuleResults = new ConcurrentBag<Task<IRuleResult>>();

        public AsyncRuleService(
            IEnumerable<IRuleAsyncGeneral> rules,
            IRuleEngineConfiguration ruleEngineTerminated,
            CancellationToken cancellationToken)
        {
            _rules = rules;
            _rxRuleService = new RxRuleService<IRuleAsyncGeneral>(_rules);
            _ruleEngineConfiguration = ruleEngineTerminated;
            _cancellationToken = cancellationToken;
        }

        public async Task InvokeAsync()
        {
            await ExecuteAsyncRules(_rxRuleService.FilterRxRules(_rules));
        }

        public async Task<IEnumerable<IRuleResult>> GetAsyncRuleResultsAsync()
        {
            await Task.WhenAll(_parallelRuleResults);

            Parallel.ForEach(_parallelRuleResults, rule =>
            {
                rule.Result.AssignRuleName(rule.GetType().Name);
                AddToAsyncRuleResults(rule.Result);
            });

            return _asyncRuleResults;
        }

        private async Task ExecuteAsyncRules(IEnumerable<IRuleAsyncGeneral> rules)
        {
            await ExecuteParallelRules(rules);

            foreach (var rule in OrderByExecutionOrder(rules))
            {
                await InvokeNestedRulesAsync(rule.Configuration.InvokeNestedRulesFirst, false, rule);

                if (rule.CanInvoke() && !_ruleEngineConfiguration.IsRuleEngineTerminated())
                {
                    await InvokeNestedRulesAsync(rule.Configuration.InvokeNestedRulesFirst, true, rule);

                    try
                    {
                        await InvokeProactiveRulesAsync(rule);

                        AddToAsyncRuleResults(await ExecuteRuleAsync(rule, _cancellationToken));
                        rule.UpdateRuleEngineConfiguration(_ruleEngineConfiguration);

                        await InvokeReactiveRulesAsync(rule);
                    }
                    catch (Exception exception)
                    {
                        rule.UnhandledException = exception;

                        if (_rxRuleService.GetExceptionRules().ContainsKey(rule.GetType()))
                        {
                            await InvokeExceptionRulesAsync(rule);
                        }
                        else
                        {
                            var globalExceptionHandler = _rules.GetGlobalExceptionHandler();

                            if (globalExceptionHandler is IRuleAsyncGeneral ruleAsync)
                            {
                                globalExceptionHandler.UnhandledException = exception;
                                await ExecuteAsyncRules(new List<IRuleAsyncGeneral> { ruleAsync });
                            }
                            else
                            {
                                throw;
                            }                            
                        }
                    }

                    await InvokeNestedRulesAsync(!rule.Configuration.InvokeNestedRulesFirst, true, rule);
                }

                await InvokeNestedRulesAsync(!rule.Configuration.InvokeNestedRulesFirst, false, rule);
            }
        }

        private async Task ExecuteParallelRules(IEnumerable<IRuleAsyncGeneral> rules)
        {
            foreach (var rule in GetParallelRules(rules))
            {
                await InvokeNestedRulesAsync(rule.Configuration.InvokeNestedRulesFirst, false, rule);

                if (rule.CanInvoke() && !_ruleEngineConfiguration.IsRuleEngineTerminated())
                {
                    await InvokeNestedRulesAsync(rule.Configuration.InvokeNestedRulesFirst, true, rule);

                    await InvokeProactiveRulesAsync(rule);

                    _parallelRuleResults.Add(await Task.Factory.StartNew(async () =>
                    {
                        IRuleResult ruleResult = null;

                        try
                        {
                            ruleResult = await ExecuteRuleAsync(rule, _cancellationToken);
                        }
                        catch (Exception exception)
                        {
                            rule.UnhandledException = exception;
                            if (_rxRuleService.GetExceptionRules().ContainsKey(rule.GetType()))
                            {
                                await InvokeExceptionRulesAsync(rule);
                            }
                            else
                            {
                                var globalExceptionHandler = _rules.GetGlobalExceptionHandler();

                                if (globalExceptionHandler is IRuleAsyncGeneral ruleAsync)
                                {
                                    globalExceptionHandler.UnhandledException = exception;
                                    await ExecuteAsyncRules(new List<IRuleAsyncGeneral> { ruleAsync });
                                }
                                else
                                {
                                    throw;
                                }                                
                            }
                        }

                        return ruleResult;

                    }, rule.ParallelConfiguration.CancellationTokenSource?.Token ?? CancellationToken.None,
                        rule.ParallelConfiguration.TaskCreationOptions,
                        rule.ParallelConfiguration.TaskScheduler));

                    await InvokeReactiveRulesAsync(rule);

                    await InvokeNestedRulesAsync(!rule.Configuration.InvokeNestedRulesFirst, true, rule);
                }

                await InvokeNestedRulesAsync(!rule.Configuration.InvokeNestedRulesFirst, false, rule);
            }
        }

        private static async Task<IRuleResult> ExecuteRuleAsync(IRuleAsyncGeneral rule, CancellationToken cancellationToken)
        {
            await rule.BeforeInvokeAsync(cancellationToken);

            if (rule.IsParallel && rule.ParallelConfiguration.CancellationTokenSource != null &&
                rule.ParallelConfiguration.CancellationTokenSource.Token.IsCancellationRequested)
            {
                return null;
            }

            var ruleResult = await rule.InvokeAsync(cancellationToken);

            await rule.AfterInvokeAsync(cancellationToken);

            ruleResult.AssignRuleName(rule.GetType().Name);

            return ruleResult;
        }

        private async Task InvokeReactiveRulesAsync(IRuleAsyncGeneral asyncRule)
        {
            if (_rxRuleService.GetReactiveRules().ContainsKey(asyncRule.GetType()))
            {
                await ExecuteAsyncRules(_rxRuleService.GetReactiveRules()[asyncRule.GetType()]);
            }
        }

        private async Task InvokeProactiveRulesAsync(IRuleAsyncGeneral asyncRule)
        {
            if (_rxRuleService.GetProactiveRules().ContainsKey(asyncRule.GetType()))
            {
                await ExecuteAsyncRules(_rxRuleService.GetProactiveRules()[asyncRule.GetType()]);
            }
        }

        private async Task InvokeExceptionRulesAsync(IRuleAsyncGeneral asyncRule)
        {
            var exceptionRules =
                _rxRuleService.GetExceptionRules()[asyncRule.GetType()]
                    .Select(rule =>
                    {
                        rule.UnhandledException = asyncRule.UnhandledException;
                        return rule;
                    }).ToList();

            await ExecuteAsyncRules(exceptionRules);
        }

        private async Task InvokeNestedRulesAsync(bool invokeNestedRules, bool invoked, IRuleAsyncGeneral rule)
        {
            if (invokeNestedRules && rule.IsNested)
            {
                await ExecuteAsyncRules(
                    _rxRuleService.FilterRxRules(
                        rule.GetRules()
                        .Select(x => x.Rule)
                        .OfType<IRuleAsyncGeneral>()
                        .Where(x => x.Configuration.InvokeOnlyIfParent = invoked)
                        .ToList()
                    )
                );
            }
        }

        private void AddToAsyncRuleResults(IRuleResult ruleResult)
        {
            if (ruleResult != null) _asyncRuleResults.Add(ruleResult);
        }

        private static IEnumerable<IRuleAsyncGeneral> OrderByExecutionOrder(IEnumerable<IRuleAsyncGeneral> rules)
        {
            return rules.GetRulesWithExecutionOrder().OfType<IRuleAsyncGeneral>()
                    .Concat(rules.GetRulesWithoutExecutionOrder(rule => !((IRuleAsyncGeneral)rule).IsParallel).OfType<IRuleAsyncGeneral>());
        }

        private static IEnumerable<IRuleAsyncGeneral> GetParallelRules(IEnumerable<IRuleAsyncGeneral> rules)
        {
            return rules.Where(r => r.IsParallel && !r.Configuration.ExecutionOrder.HasValue)
                .AsParallel();
        }
    }
}