using System;

namespace DotNetRuleEngine.Interface
{
    public interface IRuleEngineConfiguration : IConfiguration
    {
        Guid RuleEngineId { get; set; }
    }
}