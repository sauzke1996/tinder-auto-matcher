using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tinder.Models;

namespace Tinder.Scoring
{
    public class ScoringAgent : IScoring
    {
        private const int MAX_DISTANCE_MI = 16;
        private const int MIN_PHOTO_COUNT = 2;

        private readonly string[] SuperLikeInterests = new string[]
        {
            "Gamer",
            "Gaming",
            "Movies",
            "Netflix",
            "Esports"
        };

        private readonly string[] LikeInterests = new string[]
        {
            "Cycling",
            "BoardGames",
            "Coffee",
            "Politics",
            "Walking",
            "Travel"
        };

        private readonly string[] DislikeInterests = new string[]
        {
            "Soccer",
            "Cat Lover",
            "Language Exchange",
            "Running",
            "Plant-Based",
            "Astrology",
            "Outdoors"
        };

        private readonly string[] LolNope = new string[]
        {
            "Athlete",
            "Sports",
            "Golf",
            "Soccer"
        };

        private readonly string[] LikeBio = new string[]
        {
            "Games",
            "Anime",
            "Marvel",
            "DC",
            "Nerd",
            "Star Wars",
            "DnD"
        };

        private readonly string[] LolNopeBio = new string[]
        {
            "Active",
            "Outdoors",
            "Just visiting",
            "Here for a good time not a long time",
            "420 Friendly",
            "Acab",
            "Fuck the police",
            "Polyamorous",
            "Just friends",
            "Non-monogamous",
            "Non monogamous",
            "Nothing serious",
            "Single mom",
            "Communist",
            "eat the rich"
        };

        public int Score(Recommendation recommendation)
        {
            int score = 0;
            if (recommendation.DistanceMi > MAX_DISTANCE_MI)
                score -= 100;

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
                score--;

            score += SuperLikeInterests.Count(i => recommendation.UserInfo.Bio.Contains(i, StringComparison.InvariantCultureIgnoreCase));
            score += LikeBio.Count(i => recommendation.UserInfo.Bio.Contains(i, StringComparison.InvariantCultureIgnoreCase));

            score -= (LolNopeBio.Count(i => recommendation.UserInfo.Bio.Contains(i, StringComparison.InvariantCultureIgnoreCase)) * 5);

            return score;
        }
    }
}
