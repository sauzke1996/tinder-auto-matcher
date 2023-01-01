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

            if (recommendation.UserInfo.GenderInfo == UserProfile.Gender.Male)
            {
                return -999;
            }

            if(recommendation.UserInfo.BirthDate.HasValue &&
                (recommendation.UserInfo.BirthDate.Value.Year > 2000 || recommendation.UserInfo.BirthDate.Value.Year < 1987))
            {
                score -= 10;
            }

            if (recommendation.DistanceMi > MAX_DISTANCE_MI)
                score -= recommendation.DistanceMi/MAX_DISTANCE_MI;

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

            // Descriptors
            score += ScoreFamilyPlans(recommendation.UserInfo);
            score += ScoreSmoking(recommendation.UserInfo);

            // Photos
            if (recommendation.UserInfo.Photos.Count < MIN_PHOTO_COUNT)
                score -= 6;

            //bio
            score += SuperLikeInterests.Count(i => recommendation.UserInfo.Bio.Contains(i, StringComparison.InvariantCultureIgnoreCase));
            score += LikeBio.Count(i => recommendation.UserInfo.Bio.Contains(i, StringComparison.InvariantCultureIgnoreCase));

            score -= (LolNopeBio.Count(i => recommendation.UserInfo.Bio.Contains(i, StringComparison.InvariantCultureIgnoreCase)) * 5);

            return score;
        }

        private int ScoreFamilyPlans(UserRecommendation recommendation)
        {
            var familyPlans = recommendation?.SelectedDescriptors?.FirstOrDefault(d => d.Name.Equals("Family Plans", StringComparison.InvariantCultureIgnoreCase));

            if (familyPlans == null)
                return 0;

            if (familyPlans.ChoiceSelections.Any(s => s.Name.Equals("I want children", StringComparison.InvariantCultureIgnoreCase)))
                return -20;

            return 1;
        }

        private int ScoreSmoking(UserRecommendation recommendation)
        {
            var smoking = recommendation?.SelectedDescriptors?.FirstOrDefault(d => d.Name.Equals("Smoking", StringComparison.InvariantCultureIgnoreCase));

            if (smoking == null)
                return 0;

            if (smoking.ChoiceSelections.Any(s => s.Name.Equals("Non-smoker", StringComparison.InvariantCultureIgnoreCase)))
                return 1;

            return -10;
        }
    }
}
