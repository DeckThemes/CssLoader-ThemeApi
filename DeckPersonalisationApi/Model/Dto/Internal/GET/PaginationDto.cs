using DeckPersonalisationApi.Exceptions;

namespace DeckPersonalisationApi.Model.Dto.Internal.GET;

public class PaginationDto
{
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 50;
    public string Search { get; set; } // TODO: implement
    public List<string> Filters { get; set; } = new();
    public string Order { get; set; } = "";

    public PaginationDto(int page, int perPage, string filters, string order)
    {
        Page = page;
        PerPage = perPage;
        Filters = filters.Split('.').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        Order = order;

        if (Page <= 0)
            throw new BadRequestException("Page is less than 1");

        if (PerPage <= 0)
            throw new BadRequestException("Per Page is less than 1");
    }
}