﻿using System.Threading;
using System.Threading.Tasks;
using DotNetRuleEngine.Interface;

namespace DotNetRuleEngine.Models
{
    public class ParallelConfiguration : IParallelConfiguration
    {
        public TaskCreationOptions TaskCreationOptions { get; set; } = TaskCreationOptions.None;

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public TaskScheduler TaskScheduler { get; set; } = TaskScheduler.Default;

        public bool NestedParallelRulesInherit { get; set; }
    }
}
