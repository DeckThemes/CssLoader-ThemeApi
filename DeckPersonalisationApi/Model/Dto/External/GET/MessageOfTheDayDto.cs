namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class MessageOfTheDayDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Severity { get; set; }

    public MessageOfTheDayDto(MessageOfTheDay motd)
    {
        Id = motd.Id;
        Name = motd.Name;
        Description = motd.Description;
        Date = motd.Date;
        Severity = motd.Severity.ToString();
    }
}