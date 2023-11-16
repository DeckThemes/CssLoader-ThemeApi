using DeckPersonalisationApi.Model.Dto.External.GET;

namespace DeckPersonalisationApi.Model;

public class MessageOfTheDay : IToDto<MessageOfTheDayDto>
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTimeOffset Date { get; set; }
    public MessageOfTheDaySeverity Severity { get; set; }

    public MessageOfTheDayDto ToDto()
        => new(this);

    public object ToDtoObject()
        => ToDto();
}