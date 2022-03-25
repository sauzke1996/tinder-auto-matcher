using Tinder.Models;

namespace Tinder.Scoring
{
    public interface IScoring
    {
        int Score(Recommendation recommendation);
    }
}
