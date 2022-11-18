namespace DeckPersonalisationApi.Model;

public interface IToDto
{
    object ToDtoObject();
}

public interface IToDto<T> : IToDto
{
    T ToDto();
}