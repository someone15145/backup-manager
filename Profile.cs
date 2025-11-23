// Profile.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BackupManager
{
    public class Profile : INotifyPropertyChanged
    {
        private string _name;
        private string _sourcePath;
        private string _backupPath;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string SourcePath
        {
            get => _sourcePath;
            set { _sourcePath = value; OnPropertyChanged(); }
        }

        public string BackupPath
        {
            get => _backupPath;
            set { _backupPath = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}