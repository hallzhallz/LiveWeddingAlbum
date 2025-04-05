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
using System.Windows.Shapes;

namespace LiveWeddingAlbum {
    /// <summary>
    /// Interaction logic for PhotoWindow.xaml
    /// </summary>
    public partial class PhotoWindow : Window {

        const double SIZE_RANDOM_PERCENT = 0.05;
        const double RAND_ANGLE_MAX_DEG = 30;
        const double SHRINK_PHOTO = 0.9;

        private Random _rand = new Random();

        private int _renderHeight;
        private int _renderWidth;

        private Layout _layout;

        private BitmapImage _background;


        public PhotoWindow(int height, int width, BitmapImage background, LayoutType layoutType) {
            InitializeComponent();

            _renderWidth = width;
            _renderHeight = height;
            _background = background;

            imgBackground.Source = background;

            _layout = new Layout(_renderWidth,_renderHeight, layoutType);

        }

        public void AddPhoto(BitmapImage photo, BitmapImage logo, String label, PhotoType type , double displaySeconds) {

            Photo newPhoto = new Photo(photo, logo,0.5,displaySeconds*3, label,(type == PhotoType.Notify));

            if (newPhoto.ImgWidth < newPhoto.ImgHeight) {
                type = PhotoType.Portrait;
                //newPhoto.tbLabel.Text += "Portrait\n";
            }


            Point myPosition = _layout.GetPoint(type);

            double maxWidth = myPosition.MaxWidth;
            double maxHeight = myPosition.MaxHeight;

            // set larger sizes (if photo should be big or portrait and position supplied is of that size)
            if (type == PhotoType.Big && myPosition.Big) {
                maxHeight = maxHeight * 2;
                maxWidth = maxWidth * 2;
            } else if(type == PhotoType.Portrait && myPosition.Portrait){
                maxHeight = maxHeight * 2;
            }
            

            //newPhoto.tbLabel.Text += "Max: " + maxWidth + " x " + maxHeight;

            // if destination is portrait size using width, if dest is landscape size using height.
            if ((newPhoto.ImgWidth/newPhoto.ImgHeight) > (maxWidth / maxHeight)) {
                newPhoto.ImgWidth = maxWidth * SHRINK_PHOTO;
            }
            else {
                newPhoto.ImgHeight = maxHeight * SHRINK_PHOTO;
            }
            //newPhoto.tbLabel.Text += "\nSet: " + newPhoto.ImgWidth + " x " + newPhoto.ImgHeight;

            //set position taking into account reduced size (in order to center the image)
            double widthGap = (maxWidth - newPhoto.ImgWidth) / 2;
            double heightGap = (maxHeight - newPhoto.ImgHeight) / 2;
            double posLeft = myPosition.Left - newPhoto.bPhoto.BorderThickness.Left + widthGap;
            double posTop = myPosition.Top - newPhoto.bPhoto.BorderThickness.Top + heightGap;


            //add position randomness magnitude determined by extra space made from scaling
            posLeft = posLeft + ((_rand.NextDouble() * widthGap)-(widthGap/2));
            posTop = posTop + ((_rand.NextDouble() * heightGap)-(heightGap/2));

            // set position
            Canvas.SetLeft(newPhoto, Math.Floor(posLeft));
            Canvas.SetTop(newPhoto, Math.Floor(posTop));

            // apply random angle
            newPhoto.Angle = (_rand.NextDouble() * RAND_ANGLE_MAX_DEG) - (RAND_ANGLE_MAX_DEG / 2);

            // attach to remove time event to remove photo control
            newPhoto.RemoveTime += RemovePhoto;
            this.mainGrid.Children.Add(newPhoto);

        }

        public void RemovePhoto(object sender, EventArgs e) {
            this.mainGrid.Children.Remove((Control)sender);
        }

    }
}
