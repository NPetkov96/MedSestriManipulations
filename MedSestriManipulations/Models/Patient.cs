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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
