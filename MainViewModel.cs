using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MyWpfApp.Services;

// ✅ Fix CS0234: شيل using MyWpfApp.Models لأن Product و AppVersion في MyWpfApp
namespace MyWpfApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _db;
        private readonly UpdateService   _updater;

        private ObservableCollection<Product> _products = new();
        public ObservableCollection<Product> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "جاري الاتصال...";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string _updateBadge = "";
        public string UpdateBadge
        {
            get => _updateBadge;
            set { _updateBadge = value; OnPropertyChanged(); }
        }

        private AppVersion? _pendingUpdate;

        public string AppVersion => $"الإصدار {UpdateService.CurrentVersion}";

        public ICommand RefreshCommand   { get; }
        public ICommand InstallUpdateCmd { get; }

        public MainViewModel()
        {
            _db      = new DatabaseService();
            _updater = new UpdateService();

            RefreshCommand   = new RelayCommand(async _ => await LoadProductsAsync());
            InstallUpdateCmd = new RelayCommand(async _ => await InstallPendingUpdateAsync());

            _updater.UpdateAvailable  += OnUpdateAvailable;
            _updater.NoUpdateFound    += () => StatusMessage = $"✅ أحدث إصدار  |  {DateTime.Now:HH:mm}";
            _updater.CheckFailed      += err => StatusMessage = $"⚠️ تعذّر الفحص: {err}";
            _updater.DownloadProgress += pct => StatusMessage = $"⬇️ جاري التحديث... {pct}%";

            _ = LoadProductsAsync();
        }

        private async Task LoadProductsAsync()
        {
            IsLoading     = true;
            StatusMessage = "⏳ جاري جلب البيانات...";

            try
            {
                var data = await _db.GetProductsAsync();

                Products.Clear();
                foreach (var p in data)
                    Products.Add(p);

                StatusMessage = $"✅ تم تحميل {data.Count} منتج  |  {DateTime.Now:HH:mm}";
                await _db.LogActivityAsync("LOAD_PRODUCTS", $"تم تحميل {data.Count} منتجات");
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ خطأ في الاتصال: {ex.Message}";
                MessageBox.Show(
                    $"تعذّر الاتصال بقاعدة البيانات:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnUpdateAvailable(AppVersion update)
        {
            _pendingUpdate = update;
            UpdateBadge    = $"🔔 تحديث جديد {update.Version}";

            var result = MessageBox.Show(
                $"تحديث جديد متوفر!\n\n" +
                $"الإصدار الجديد: {update.Version}\n" +
                $"الإصدار الحالي: {UpdateService.CurrentVersion}\n\n" +
                $"ملاحظات: {update.ReleaseNotes}\n\n" +
                $"هل تريد التحديث الآن؟",
                "تحديث متوفر 🚀",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
                _ = InstallPendingUpdateAsync();
        }

        private async Task InstallPendingUpdateAsync()
        {
            if (_pendingUpdate == null) return;

            try
            {
                StatusMessage = "⬇️ جاري تحميل التحديث...";
                await _updater.DownloadAndInstallAsync(_pendingUpdate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل التحديث:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "❌ فشل التحديث";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private bool _isExecuting;

        public RelayCommand(Func<object?, Task> executeAsync) => _executeAsync = executeAsync;

        public bool CanExecute(object? parameter) => !_isExecuting;

        public async void Execute(object? parameter)
        {
            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try   { await _executeAsync(parameter); }
            finally
            {
                _isExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? CanExecuteChanged;
    }
}
