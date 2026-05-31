using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MedSestriManipulations.Models
{
    public class BloodTest : INotifyPropertyChanged
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("bngPrice")]
        public decimal BngPrice { get; set; }

        [JsonPropertyName("euroPrice")]
        public decimal EuroPrice { get; set; }

        [JsonPropertyName("hasPriority")]
        public bool HasPriority { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
