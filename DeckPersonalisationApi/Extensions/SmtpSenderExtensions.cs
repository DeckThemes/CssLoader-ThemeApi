using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Services;

namespace DeckPersonalisationApi.Extensions;

public static class SmtpSenderExtensions
{
    public static void SendNewSubmissionEmail(this SmtpSender sender, CssSubmission submission,
        List<string> errors)
    {
        User author = submission.Owner;
        
        if (string.IsNullOrWhiteSpace(author.Email))
        {
            Console.WriteLine($"User {author.Username} has no email configured...");
            return;
        }

        string cssOrAudio = submission.New.Type == ThemeType.Css ? "CSS theme" : "audio pack";
        string errorText =
            $"Our automated system has found a few issues with your submission:\n{string.Join("\n", errors.Select(x => $"- {x}"))}\n\n";

        string body = $"Hi {author.Username}!\n\n" +
                      $"We have received your {cssOrAudio} submission '{submission.New.Name}'\n\n" +
                      $"You can view your submission here: {sender.Config.FrontendUrl}/submissions/view?submissionId={submission.Id}\n\n" +
                      $"{errorText}" + 
                      $"We also have a discord, for more instant communication: {sender.Config.DiscordInvite}\n\n" +
                      "Cheers,\nThe DeckThemes team";

        try
        {
            sender.Send(author.Email, "DeckThemes: Submission received", body);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Sending email failed: {e.Message}");
        }
    }
    
    public static void DenyOrApproveSubmissionEmail(this SmtpSender sender, CssSubmission submission)
    {
        User author = submission.Owner;
        
        if (string.IsNullOrWhiteSpace(author.Email))
        {
            Console.WriteLine($"User {author.Username} has no email configured...");
            return;
        }

        string cssOrAudio = submission.New.Type == ThemeType.Css ? "CSS theme" : "audio pack";
        string reason = string.IsNullOrWhiteSpace(submission.Message) ? "(No message has been provided)" : submission.Message;
        string stateShort = submission.Status == SubmissionStatus.Approved ? "approved" : "denied";
        string state = submission.Status == SubmissionStatus.Approved
            ? "and we have approved it!"
            : "and we are sorry to say to have denied it.";

        string body = $"Hi {author.Username}!\n\n" +
                      $"We have reviewed your {cssOrAudio} submission '{submission.New.Name}', {state}\n\n" +
                      $"You can view your submission here: {sender.Config.FrontendUrl}/submissions/view?submissionId={submission.Id}\n\n" +
                      $"Reviewed by: {submission.ReviewedBy!.Username}\nAttached message:\n{reason}\n\n" + 
                      $"We also have a discord, for more instant communication: {sender.Config.DiscordInvite}\n\n" +
                      "Cheers,\nThe DeckThemes team";

        try
        {
            sender.Send(author.Email, $"DeckThemes: Submission {stateShort}", body);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Sending email failed: {e.Message}");
        }
    }
}