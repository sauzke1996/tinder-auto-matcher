using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Tinder.Models
{
    public class Descriptor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;
        [JsonPropertyName("choice_selections")]
        public IReadOnlyList<Selection> ChoiceSelections { get; set; } = default!;

        public class Selection
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = default!;
        }
    }
}
