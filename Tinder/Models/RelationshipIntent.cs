using System.Text.Json.Serialization;

namespace Tinder.Models
{
    public class RelationshipIntent
    {
        [JsonPropertyName("descriptor_choice_id")]
        public string DescriptorChoiceId { get; set; } = default!;

        [JsonPropertyName("body_text")]
        public string BodyText { get; set; } = default!;
    }
}
