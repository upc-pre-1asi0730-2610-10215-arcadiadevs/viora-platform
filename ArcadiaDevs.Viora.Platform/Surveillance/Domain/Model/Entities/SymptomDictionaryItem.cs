namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;

public class SymptomDictionaryItem
{
    public string Id { get; private set; }
    public string DescriptionEn { get; private set; }
    public string DescriptionEs { get; private set; }

    protected SymptomDictionaryItem()
    {
        Id = string.Empty;
        DescriptionEn = string.Empty;
        DescriptionEs = string.Empty;
    }

    public SymptomDictionaryItem(string id, string descriptionEn, string descriptionEs)
    {
        Id = id;
        DescriptionEn = descriptionEn;
        DescriptionEs = descriptionEs;
    }
}
