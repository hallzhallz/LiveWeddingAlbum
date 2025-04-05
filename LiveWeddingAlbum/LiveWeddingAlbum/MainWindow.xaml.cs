using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Timers;
using System.Windows.Interop;
using System.Windows.Threading;
using System.ComponentModel;
using System.IO;

namespace LiveWeddingAlbum {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {



        //constants
        public const string IMAGE_BLACK = "pack://application:,,,/Images/black.png";
        public const string IMAGE_WHITE = "pack://application:,,,/Images/white.png";
        //disable screensaver constants http://www.pixvillage.com/blogs/devblog/archive/2007/02/27/6493.aspx
        private const int WM_SYSCOMMAND = 0x112;
        private const int SC_SCREENSAVE = 0xF140;
        private const int SC_MONITORPOWER = 0xF170;
        // notify time (multiplied by 3 by addphoto)
        const double NOTIFY_TIME = 2;     //time to display photo copying messages for

        // private vars
        private List<PhotoWindow> _windows = new List<PhotoWindow>();
        private int _photoCount = 0;    //count to determine if should display big photo
        private double _photoDisplaySec = 20;   // approx time to display each image MIN 10 SEC
        private double _photoAddRate;   //rate at which to add new photos
        private int _nextBigPhoto = 2;   //after how many old photos to display a big photo
        private PhotoManager _photoManager;
        private Random _rand = new Random();
        private BitmapImage _logo, _imgWhite, _imgBlack;
        private LayoutType _currentLayout;

        private DispatcherTimer _addPhotoTimer = new DispatcherTimer(DispatcherPriority.Render);

        public MainWindow() {


            // handle screen saver and monitor power events
            SourceInitialized += delegate {
                HwndSource source = (HwndSource)
                PresentationSource.FromVisual(this);
                source.AddHook(Hook);
            };

            // load manager
            _photoManager = new PhotoManager(tbStatus, Properties.Settings.Default.CopyRecentOnly);

            try {
                InitializeComponent();
            }
            catch (Exception ex) {

                throw ex;
            }

            //init 
            _imgWhite = GetImage(IMAGE_WHITE);
            _imgBlack = GetImage(IMAGE_BLACK);

            // get set images on mainwindow ui
            LoadAllFromSettings();

        }

        // screen saver event handler override (stop screen saver)
        private static IntPtr Hook(IntPtr hwnd,
            int msg, IntPtr wParam, IntPtr lParam,
            ref bool handled) {
            if (msg == WM_SYSCOMMAND &&
                ((((long)wParam & 0xFFF0) == SC_SCREENSAVE) ||
                ((long)wParam & 0xFFF0) == SC_MONITORPOWER))
                handled = true;
            return IntPtr.Zero;
        }

        private void bStart_Click(object sender, RoutedEventArgs e) {

            // hide the mouse
            Mouse.OverrideCursor = System.Windows.Input.Cursors.None;

            // setup logo bitmap
            _logo = GetImage(Properties.Settings.Default.logoPath);

            //reset count
            _photoCount = 0;

            //get the set layout type (default to four layout if operation fails)
            bool gotSetting = Enum.TryParse<LayoutType>(Properties.Settings.Default.LayoutType.ToString(), true, out _currentLayout);
            if (!gotSetting) { _currentLayout = LayoutType.Four; }

            // add window for each screen
            foreach (Screen screen in Screen.AllScreens) {
                //create window with size of screen


                //create window
                PhotoWindow thisWindow = new PhotoWindow(screen.Bounds.Height, screen.Bounds.Width, GetImage(Properties.Settings.Default.backgroundPath), _currentLayout);
                thisWindow.Left = screen.WorkingArea.Left;
                thisWindow.Top = screen.WorkingArea.Top;
                thisWindow.Width = screen.Bounds.Width;
                thisWindow.Height = screen.Bounds.Height;

                // add key down listener
                thisWindow.KeyUp += new System.Windows.Input.KeyEventHandler(PhotoWindow_KeyUp);

                // add window to collection and show it
                _windows.Add(thisWindow);
                thisWindow.Show();

                // if not multi monitor selected then exit for
                if (Properties.Settings.Default.MultipleMonitors == false) {
                    break;
                }

            }

            //attach to photomanager message event
            _photoManager.PropertyChanged += PhotoManagerEvent;

            // if this rate is too fast then the program will eat memory then crash.
            _photoAddRate = Math.Max(Math.Ceiling(_photoDisplaySec / ((int)_currentLayout / 2)),3);

            //init and start timer
            _addPhotoTimer.Interval = new TimeSpan((int)Math.Floor(_photoAddRate * TimeSpan.TicksPerSecond));
            _addPhotoTimer.Tick += _addPhotoTimer_Tick;
            _addPhotoTimer.Start();

            DisplayNotification("Starting...");

        }

        void _addPhotoTimer_Tick(object sender, EventArgs e) {
            AddPhoto();
        }

        void AddPhoto() {
            if ((_photoCount % _nextBigPhoto) == 0) {
                _photoCount = 0;
                _nextBigPhoto = GetNextBigPhotoNumber(_currentLayout);
                AddBigPhoto();
            }
            else {
                AddSmallPhoto();
            }
            _photoCount++;
        }

        int GetNextBigPhotoNumber(LayoutType type) {
            switch(type) {
                case LayoutType.One:
                case LayoutType.Two:
                case LayoutType.Four:
                    return ((int)type + 1);
                case LayoutType.Six:
                    return 3;
                default:
                    return (int)Math.Ceiling(Math.Sqrt((int)type));
            }
        }

        void AddBigPhoto() {
            foreach (PhotoWindow pw in _windows) {
                pw.AddPhoto(GetImage(_photoManager.GetNewPhoto()), _logo, "", PhotoType.Big, _photoDisplaySec+1);
            }
        }

        void AddSmallPhoto() {
            foreach (PhotoWindow pw in _windows) {
                pw.AddPhoto(GetImage(_photoManager.GetOldPhoto()), _logo, "", PhotoType.Small, _photoDisplaySec+1);
            }
        }

        void PhotoManagerEvent(object sender, PropertyChangedEventArgs a) {
                    if (Properties.Settings.Default.ShowNotifications) {
                        if (a.PropertyName == "Message") {
                            //tell gui thread to update
                            Dispatcher.BeginInvoke( new Action(() => 
                            {
                                DisplayNotification(_photoManager.Message);
                                UpdateMainWindowPhotoCount();                           
                            }), DispatcherPriority.Render);
                        }
                    }
        }

        void DisplayNotification(String message) {
            if (_windows.Count > 0) {
                foreach (PhotoWindow pw in _windows) {
                    pw.AddPhoto(_imgWhite, _logo, message, PhotoType.Notify, NOTIFY_TIME);
                }
            }
        }

        private void UpdateMainWindowPhotoCount() {
            tbStatus.Text = String.Format("{0} Photos in album {1}.", _photoManager.AlbumPhotoCount, _photoManager.AlbumPath);
        }


        void LoadAllFromSettings() {

            if (String.IsNullOrEmpty(Properties.Settings.Default.backgroundPath)) {
                Properties.Settings.Default.backgroundPath = IMAGE_BLACK;
            }

            if (String.IsNullOrEmpty(Properties.Settings.Default.logoPath)) {
                Properties.Settings.Default.logoPath = IMAGE_WHITE;
            }

            SetImage(iBackground, Properties.Settings.Default.backgroundPath);
            SetImage(iLogo, Properties.Settings.Default.logoPath);

            //update status directory and count
            UpdateMainWindowPhotoCount();

        }

        bool SetImage(System.Windows.Controls.Image img, String path) {
            try {
                img.Source = GetImage(path);
                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        private BitmapImage GetImage(String path) {

            BitmapImage pathImg = new BitmapImage();

            if (String.IsNullOrEmpty(path)) {
                return GetImage(IMAGE_BLACK);
            }

            if (path != IMAGE_BLACK && path != IMAGE_WHITE) {
                //if not exists
                FileInfo checkExists = new FileInfo(path);
                if (!checkExists.Exists) {
                    return GetImage(IMAGE_BLACK);
                }
            }

            //now make a bitmap image
            pathImg.BeginInit();
            pathImg.CacheOption = BitmapCacheOption.None;
            //pathImg.UriCachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            pathImg.UriSource = new Uri(path);
            pathImg.EndInit();
            pathImg.Freeze(); // meant to help with memory

            return pathImg;
        }

        void PhotoWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                _addPhotoTimer.Stop();
                _addPhotoTimer.Tick -= _addPhotoTimer_Tick;

                for (int pw = 0; pw < _windows.Count; pw++) {
                    _windows[pw].Close();
                    _windows[pw] = null;
                }

                _windows.Clear();
                GC.Collect();
                Mouse.OverrideCursor = null;

                _photoManager.PropertyChanged -= PhotoManagerEvent;

            }

        }

        void HandleRequestNavigate(object sender, RoutedEventArgs e) {
            string navigateUri = hl.NavigateUri.ToString();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(navigateUri)); e.Handled = true;
        }

        private void bSelectBackground_Click(object sender, RoutedEventArgs e) {
            // select background image
            Properties.Settings.Default.backgroundPath = GetImagePathFromDialog(Properties.Settings.Default.backgroundPath);
            Properties.Settings.Default.Save();

            LoadAllFromSettings();

        }

        private void bSelectLogo_Click(object sender, RoutedEventArgs e) {
            // select logo image
            Properties.Settings.Default.logoPath = GetImagePathFromDialog(Properties.Settings.Default.logoPath);
            Properties.Settings.Default.Save();

            LoadAllFromSettings();
        }

        private string GetImagePathFromDialog(string ifNullString) {
            // returns the path selected in the dialog or the ifnullstring
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "Image Files(*.bmp;*.jpg;*.gif;*.png)|*.bmp;*.jpg;*.gif;*.png|All files (*.*)|*.*";
            ofd.FilterIndex = 0;
            DialogResult dialogResponse = ofd.ShowDialog();
            if (dialogResponse == System.Windows.Forms.DialogResult.OK) {
                //System.Windows.MessageBox.Show(ofd.FileName);
                return ofd.FileName;
            }
            else {
                return ifNullString;
            }
        }

        private void bNoBackground_Click(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.backgroundPath = IMAGE_BLACK;
            Properties.Settings.Default.Save();
            LoadAllFromSettings();
        }

        private void bNoLogo_Click(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.logoPath = IMAGE_WHITE;
            Properties.Settings.Default.Save();
            LoadAllFromSettings();
        }

        private void SaveSettings(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.Save();
            _photoManager.CopyRecentOnly = Properties.Settings.Default.CopyRecentOnly;
        }



    }
}
