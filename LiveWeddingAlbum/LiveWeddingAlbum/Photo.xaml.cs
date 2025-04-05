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
using System.Timers;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace LiveWeddingAlbum {

    public enum PhotoType {
        None,
        Big,
        Small,
        Notify,
        Portrait
    }


    /// <summary>
    /// Interaction logic for Photo.xaml
    /// </summary>
    public partial class Photo : UserControl {

        private TimeSpan _effectDuration;
        private TimeSpan _beginRemoveEffectTime;
        private DispatcherTimer _removeTimer = new DispatcherTimer(DispatcherPriority.Send);
        private DispatcherTimer _removeEffectTimer = new DispatcherTimer(DispatcherPriority.Render);
        private bool _hidePhoto;
        private BitmapImage _photo;


        public event EventHandler RemoveTime;

        public Photo(BitmapImage photo, BitmapImage logo, double effectTimeSec, double showTimeSec, string text, bool hidePhoto) {
            InitializeComponent();

            _photo = photo;
            _hidePhoto = hidePhoto;
            if (_hidePhoto) {
                iPhoto.Visibility = System.Windows.Visibility.Collapsed;
            }

            // set images
            iPhoto.Source = photo;
            iLogo.Source = logo;

            // set text
            tbLabel.Text = text;

            showTimeSec = showTimeSec + 2; // add the two seconds the fade waits for to load.

            // animation times
            _effectDuration = new TimeSpan((int)Math.Floor(effectTimeSec * TimeSpan.TicksPerSecond));
            _beginRemoveEffectTime = new TimeSpan((int)Math.Floor((showTimeSec - (effectTimeSec + 0.5))*TimeSpan.TicksPerSecond));


            // remove event timer
            _removeTimer.Interval = new TimeSpan((int)Math.Floor(showTimeSec*TimeSpan.TicksPerSecond));
            _removeTimer.Tick += RemoveTimerTriggered;

            //remove effect event timer
            _removeEffectTimer.Interval = _beginRemoveEffectTime;
            _removeEffectTimer.Tick += RemoveEffectTimerTriggered;

            //start timers
            //Dispatcher.BeginInvoke((Action)(()=>_removeTimer.Start()));
            //Dispatcher.BeginInvoke((Action)(()=>_removeEffectTimer.Start()));
            _removeTimer.Start();
            _removeEffectTimer.Start();

        }

        private void RemoveTimerTriggered(object sender, EventArgs e) {
                _removeTimer.Stop();

                // null and dispse anything that might use memory
                _photo = null;
                iPhoto.Source = null;
                iLogo.Source = null;

                GC.Collect(); //force garbage collection

                RemoveTime(this, e);// raise remove time event
        }

        private void RemoveEffectTimerTriggered(object sender, EventArgs e) {
                _removeEffectTimer.Stop();
                // show remove effect
                Storyboard sRemoveEffect = (Storyboard)FindResource("sRemoveEffect");
                sRemoveEffect.Begin(this);
        }

        public double ImgWidth {
            get {
                if (iPhoto.Width != iPhoto.Width) { //if is NaN
                    return iPhoto.Source.Width;
                }
                else {
                    return iPhoto.Width;
                }
            }
            set {
                // set photo
                double aspect = iPhoto.Source.Width / iPhoto.Source.Height;
                iPhoto.Width = Math.Floor(value);
                iPhoto.Height = Math.Floor(value / aspect);
                AdjustLogoAndLabel();
            }
        }

        public double ImgHeight {
            get {
                if (iPhoto.Height != iPhoto.Height) { //if is NaN
                    return iPhoto.Source.Height;
                }
                else {
                    return iPhoto.Height;
                }

            }
            set {
                double aspect = iPhoto.Source.Height / iPhoto.Source.Width;
                iPhoto.Height = Math.Floor(value);
                iPhoto.Width = Math.Floor(value / aspect);
                AdjustLogoAndLabel();
            }
        }

        public double Angle {
            get { return ((RotateTransform)this.RenderTransform).Angle; }
            set { this.RenderTransform = new RotateTransform(value, ((iPhoto.Width + (2 * bPhoto.BorderThickness.Left)) / 2), ((iPhoto.Height + (2 * bPhoto.BorderThickness.Top)) / 2)); }
        }

        public TimeSpan EffectDuration {
            get { return _effectDuration; }
            set { _effectDuration = value; }
        }

        public TimeSpan BeginRemoveEffectTime {
            get { return _beginRemoveEffectTime; }
            set { _beginRemoveEffectTime = value; }
        }

        private void AdjustLogoAndLabel() {
            double borderThickness = bPhoto.BorderThickness.Top;

            double newHeight = borderThickness*3;
            double aspect = iLogo.Source.Height / iLogo.Source.Width;
            iLogo.Height = Math.Floor(newHeight);
            iLogo.Width = Math.Floor(newHeight / aspect);

            double logoAndLabelMarginTop = 0;
            if (!_hidePhoto) {
                logoAndLabelMarginTop = iPhoto.Height + borderThickness;
            }

            gLogoAndLabel.Width = bPhoto.Width;
            gLogoAndLabel.Margin = new Thickness(0,logoAndLabelMarginTop,0,0);

            double logoWidthSpace = iLogo.Width + borderThickness;

            vbLabel.Margin = new Thickness(logoWidthSpace, 0, 0, 0);
            vbLabel.Width = iPhoto.Width - logoWidthSpace;
            vbLabel.Height = iLogo.Height;
        }


    }

   


}
