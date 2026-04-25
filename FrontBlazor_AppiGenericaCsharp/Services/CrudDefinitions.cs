namespace FrontBlazor_AppiGenericaCsharp.Services
{
    public enum FieldType
    {
        Text,
        Integer,
        Long,
        Decimal,
        Date,
        Boolean
    }

    public sealed class CrudFieldDefinition
    {
        public string Name { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public FieldType Type { get; init; } = FieldType.Text;
        public bool Required { get; init; }
        public bool IsForeignKey { get; init; }
        public bool Multiline { get; init; }
    }

    public sealed class LookupOption
    {
        public string Value { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
    }
}