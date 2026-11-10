using System.Text.RegularExpressions;
using RangerAi.Infrastructure.Extensions;
using RangerAi.Infrastructure.Models;
using RestSharp;

namespace RangerAi.Handlers
{
    public interface IHandler
    {
        ChatRequest ChatRequest { get; set; }
        ChatResponse ChatResponse { get; set; }
        Task<ChatResponse> Handle();
        double Score { get; }
        bool ShouldAnswer { get; }
    }

    public abstract class HandlerBase : IHandler
    {
        private int _score = -1;

        public virtual string Intent => GetType().Name;

        public virtual ChatRequest ChatRequest { get; set; }
        public virtual ChatResponse ChatResponse { get; set; }

        protected virtual Regex MatchPattern { get; }
        protected virtual List<string> Responses { get; }

        public virtual double Score
        {
            get
            {
                if (_score < 0 && this.ChatResponse != null &&
                    this.ChatRequest.Messages.Count > 0 &&
                    this.ChatRequest.Messages.Last().Content.Contains(MatchPattern.ToString(),
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    _score = 1;
                }

                return _score;
            }
        }

        public virtual bool ShouldAnswer => this.ChatRequest != null &&
                                            this.ChatRequest.Messages.Count > 0 &&
                                            MatchPattern.IsMatch(this.ChatRequest.Messages.Last().Content);

        public virtual Task<ChatResponse> Handle()
        {
            return Task.FromResult(new ChatResponse
            {
                Message = new ChatMessage { Content = Responses.PickRandom() }
            });
        }
    }
}
