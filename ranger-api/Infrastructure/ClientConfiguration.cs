namespace RangerAi.Infrastructure;

public class ClientConfiguration
{
    public class ApplicationSettings
    {
        public bool ResolveLocal { get; set; }
        public bool ResolveLocalMl { get; set; }
        public bool ResolveLuis { get; set; }
    }
}
