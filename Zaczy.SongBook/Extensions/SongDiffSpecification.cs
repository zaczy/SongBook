namespace Zaczy.SongBook.Extensions;

public class SongDiffSpecification
{
    public SongDiffSpecification(string fieldName, string value, string referenceValue)
    {
        FieldName = fieldName;
        Value = value;
        ReferenceValue = referenceValue;
    }

    public string FieldName { get; set; }
    public string Value { get; set; }

    public string ReferenceValue { get; set; }
}