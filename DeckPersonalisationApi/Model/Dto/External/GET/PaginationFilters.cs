namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class PaginationFilters
{
    public Dictionary<string, long> Filters { get; set; }
    public List<string> Order { get; set; }

    public PaginationFilters(Dictionary<string, long> filters, List<string> order)
    {
        Filters = filters;
        Order = order;
    }
}