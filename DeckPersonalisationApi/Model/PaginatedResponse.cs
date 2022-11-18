using DeckPersonalisationApi.Model.Dto.External.GET;

namespace DeckPersonalisationApi.Model;

public record PaginatedResponse<T>(long Total, IEnumerable<T> Items) : IToDto<PaginationResponseDto>
    where T : IToDto
{
    public PaginationResponseDto ToDto()
        => new(Total, Items.Select(x => x.ToDtoObject()).ToList());

    public object ToDtoObject()
        => ToDto();
}