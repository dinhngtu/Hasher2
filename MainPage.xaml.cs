using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Hasher2 {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [System.Runtime.InteropServices.Guid("DCBE1619-05D2-40DA-ABAC-99C5D6D8412B")]
    public sealed partial class MainPage : Page {
        private const int BlockSize = 64 * 1024;
        private const long HashReportBlockSize = 1024 * 1024;
        private const ulong AutoHashSize = ulong.MaxValue;
        private readonly Size MinSize = new Size(500, 380);

        public HasherViewModel ViewModel { get; set; }

        public MainPage() {
            var av = ApplicationView.GetForCurrentView();
            var cav = CoreApplication.GetCurrentView();

            InitializeComponent();
            InitializeTitleBar(av, cav);

            av.SetPreferredMinSize(MinSize);
            ApplicationView.PreferredLaunchViewSize = MinSize;
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            ViewModel = new HasherViewModel() {
                Enabled = true,
                SelectedAlgorithm = "SHA256",
                ShowProgress = false,
            };
        }

        private void InitializeTitleBar(ApplicationView av, CoreApplicationView cav) {
            cav.TitleBar.ExtendViewIntoTitleBar = true;
            UpdateTitleBar(cav.TitleBar, null);
            Window.Current.SetTitleBar(AppTitleBar);
            Window.Current.Activated += CurrentWindow_Activated;
            av.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            av.TitleBar.ButtonForegroundColor = (Color)Resources["SystemBaseHighColor"];
            av.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            av.TitleBar.ButtonInactiveForegroundColor = (Color)Resources["SystemChromeDisabledLowColor"];
            av.TitleBar.ButtonHoverBackgroundColor = (Color)Resources["SystemListLowColor"];
            av.TitleBar.ButtonHoverForegroundColor = (Color)Resources["SystemBaseHighColor"];
            av.TitleBar.ButtonPressedBackgroundColor = (Color)Resources["SystemListMediumColor"];
            av.TitleBar.ButtonPressedForegroundColor = (Color)Resources["SystemBaseHighColor"];
            cav.TitleBar.LayoutMetricsChanged += UpdateTitleBar;
            cav.TitleBar.IsVisibleChanged += TitleBar_IsVisibleChanged;
        }

        private void CurrentWindow_Activated(object sender, WindowActivatedEventArgs e) {
            VisualStateManager.GoToState(
                this,
                e.WindowActivationState == CoreWindowActivationState.Deactivated
                    ? WindowNotFocused.Name
                    : WindowFocused.Name,
                false);
        }

        private void UpdateTitleBar(CoreApplicationViewTitleBar coreTitleBar, object args) {
            AppTitleBar.Height = coreTitleBar.Height;
        }

        private void TitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args) {
            AppTitleBar.Visibility = sender.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task SetPickedFile(object sender, RoutedEventArgs e, StorageFile picked, bool autoHash = true) {
            ViewModel.PickedFile = picked;
            ViewModel.Progress = 0.0;
            ViewModel.ShowProgress = false;
            if (autoHash && (await picked.GetBasicPropertiesAsync()).Size < AutoHashSize) {
                HashButton_Click(sender, e);
            }
        }

        private void Page_DragOver(object sender, DragEventArgs e) {
            e.AcceptedOperation = DataPackageOperation.Link;
        }

        private async void Page_Drop(object sender, DragEventArgs e) {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count == 1) {
                    await SetPickedFile(sender, e, items[0] as StorageFile);
                }
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e) {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            var picked = await picker.PickSingleFileAsync();
            if (picked != null) {
                await SetPickedFile(sender, e, picked, autoHash: false);
            }
        }

        private void TransferButton_Click(object sender, RoutedEventArgs e) {
            ViewModel.CompareHash = ViewModel.OutputHash;
            ViewModel.OutputHash = null;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e) {
            if (ViewModel.OutputHash != null) {
                var pkg = new DataPackage();
                pkg.SetText(ViewModel.OutputHash);
                Clipboard.SetContent(pkg);
            }
        }

        private async void PasteButton_Click(object sender, RoutedEventArgs e) {
            ViewModel.CompareHash = await Clipboard.GetContent().GetTextAsync();
        }

        private void HashButton_Click(object sender, RoutedEventArgs e) {
            if (ViewModel.PickedFile == null) {
                return;
            }

            if (!ViewModel.Enabled) {
                ViewModel.CancellationTokenSource?.Cancel();
                return;
            }

            var sf = ViewModel.PickedFile;
            var algoId = ViewModel.SelectedAlgorithm;
            var cts = new CancellationTokenSource();

            ViewModel.Enabled = false;
            ViewModel.Progress = 0;
            ViewModel.ShowProgress = true;
            ViewModel.CancellationTokenSource = cts;

            _ = Windows.System.Threading.ThreadPool.RunAsync(async item => {
                bool success = false;
                var hasher = HashAlgorithmProvider.OpenAlgorithm(algoId).CreateHash();
                try {
                    if (hasher != null) {
                        using var stream = await sf.OpenStreamForReadAsync();
                        var buffer = new byte[BlockSize];

                        while (true) {
                            if (cts.IsCancellationRequested) {
                                break;
                            }
                            var readCount = await stream.ReadAsync(buffer, 0, BlockSize);
                            if (readCount == 0) {
                                success = true;
                                break;
                            } else {
                                hasher.Append(buffer.AsBuffer(0, readCount));
                            }

                            if (stream.Position % HashReportBlockSize == 0) {
                                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => {
                                    ViewModel.Progress = Math.Min(100.0 * stream.Position / stream.Length, 99.9);
                                });
                            }
                        }
                    }
                } catch {
                    success = false;
                } finally {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => {
                        ViewModel.Enabled = true;
                        ViewModel.CancellationTokenSource?.Dispose();
                        ViewModel.CancellationTokenSource = null;
                        if (success) {
                            ViewModel.OutputHash = BitConverter.ToString(hasher.GetValueAndReset().ToArray()).Replace("-", "").ToLower(CultureInfo.InvariantCulture);
                            ViewModel.Progress = 100.0;
                            ViewModel.ShowProgress = true;
                        } else {
                            ViewModel.OutputHash = null;
                            ViewModel.Progress = 0.0;
                            ViewModel.ShowProgress = false;
                        }
                    });
                }
            });
        }

        private void OutputHash_GotFocus(object sender, RoutedEventArgs e) {
            (sender as TextBox)?.SelectAll();
        }
    }
}
