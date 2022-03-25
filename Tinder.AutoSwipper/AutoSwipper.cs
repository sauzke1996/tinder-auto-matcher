using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
                await MatchTeasedRecommendations(cancellationToken);
                var profile = await _client.GetProfile(cancellationToken);
                await MatchRecommendations(profile.LikesInfo.LikesRemaining, cancellationToken);                
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An non-recoverable error occured");
            }
        }

        private async Task MatchRecommendations(int matchesRemaining,CancellationToken cancellationToken)
        {
            var recs = await GetRecommendations(cancellationToken);
                
            _logger.LogInformation($"{recs.Count} Recommendations");

            var matchCount = matchesRemaining;
            foreach (var rec in recs)
            {
                var score = _scoring.Score(rec);
                if (score >= MIN_SCORE)
                {

                    var like = await _client.Like(rec.UserInfo.Id, cancellationToken);
                    if (like.Match != null)
                        _logger.LogInformation($"You matched {rec.UserInfo.Name} with score {score}");
                    else
                        _logger.LogError($"{rec.UserInfo.Name} ({rec.UserInfo.Id}) was not a match with score {score}");

                    matchCount--;
                }
                else
                {
                    await _client.Pass(rec.UserInfo.Id, cancellationToken);
                    _logger.LogError($"Passed {rec.UserInfo.Name} ({rec.UserInfo.Id}) with score {score}");
                }

                if (matchCount == 0)
                {
                    break;
                }
            }
        }

        private async Task MatchTeasedRecommendations(CancellationToken cancellationToken)
        {
            ISet<string> teaserPhotoIds = await GetTeaserPhotoIds(cancellationToken);
            _logger.LogDebug($"{teaserPhotoIds.Count} people liked you");

            await foreach (var teasedRec in GetTeasedRecommendations(teaserPhotoIds, cancellationToken))
            {
                var like = await _client.Like(teasedRec.UserInfo.Id, cancellationToken);
                if (like.Match != null)
                    _logger.LogInformation("You matched " + teasedRec.UserInfo.Name);
                else
                    _logger.LogError($"{teasedRec.UserInfo.Name} ({teasedRec.UserInfo.Id}) was not a match");
            }
        }

        private async Task<IReadOnlyList<Recommendation>> GetRecommendations(CancellationToken cancellationToken)
        {
            return await _client.GetRecommendations(cancellationToken) 
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

        private async IAsyncEnumerable<Recommendation> GetTeasedRecommendations(ISet<string> teaserPhotoIds, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var recs = await GetRecommendations(cancellationToken);
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
