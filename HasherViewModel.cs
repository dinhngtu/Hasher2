using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO.Hashing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Windows.Perception.Spatial;
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
                    _compareHash = Regex.Replace(value, $"[^0-9a-fA-F]", "").ToLower(CultureInfo.InvariantCulture);
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

        public struct AvailableAlgorithm {
            public string Name;
            public int HashLength;
            public bool Enabled;
            public Func<object> Factory;
        }

        public static IList<AvailableAlgorithm> AvailableAlgorithms = new List<AvailableAlgorithm>() {
            new AvailableAlgorithm() {
                Name = "CRC32",
                HashLength = 32,
                Enabled = true,
                Factory = () => new Crc32(),
            },
            new AvailableAlgorithm() {
                Name = "CRC64",
                HashLength = 64,
                Enabled = false,
                Factory = () => new Crc64(),
            },
            new AvailableAlgorithm() {
                Name = HashAlgorithmNames.Md5,
                HashLength = 128,
                Enabled = true,
                Factory = () => HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).CreateHash(),
            },
            new AvailableAlgorithm() {
                Name = HashAlgorithmNames.Sha1,
                HashLength = 160,
                Enabled = true,
                Factory = () => HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1).CreateHash(),
            },
            new AvailableAlgorithm() {
                Name = HashAlgorithmNames.Sha256,
                HashLength = 256,
                Enabled = true,
                Factory = () => HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256).CreateHash(),
            },
            new AvailableAlgorithm() {
                Name = HashAlgorithmNames.Sha384,
                HashLength = 384,
                Enabled = true,
                Factory = () => HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha384).CreateHash(),
            },
            new AvailableAlgorithm() {
                Name = HashAlgorithmNames.Sha512,
                HashLength = 512,
                Enabled = true,
                Factory = () => HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha512).CreateHash(),
            },
        };

        public IList<string> AvailableAlgorithmNames = AvailableAlgorithms.Where(x => x.Enabled).Select(x => x.Name).ToList();

        string FindHash(string value) {
            if (!string.IsNullOrEmpty(OutputHash)) {
                var hashStringLen = OutputHash.Length;
                var match = Regex.Match(value ?? "", $"([0-9a-fA-F](?:[- ]*)){{{hashStringLen}}}");
                if (match.Success) {
                    return match.Value;
                }
            }
            // long hash to short hash
            foreach (var avail in AvailableAlgorithms.Reverse()) {
                var hashStringLen = avail.HashLength / 4;
                var match = Regex.Match(value ?? "", $"([0-9a-fA-F](?:[- ]*)){{{hashStringLen}}}");
                if (match.Success) {
                    return match.Value;
                }
            }
            return null;
        }

        public void SetCompareHashFromFileName(string name) {
            var match = Regex.Match(name ?? "", @".+\[([0-9a-fA-F]{8})\]");
            if (match.Success) {
                CompareHash = match.Groups[1].Value;
                SyncHashAlgo();
            }
        }

        void SyncHashAlgo() {
            var bits = CompareHash?.Length * 4;
            foreach (var avail in AvailableAlgorithms) {
                if (avail.Enabled && avail.HashLength == bits) {
                    SelectedAlgorithm = avail.Name;
                    return;
                }
            }
        }

        public void OnPaste(string value) {
            CompareHash = FindHash(value) ?? "";
            SyncHashAlgo();
        }

        public static object CreateHash(string algoId) {
            foreach (var avail in AvailableAlgorithms) {
                if (avail.Enabled && avail.Name == algoId) {
                    return avail.Factory();
                }
            }
            return null;
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
