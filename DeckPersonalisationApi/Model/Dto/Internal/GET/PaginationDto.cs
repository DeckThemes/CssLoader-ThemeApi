namespace DeckPersonalisationApi.Model.Dto.Internal.GET;

public class PaginationDto
{
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 50;
    public List<string> Filters { get; set; } = new();
    public string Order { get; set; } = "";

    public PaginationDto(int page, int perPage, string filters, string order)
    {
        Page = page;
        PerPage = perPage;
        Filters = filters.Split('.').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        Order = order;
    }
}