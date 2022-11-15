using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Services;

public class ImageService
{
    private UserService _user;
    private IConfiguration _conf;
    private ApplicationContext _ctx;

    public string ImageDir => _conf["Config:ImagePath"]!;
    public int MaxImages => int.Parse(_conf["Config:MaxImages"]!);

    public ImageService(UserService user, IConfiguration config, ApplicationContext ctx)
    {
        _user = user;
        _conf = config;
        _ctx = ctx;
    }

    public SavedImage? GetImage(string? id)
        => _ctx.Image.FirstOrDefault(x => x.Id == id);

    public List<SavedImage> GetImagesByUser(User user)
        => _ctx.Image.Where(x => x.Owner == user).ToList();

    public int GetImageCountByUser(User user)
        => _ctx.Image.Count(x => x.Owner == user);

    public string GetFullFilePath(SavedImage image)
        => Path.Join(ImageDir, image.Path);

    public SavedImage CreateImage(Stream image, string filename, string userId)
    {
        User? user = _user.GetUserById(userId);

        if (user == null)
            throw new BadRequestException("User not found");

        return CreateImage(image, filename, user);
    }
    public SavedImage CreateImage(Stream image, string filename, User user)
    {
        if (!filename.EndsWith(".jpg")) // TODO: Check if it's actually a jpg
            throw new BadRequestException("File is not a .jpg");

        if (image.Length >= 0x80000) // 0.5 MiB
            throw new BadRequestException("File is too large. Max is 0.5 MiB");

        if (GetImageCountByUser(user) > MaxImages)
            throw new BadRequestException("User has reached the max upload limit");

        string id = Guid.NewGuid().ToString();
        string path = $"{id}.jpg";
        var file = File.Create(Path.Join(ImageDir, $"{id}.jpg"));
        image.CopyTo(file);
        file.Close();

        SavedImage result = new()
        {
            Id = id,
            Owner = user,
            Path = path
        };
        
        _ctx.Image.Add(result);
        _ctx.SaveChanges();

        return result;
    }
}