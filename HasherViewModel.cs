using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO.Hashing;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.Security.Cryptography.Core;
using Windows.Storage;

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
                    _compareHash = Regex.Replace(value ?? "", @"[^0-9a-fA-F]", "").ToLower(CultureInfo.InvariantCulture);
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
                    _hashOutputInSync = false;
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
                    _hashOutputInSync = false;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        public IList<string> AvailableAlgorithms = new List<string>() {
            "CRC32",
            HashAlgorithmNames.Md5,
            HashAlgorithmNames.Sha1,
            HashAlgorithmNames.Sha256,
            HashAlgorithmNames.Sha384,
            HashAlgorithmNames.Sha512,
        };

        public static object CreateHash(string algoId) {
            switch (algoId) {
                case "CRC32":
                    return new Crc32();
                case "CRC64":
                    return new Crc64();
                default:
                    return HashAlgorithmProvider.OpenAlgorithm(algoId).CreateHash();
            }
        }

        private bool _hashOutputInSync;
        public bool HashOutputInSync {
            get {
                return _hashOutputInSync;
            }
            set {
                if (_hashOutputInSync != value) {
                    _hashOutputInSync = value;
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
                    || string.IsNullOrEmpty(CompareHash)) {
                    return "";
                }
                if (HashValueReady && !HashOutputInSync) {
                    return "NeedsRehash";
                }
                return string.Equals(OutputHash, CompareHash, StringComparison.OrdinalIgnoreCase).ToString();
            }
        }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
