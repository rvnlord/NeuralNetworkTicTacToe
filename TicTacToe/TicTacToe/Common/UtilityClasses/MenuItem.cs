using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TipTacToe.Common.UtilityClasses
{
    public class MenuItem : INotifyPropertyChanged
    {
        private bool _isEnabled = true;
        private string _text;
        private ObservableCollection<MenuItem> _subItems;
        private bool _isExpanded = true;
        public event PropertyChangedEventHandler PropertyChanged;

        public MenuItem(string text)
        {
            Text = text;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                OnNotifyPropertyChanged("IsEnabled");
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                OnNotifyPropertyChanged("IsExpanded");
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text == value) return;
                _text = value;
                OnNotifyPropertyChanged("Text");
            }
        }

        public ObservableCollection<MenuItem> Items
        {
            get => _subItems ?? (_subItems = new ObservableCollection<MenuItem>());
            set
            {
                if (_subItems == value) return;
                _subItems = value;
                OnNotifyPropertyChanged("SubItems");
            }
        }

        private void OnNotifyPropertyChanged(string ptopertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ptopertyName));
        }

        public override string ToString()
        {
            return $"{Text}, {(_isEnabled ? "enabled" : "disabled")}";
        }
    }
}
