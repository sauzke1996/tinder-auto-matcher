using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tinder.Models;

using static Tinder.Scoring.TagDictionary;

namespace Tinder.Scoring
{
    public class ScoringAgent : IScoring
    {
        private const int MAX_DISTANCE_MI = 16;
        private const int MIN_PHOTO_COUNT = 2;

        public int Score(Recommendation recommendation)
        {
            int score = 0;
            if (recommendation.DistanceMi > MAX_DISTANCE_MI)
                score -= 15;

            //tags
            if (recommendation.ExperimentInfo?.UserInterest?.SelectedInterests != null)
            {
                score += (recommendation.ExperimentInfo.UserInterest.SelectedInterests
                    .Count(i => SuperLikeInterests.Any(l => l.Equals(i.Name, StringComparison.InvariantCultureIgnoreCase))) * 3);

                score += recommendation.ExperimentInfo.UserInterest.SelectedInterests
                    .Count(i => LikeInterests.Any(l => l.Equals(i.Name, StringComparison.InvariantCultureIgnoreCase)));

                score -= recommendation.ExperimentInfo.UserInterest.SelectedInterests
                    .Count(i => DislikeInterests.Any(l => l.Equals(i.Name, StringComparison.InvariantCultureIgnoreCase)));

                score -= (recommendation.ExperimentInfo.UserInterest.SelectedInterests
                    .Count(i => LolNope.Any(l => l.Equals(i.Name, StringComparison.InvariantCultureIgnoreCase))) * 3);
            }

            if (recommendation.UserInfo.Photos.Count < MIN_PHOTO_COUNT)
                score -= 6;

            //bio
            score += SuperLikeInterests.Count(i => recommendation.UserInfo.Bio.Contains(i, StringComparison.InvariantCultureIgnoreCase));
            score += LikeBio.Count(i => recommendation.UserInfo.Bio.Contains(i, StringComparison.InvariantCultureIgnoreCase));

            score -= (LolNopeBio.Count(i => recommendation.UserInfo.Bio.Contains(i, StringComparison.InvariantCultureIgnoreCase)) * 5);

            return score;
        }
    }
}
