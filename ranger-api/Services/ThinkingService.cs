namespace RangerAi.Services;

public class ThinkingService
{
    public static string GetThinking()
    {
        var prompts = @"Sniffing out the details
Scanning the brush
Hunting the idea
Laying low and pondering
Curling up with a thought
Testing the ground ahead
Listening to the wind
""Laying low, pondering""
Watching from the shadows
Watching from the shadows
Waiting for the right moment
Testing the ground ahead
Waiting for the right moment
Scanning the brush
Calculating my next step
Listening to the wind
Scanning the brush
Hunting the idea
Scanning the brush
Watching from the shadows
Waiting for the right moment
Sniffing out the details
Circling the idea
Plotting a clever move
Circling the idea
Planning my path
Tail twitching in thought
Testing the ground ahead
Scanning the brush
Hunting the idea
Whiskers twitching
Calculating my next step
Hunting the idea
Circling the idea
Thinking quietly
Sniffing out the details
Reading the trail
Planning my path
Ears perked up
Whiskers twitching
Testing the ground ahead
Whiskers twitching
Scanning the brush
Circling the idea
Tracking the scent
Scanning the brush
Circling the idea
Waiting for the right moment
Scanning the brush
Calculating my next step
Eyes narrowing
Tracking the scent
Tail twitching in thought
Hunting the idea
Tracking the scent
Scanning the brush
Listening to the wind
Planning my path
Eyes narrowing
Circling the idea
Curling up with a thought
Tracking the scent
Curling up with a thought
Ears perked up
Ears perked up
Reading the trail
Thinking quietly
Listening to the wind
Plotting a clever move
Hunting the idea
Hunting the idea
Whiskers twitching
Tail twitching in thought
Scanning the brush
Testing the ground ahead
""Laying low, pondering""
Eyes narrowing
Tracking the scent
Tail twitching in thought
Sniffing out the details
Circling the idea
Sniffing out the details
Planning my path
Curling up with a thought
Waiting for the right moment
Circling the idea
Hunting the idea
Curling up with a thought
Sniffing out the details
Curling up with a thought
Eyes narrowing
Tail twitching in thought
Testing the ground ahead
Reading the trail
Hunting the idea
Fox-Like Thinking Prompts
Calling in recon
Hunting the answer
Looking deeper
Sniffing out patterns
Tracking the trail
Ears up!
Eyes sharpening
Scent acquired!
My tail is twitching
Listening to the wind
Glancing at the shadows
Waiting for movement
Scanning the brush
""Silent paws, sharp mind""
Circling the thought
Alert and analyzing
On the scent
Following the track
Marking the path
Quiet focus
Thinking...
";
        var p = prompts.Trim()
            .Split('\n')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();
        var r = new Random();
        return p[r.Next(p.Length)];
    }

}
