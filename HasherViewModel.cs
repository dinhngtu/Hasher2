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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashButtonText)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashButtonIcon)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashCompareStatus)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashCompareStatusText)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashCompareStatusTextColor)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OutputHash)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashValueReady)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashCompareStatus)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashCompareStatusText)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashCompareStatusTextColor)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompareHash)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashCompareStatus)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashCompareStatusText)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashCompareStatusTextColor)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PickedFile)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PickedFileName)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PickedFileReady)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
                }
            }
        }

        private Visibility _progressVisibility;
        public Visibility ProgressVisibility {
            get {
                return _progressVisibility;
            }
            set {
                if (_progressVisibility != value) {
                    _progressVisibility = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressVisibility)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedAlgorithm)));
                }
            }
        }

        public bool PickedFileReady => PickedFile != null;
        public string PickedFileName => PickedFile?.Name;
        public string HashButtonIcon => Enabled ? "Play" : "Cancel";
        public string HashButtonText => Enabled ? "Hash" : "Cancel";
        public bool HashValueReady => !string.IsNullOrEmpty(OutputHash);
        public bool? HashCompareStatus {
            get {
                if (!Enabled
                    || string.IsNullOrEmpty(OutputHash)
                    || string.IsNullOrWhiteSpace(CompareHash)) {
                    return null;
                }
                return string.Equals(OutputHash, CompareHash?.Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }
        public string HashCompareStatusText {
            get {
                var cmp = HashCompareStatus;
                if (cmp.HasValue) {
                    return cmp.Value ? "Hashes match." : "Hashes do not match.";
                } else {
                    return "";
                }
            }
        }

        public SolidColorBrush HashCompareStatusTextColor {
            get {
                if (HashCompareStatus ?? true) {
                    return new SolidColorBrush((Color)Application.Current.Resources["SystemBaseHighColor"]);
                } else {
                    return new SolidColorBrush((Color)Application.Current.Resources["SystemErrorTextColor"]);
                }
            }
        }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
