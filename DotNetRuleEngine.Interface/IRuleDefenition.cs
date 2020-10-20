using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetRuleEngine.Interface
{
    public interface IRuleDefenition
    {
        object Rule { get; set; }
        object Model { get; set; }
    }
}
