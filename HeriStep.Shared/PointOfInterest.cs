using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeriStep.Shared
{
    public class PointOfInterest
    {
        [JsonPropertyName("isOpen")]
        public bool IsOpen { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("tourID")]
        public int TourID { get; set; }

        [JsonPropertyName("ttsScript")]
        public string TtsScript { get; set; }

        [Key]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("radius")]
        public double Radius { get; set; }

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("audioUrl")]
        public string AudioUrl { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; }
    }
}