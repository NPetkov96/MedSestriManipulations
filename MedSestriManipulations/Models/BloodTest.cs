using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Maui.Controls;

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

        private string _rowBackgroundColor = "Transparent";

        [JsonIgnore]
        public string RowBackgroundColor
        {
            get => _rowBackgroundColor;
            set
            {
                if (_rowBackgroundColor == value)
                    return;

                _rowBackgroundColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowBackgroundColor)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        // UI-only: current search query, used for text highlighting
        [JsonIgnore]
        public string SearchText { get; set; } = string.Empty;

        // UI-only: cached FormattedString for display (reduces per-cell formatting work)
        [JsonIgnore]
        public FormattedString? DisplayNameFormatted { get; private set; }

        // Update the cached formatted string. Must be called on the UI thread.
        public void UpdateFormattedName(string search)
        {
            SearchText = search ?? string.Empty;
            var fs = new FormattedString();

            if (string.IsNullOrEmpty(SearchText))
            {
                fs.Spans.Add(MakeSpan(Name, Color.FromArgb("#1A1A1A")));
                DisplayNameFormatted = fs;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayNameFormatted)));
                return;
            }

            int idx = Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                fs.Spans.Add(MakeSpan(Name, Color.FromArgb("#1A1A1A")));
                DisplayNameFormatted = fs;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayNameFormatted)));
                return;
            }

            if (idx > 0)
                fs.Spans.Add(MakeSpan(Name.Substring(0, idx), Color.FromArgb("#1A1A1A")));

            fs.Spans.Add(new Span
            {
                Text = Name.Substring(idx, SearchText.Length),
                TextColor = Color.FromArgb("#0066CC"),
                BackgroundColor = Color.FromArgb("#E8F0FF"),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            });

            if (idx + SearchText.Length < Name.Length)
                fs.Spans.Add(MakeSpan(Name.Substring(idx + SearchText.Length), Color.FromArgb("#1A1A1A")));

            DisplayNameFormatted = fs;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayNameFormatted)));
        }

        private static Span MakeSpan(string text, Color color) => new Span
        {
            Text = text,
            TextColor = color,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold
        };
    }
}
