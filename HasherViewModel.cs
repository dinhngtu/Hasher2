using System;
using System.ComponentModel;
using System.Threading;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Hasher2 {
    public class HasherViewModel : INotifyPropertyChanged {
        private bool _enabled;
        public bool Enabled {
            get {
                return _enabled;
            }
            set {
                if (_enabled != value) {
                    _enabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        private string _outputHash;
        public string OutputHash {
            get {
                return _outputHash;
            }
            set {
                if (_outputHash != value) {
                    _outputHash = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        private string _compareHash;
        public string CompareHash {
            get {
                return _compareHash;
            }
            set {
                if (_compareHash != value) {
                    _compareHash = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        private StorageFile _pickedFile;
        public StorageFile PickedFile {
            get {
                return _pickedFile;
            }
            set {
                if (_pickedFile != value) {
                    _pickedFile = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        private double _progress;
        public double Progress {
            get {
                return _progress;
            }
            set {
                if (_progress != value) {
                    _progress = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        private bool _showProgress;
        public bool ShowProgress {
            get {
                return _showProgress;
            }
            set {
                if (_showProgress != value) {
                    _showProgress = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        private string _selectedAlgorithm;
        public string SelectedAlgorithm {
            get {
                return _selectedAlgorithm;
            }
            set {
                if (_selectedAlgorithm != value) {
                    _selectedAlgorithm = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        public bool PickedFileReady => PickedFile != null;
        public bool HashValueReady => !string.IsNullOrEmpty(OutputHash);
        public string HashCompareStatus {
            get {
                if (!Enabled
                    || string.IsNullOrEmpty(OutputHash)
                    || string.IsNullOrWhiteSpace(CompareHash)) {
                    return "";
                }
                return string.Equals(OutputHash, CompareHash?.Trim(), StringComparison.OrdinalIgnoreCase).ToString();
            }
        }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
