using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MedSestriManipulations.Models
{
    public class Patient : INotifyPropertyChanged
    {
        public string FullName { get; set; } = "";
        public string Note { get; set; } = "";
        public string EGN { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string LabId { get; set; } = "";
        public string LabPassword { get; set; } = "";
        public DateTime Date { get; set; }
        public List<BloodTest> BloodTests { get; set; } = new();

        private bool isSelected;

        [JsonIgnore]
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public DateTime CreatedAtLocal => Date.ToLocalTime();

        // ── UI-only display helpers ──
        [JsonIgnore]
        public decimal TotalEuro => BloodTests?.Sum(b => b.EuroPrice) ?? 0;

        [JsonIgnore]
        public int TestsCount => BloodTests?.Count ?? 0;

        [JsonIgnore]
        public string TestsCountText =>
            TestsCount == 1 ? "1 изследване" : $"{TestsCount} изследвания";

        [JsonIgnore]
        public string DateText => Date.ToString("dd.MM.yyyy  ·  HH:mm");

        // Short date for compact rows: "29.05 · 12:44"
        [JsonIgnore]
        public string ShortDateText => Date.ToString("dd.MM · HH:mm");

        // Phone grouped as "0878 559 095"
        [JsonIgnore]
        public string PhoneFormatted
        {
            get
            {
                var p = PhoneNumber?.Trim() ?? string.Empty;
                if (p.Length == 10)
                    return $"{p.Substring(0, 4)} {p.Substring(4, 3)} {p.Substring(7, 3)}";
                return p;
            }
        }

        [JsonIgnore]
        public bool IsZeroPrice => TotalEuro == 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
