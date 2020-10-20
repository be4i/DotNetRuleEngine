using System;
using System.Collections.Generic;
using System.Linq;
using DotNetRuleEngine.Exceptions;
using DotNetRuleEngine.Interface;

namespace DotNetRuleEngine
{
    internal static class InternalExtensions
    {
        public static bool CanInvoke(this IGeneralRule rule) =>
            !rule.Configuration.Skip && rule.Configuration.Constraint.Invoke2();

        public static bool Invoke2(this Func<bool> predicate) =>
            predicate == null || predicate();


        public static void AssignRuleName(this IRuleResult ruleResult, string ruleName)
        {
            if (ruleResult != null) ruleResult.Name = ruleResult.Name ?? ruleName;
        }

        public static void Validate<T>(this T model)
        {
            if (model == null) throw new ModelInstanceNotFoundException();
        }

        public static void UpdateRuleEngineConfiguration(this IGeneralRule rule,
            IConfiguration ruleEngineConfiguration)
        {
            if (ruleEngineConfiguration.Terminate == null && rule.Configuration.Terminate == true)
            {
                ruleEngineConfiguration.Terminate = true;
            }
        }

        public static bool IsRuleEngineTerminated(this IConfiguration ruleEngineConfiguration)
            => ruleEngineConfiguration.Terminate != null && ruleEngineConfiguration.Terminate.Value;

        public static IEnumerable<IGeneralRule> GetRulesWithExecutionOrder(this IEnumerable<IGeneralRule> rules,
            Func<IGeneralRule, bool> condition = null)
        {
            condition = condition ?? (rule => true);

            return rules.Where(r => r.Configuration.ExecutionOrder.HasValue)
                .Where(condition)
                .OrderBy(r => r.Configuration.ExecutionOrder);
        }

        public static IEnumerable<IGeneralRule> GetRulesWithoutExecutionOrder(this IEnumerable<IGeneralRule> rules,
            Func<IGeneralRule, bool> condition = null)
        {
            condition = condition ?? (k => true);

            return rules.Where(r => !r.Configuration.ExecutionOrder.HasValue)
                .Where(condition);
        }

        public static IGeneralRule GetGlobalExceptionHandler(this IEnumerable<IGeneralRule> rules)
        {
            var globalExceptionHandler = rules.Where(r => r.IsGlobalExceptionHandler).ToList();

            if (globalExceptionHandler.Count > 1)
            {
                throw new GlobalHandlerException("Found multiple GlobalHandlerException. Only one can be defined.");
            }

            return globalExceptionHandler.SingleOrDefault();
        }
    }
}