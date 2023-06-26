using System.Text;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Services;
using Discord;
using Discord.Webhook;

namespace DeckPersonalisationApi.Utils;

public class Utils
{
    public static long DirSize(string path) => DirSize(new DirectoryInfo(path));
    public static long DirSize(DirectoryInfo d) // https://stackoverflow.com/questions/468119/whats-the-best-way-to-calculate-the-size-of-a-directory-in-net
    {    
        long size = 0;    
        // Add file sizes.
        FileInfo[] fis = d.GetFiles();
        foreach (FileInfo fi in fis) 
        {      
            size += fi.Length;    
        }
        // Add subdirectory sizes.
        DirectoryInfo[] dis = d.GetDirectories();
        foreach (DirectoryInfo di in dis) 
        {
            size += DirSize(di);   
        }
        return size;  
    }

    public static void SendDiscordWebhook(AppConfiguration configuration, CssSubmission submission)
    {
        if (string.IsNullOrWhiteSpace(configuration.DiscordWebhook))
            return;

        try
        {
            DiscordWebhookClient client = new DiscordWebhookClient(configuration.DiscordWebhook);

            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(submission.Owner.Username, submission.Owner.GetAvatarUri()!.AbsoluteUri)
                .WithFooter($"Target: {submission.New.Target} | Version: {submission.New.Version} | By: {submission.New.SpecifiedAuthor} | Type: {submission.New.Type.ToString()}");

            if (submission.Status == SubmissionStatus.AwaitingApproval)
            {
                embed.WithTitle($"New Submission: {submission.New.Name}").WithColor(Discord.Color.Blue);
            }
            else if (submission.Status == SubmissionStatus.Approved)
            {
                embed.WithTitle($"Approved: {submission.New.Name}").WithColor(Discord.Color.Green);
                
                if (submission.New.Images.Count >= 1)
                    embed.WithImageUrl(configuration.LegacyUrlBase + "blobs/" + submission.New.Images.First().Id + ".jpg");
            }
            else
            {
                embed.WithTitle($"Denied: {submission.New.Name}").WithColor(Discord.Color.Red);
            }

            client.SendMessageAsync(embeds: new []{embed.Build()}).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Sending discord webhook failed! {e.Message}");
        }
    }
    
    public static string GetFixedLengthString(int len, string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789")
    {
        StringBuilder sb = new StringBuilder();
        Random randomNumber = new Random();
        for (int i = 0; i < len; i++)
        {
            sb.Append(chars[randomNumber.Next(0, chars.Length)]);
        }
        return sb.ToString();
    }

    public static string GetFixedLengthHexString(int len)
        => GetFixedLengthString(len, "1234567890abcdef");
}