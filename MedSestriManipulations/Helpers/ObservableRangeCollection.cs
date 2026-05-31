using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MedSestriManipulations.Helpers
{
    // Minimal ObservableRangeCollection with batch ReplaceRange/AddRange support.
    // Uses a single Reset notification to avoid many CollectionChanged events.
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        public ObservableRangeCollection()
        {
        }

        public ObservableRangeCollection(IEnumerable<T> items) : base(items != null ? new List<T>(items) : new List<T>())
        {
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;

            CheckReentrancy();
            bool added = false;
            foreach (var i in items)
            {
                Items.Add(i);
                added = true;
            }

            if (added)
            {
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public void ReplaceRange(IEnumerable<T> items)
        {
            CheckReentrancy();
            Items.Clear();
            if (items != null)
            {
                foreach (var i in items)
                    Items.Add(i);
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
