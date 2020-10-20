using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetRuleEngine.Interface
{
    public interface IRuleConcrateModel<T> : IGeneralRule
    {
        new T Model { get; set; }
    }
}
