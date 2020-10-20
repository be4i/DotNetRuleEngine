﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetRuleEngine.Interface;
using DotNetRuleEngine.Models;

namespace DotNetRuleEngine
{
    public static class Extensions
    {
        public static T To<T>(this object @object) => @object != null ? (T)@object : default(T);

        public static T To<T>(this Task<object> @object) => @object != null ? (T)@object.Result : default(T);

        public static Guid GetRuleEngineId(this IGeneralRule rule) =>
            rule.Configuration.To<RuleEngineConfiguration>().RuleEngineId;

        public static string GetRuleName<T>(this IGeneralRule rule) =>
            rule.GetType().Name;

        public static IRuleResult FindRuleResult<T>(this IEnumerable<IRuleResult> ruleResults) =>
            ruleResults.FirstOrDefault(r => string.Equals(r.Name, typeof(T).Name, StringComparison.OrdinalIgnoreCase));

        public static IEnumerable<IRuleResult> FindRuleResults<T>(this IEnumerable<IRuleResult> ruleResults) =>
            ruleResults.Where(r => string.Equals(r.Name, typeof(T).Name, StringComparison.OrdinalIgnoreCase));

        public static IRuleResult FindRuleResult(this IEnumerable<IRuleResult> ruleResults, string ruleName) =>
            ruleResults.FirstOrDefault(r => string.Equals(r.Name, ruleName, StringComparison.OrdinalIgnoreCase));

        public static IEnumerable<IRuleResult> FindRuleResults(this IEnumerable<IRuleResult> ruleResults, string ruleName) =>
            ruleResults.Where(r => string.Equals(r.Name, ruleName, StringComparison.OrdinalIgnoreCase));

        public static RuleEngine ApplyRules(this RuleEngine ruleEngineExecutor,
            params IRuleDefenition[] rules)
        {
            ruleEngineExecutor.AddRules(rules);

            return ruleEngineExecutor;
        }

        public static IEnumerable<IRuleResult> GetErrors(this IEnumerable<IRuleResult> ruleResults)
            => ruleResults.Where(r => r.Error != null);

        public static bool AnyError(this IEnumerable<IRuleResult> ruleResults) => ruleResults.Any(r => r.Error != null);

        public static RuleType GetRuleType<T>(this IGeneralRule rule)
        {
            if (rule.IsProactive) return RuleType.ProActiveRule;
            if (rule.IsReactive) return RuleType.ReActiveRule;
            if (rule.IsExceptionHandler) return RuleType.ExceptionHandlerRule;

            return RuleType.None;
        }
    }

    public enum RuleType
    {
        None,
        ProActiveRule,
        ReActiveRule,
        ExceptionHandlerRule
    }
}
