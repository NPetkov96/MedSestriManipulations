using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace MedSestriManipulations.Models
{
    // Simple grouped collection where Key is the display string for the group
    // (e.g. "Май 2026"). Inherits ObservableCollection<Patient> so it works as
    // a CollectionView group source.
    public class PatientGroup : Helpers.ObservableRangeCollection<Patient>, INotifyPropertyChanged
    {
        private readonly Helpers.ObservableRangeCollection<Patient> _allPatients = new();
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

        private readonly ICommand _toggleCommand;
        public ICommand ToggleCommand => _toggleCommand;

        public PatientGroup(string key)
        {
            Key = key;
            _toggleCommand = new Command(() => IsExpanded = !IsExpanded);
        }

        public PatientGroup(string key, IEnumerable<Patient> patients)
        {
            Key = key;
            _toggleCommand = new Command(() => IsExpanded = !IsExpanded);
            if (patients != null)
                _allPatients.AddRange(patients);
            // default expanded
            RefreshItems();
        }

        private void RefreshItems()
        {
            if (IsExpanded)
            {
                this.Clear();
                this.AddRange(_allPatients);
            }
            else
            {
                this.Clear();
            }
        }

        // Allow replacing the underlying items when group needs to be updated
        public void SetItems(IEnumerable<Patient> patients)
        {
            _allPatients.ReplaceRange(patients ?? Enumerable.Empty<Patient>());
            RefreshItems();
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
