namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class PaginationFilters
{
    public List<string> Filters { get; set; }
    public List<string> Order { get; set; }

    public PaginationFilters(List<string> filters, List<string> order)
    {
        Filters = filters;
        Order = order;
    }
}