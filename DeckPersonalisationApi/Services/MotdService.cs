using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Services;

public class MotdService(ApplicationContext ctx)
{
    public MessageOfTheDay? Get()
        => ctx.MessageOfTheDay.FirstOrDefault();

    public MessageOfTheDay Set(string name, string description, MessageOfTheDaySeverity severity)
    {
        MessageOfTheDay motd = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            Severity = severity,
            Date = DateTimeOffset.Now
        };

        ctx.MessageOfTheDay.Add(motd);
        ctx.SaveChanges();
        return motd;
    }
    
    public MessageOfTheDay Update(string name, string description, MessageOfTheDaySeverity severity)
    {
        MessageOfTheDay obj = Get().Require();
        obj.Name = name;
        obj.Description = description;
        obj.Severity = severity;
        ctx.MessageOfTheDay.Update(obj);
        ctx.SaveChanges();
        return obj;
    }

    public void Delete()
    {
        ctx.MessageOfTheDay.RemoveRange(ctx.MessageOfTheDay);
        ctx.SaveChanges();
    }
}