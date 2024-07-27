namespace Modrinth.Api.Core.Filter
{
    public class Facet
    {

        public string Key { get; set; }
        public string Value { get; set; }
        public LogicalOperator LogicalOperator { get; set; }
    }
}
