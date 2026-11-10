
namespace RangerAi.Infrastructure.Models
{
    public class Integration
    {
        public string Id { get; set; }

        /// <summary>
        /// Listener URL for the integrated server
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Lister port for receiver
        /// </summary>
        public int Port { get; set; }
    }
}
