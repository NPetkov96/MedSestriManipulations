using System.Text.Json.Serialization;

namespace MedSestriManipulations.Models
{
    public class HistoryListItem
    {
        public string HeaderText { get; init; } = string.Empty;
        public Patient? Patient { get; init; }

        [JsonIgnore]
        public bool IsHeader => Patient == null;

        [JsonIgnore]
        public bool IsPatient => Patient != null;
    }
}
