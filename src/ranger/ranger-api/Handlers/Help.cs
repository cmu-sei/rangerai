using System.Text.RegularExpressions;

namespace RangerAi.Handlers;

public class Help : HandlerBase
{
    protected override Regex MatchPattern { get; } = new(
        @"^help$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    protected override List<string> Responses { get; } =
    [
        """
        🦊 Ranger understands you at multiple levels:
        
        1. 🗣️ Explicit Commands
        
            Direct tasking, usually starting with a /:
        
                alive?
                help
                time
        
        2. 🛠️ Crucible API Access
          
            Ranger can directly interact with the Crucible platform:
        
                /ghosts
                /scenario
                /course
                
            Learn more: <a href="https://cmu-sei.github.io/rangerai/">Ranger Documentation</a>
        
        3. 🤔 Probable Intents
          
            Ambiguous or indirect phrasing triggers Ranger's AI:
        
                Ranger how many machines is ghosts running?
        
            If no known intent is matched, Ranger uses its internal learning system to infer meaning and improve over time.
        """
    ];
}
