using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Tinder.Models
{
    public class Interest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;
    }
}
