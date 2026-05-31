using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace MedSestriManipulations.Models
{
    // Simple grouped collection where Key is the display string for the group
    // (e.g. "Май 2026"). Inherits ObservableCollection<Patient> so it works as
    // a CollectionView group source.
    public class PatientGroup : ObservableCollection<Patient>, INotifyPropertyChanged
    {
        private readonly List<Patient> _allPatients = new();
        private bool isExpanded = false;

        public string Key { get; }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded == value) return;
                isExpanded = value;
                OnPropertyChanged();
                RefreshItems();
            }
        }

        public ICommand ToggleCommand => new Command(() => IsExpanded = !IsExpanded);

        public PatientGroup(string key)
        {
            Key = key;
        }

        public PatientGroup(string key, IEnumerable<Patient> patients)
        {
            Key = key;
            if (patients != null)
                _allPatients = patients.ToList();
            // default expanded
            RefreshItems();
        }

        private void RefreshItems()
        {
            this.Clear();
            if (IsExpanded)
            {
                foreach (var p in _allPatients)
                    this.Add(p);
            }
        }

        // Allow replacing the underlying items when group needs to be updated
        public void SetItems(IEnumerable<Patient> patients)
        {
            _allPatients.Clear();
            if (patients != null)
                _allPatients.AddRange(patients);
            RefreshItems();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
