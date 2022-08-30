using Microsoft.Extensions.Logging;
using Tinder.Models;
using Tinder.Scoring;

namespace Tinder.AutoSwipper
{
    public class AutoSwipper
    {
        private const int MIN_SCORE = 0;

        private readonly ILogger<Program> _logger;        
        private readonly ITinderClient _client;
        private readonly IScoring _scoring;        

        public AutoSwipper(ITinderClient tinderClient, IScoring scoring, ILogger<Program> logger)
        {
            _client = tinderClient;
            _scoring = scoring;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await MatchRecommendations(cancellationToken);                
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An non-recoverable error occured");
            }
        }

        private async Task MatchRecommendations(CancellationToken cancellationToken)
        {
            var recs = await GetRecommendations(true, cancellationToken);

            ISet<string> teaserPhotoIds = await GetTeaserPhotoIds(cancellationToken);
            _logger.LogInformation($"{teaserPhotoIds.Count} people liked you");

            bool likesRemaining = true;
            while (recs != null && recs.Any() && likesRemaining)
            {
                _logger.LogInformation($"{recs.Count} Recommendations");

                await MatchTeasedRecommendations(recs, teaserPhotoIds, cancellationToken);

                foreach (var rec in recs)
                {
                    var score = _scoring.Score(rec);
                    
                    if (score >= MIN_SCORE)
                    {
                        var like = await _client.Like(rec.UserInfo.Id, cancellationToken);
                        if (like.Match != null)
                            _logger.LogInformation($"You Matched {rec.UserInfo.Name} with score {score}");
                        else
                            _logger.LogInformation($"{rec.UserInfo.Name} ({rec.UserInfo.Id}) was not a match with score {score}");

                        likesRemaining = like.LikesRemaining > 0;
                        if (!likesRemaining)
                        {
                            _logger.LogInformation($"{like.LikesRemaining} Likes remaining");                            
                            break;
                        }
                    }
                    else
                    {
                        await _client.Pass(rec.UserInfo.Id, cancellationToken);
                        _logger.LogError($"Passed {rec.UserInfo.Name} ({rec.UserInfo.Id}) with score {score}");
                    }
                }
                recs = await GetRecommendations(true, cancellationToken);
            }
        }

        private async Task MatchTeasedRecommendations(IReadOnlyList<Recommendation> recs, ISet<string> teaserPhotoIds, CancellationToken cancellationToken)
        {
            foreach (var teasedRec in GetTeasedRecommendations(recs, teaserPhotoIds))
            {
                var like = await _client.Like(teasedRec.UserInfo.Id, cancellationToken);
                if (like.Match != null)
                    _logger.LogInformation("You Matched " + teasedRec.UserInfo.Name);
                else
                    _logger.LogError($"{teasedRec.UserInfo.Name} ({teasedRec.UserInfo.Id}) was not a match");

                if (like.LikesRemaining <= 0)
                {
                    _logger.LogInformation($"{like.LikesRemaining} Likes remaining");
                    break;
                }
            }
        }

        private async Task<IReadOnlyList<Recommendation>> GetRecommendations(bool explore = false,  CancellationToken cancellationToken = default)
        {
            return explore ? await _client.Explore(TinderClient.GAMING, cancellationToken) :
                await _client.GetRecommendations(cancellationToken) 
                ?? new List<Recommendation>();            
        }

        private async Task<ISet<string>> GetTeaserPhotoIds(CancellationToken cancellationToken)
        {
            var teasers = await _client.GetTeasers(cancellationToken);
            return teasers
                .SelectMany(t => t.User.Photos)
                .Select(photo => photo.Id)
                .ToHashSet();
        }

        private IEnumerable<Recommendation> GetTeasedRecommendations(IReadOnlyList<Recommendation> recs, ISet<string> teaserPhotoIds)
        {
            foreach (var rec in recs)
            {
                if (rec.UserInfo.Photos.Any(photo => teaserPhotoIds.Contains(photo.Id)))
                {
                    yield return rec;
                }
            }
        }
    }
}
