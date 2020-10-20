namespace DotNetRuleEngine.Interface
{
    public interface IRule<T> : IRuleGeneral
    {
        new T Model { get; set; }

        void AddRule(IRule<T> rule);

        void AddRule<TK>() where TK : IRule<T>;
    }
}
