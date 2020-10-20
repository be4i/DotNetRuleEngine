namespace DotNetRuleEngine.Interface
{
    public interface IRule<T> : IRuleGeneral, IRuleConcrateModel<T>
    {
        void AddRule(IRule<T> rule);

        void AddRule<TK>() where TK : IRule<T>;
    }
}
