using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.ComponentModel;
using System.Timers;
using System.Windows.Threading;


namespace LiveWeddingAlbum {
    class PhotoManager : INotifyPropertyChanged {
        public const string IMAGE_BLACK = "pack://application:,,,/Images/black.png";
        const int MAX_FILE_SIZE = 10 * 1024 * 1024;
        const int IMPORT_INTERVAL_SEC = 3;

        private System.Windows.Controls.TextBlock _updateTextBlock;
        private bool _copyRecentOnly;

        private String _albumDirectory; // directory to store album photos

        private String _message;

        private List<String> _drivesPresentLastCopy; // drives present last time we check for new drives (to prevent copy twice)
        private MD5 md5Hash;

        private Dictionary<String, FileInfo> _album = new Dictionary<string, FileInfo>(); // list of all photos for display
        private int _albumNewCursor = -1; // division of old vs new files in list

        private Random _rand = new Random();

        private System.Timers.Timer _importTimer = new System.Timers.Timer();

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string propName) {
            if (this.PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion

        public PhotoManager(System.Windows.Controls.TextBlock updateTextBlock, bool copyRecentOnly) {
            _updateTextBlock = updateTextBlock;
            _copyRecentOnly = copyRecentOnly;

            _drivesPresentLastCopy = new List<string>();

            //setup directory - create if necessary
            _albumDirectory = String.Format("{0}\\LiveWeddingAlbum", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));

            DirectoryInfo album = new DirectoryInfo(_albumDirectory);
            album.Create();

            //setup hasher
            md5Hash = MD5.Create();

            //import existing photos in album folder
            LoadFromAlbumDirectory();

            //setup import timer
            _importTimer.Interval = IMPORT_INTERVAL_SEC*1000;
            _importTimer.Elapsed += ImportPhotos;
            _importTimer.Start();
        }

        private void ImportPhotos(object sender, EventArgs e) {
                 _importTimer.Stop();
                CopyPhotosFromDevices();
                _importTimer.Start();
        }

        private void LoadFromAlbumDirectory() {
            DirectoryInfo album = new DirectoryInfo(_albumDirectory);
            FileInfo[] albumFiles = album.GetFiles();

            foreach (FileInfo file in albumFiles) {
                _album.Add(file.Name.Substring(0, file.Name.Length - file.Extension.Length), file);
            }

            UpdateMessage(String.Format("{0} photos in album",_album.Count));

        }

        public bool CopyRecentOnly {
            get { return _copyRecentOnly; }
            set { _copyRecentOnly = value; }
        }

        public String GetNewPhoto() {
            if (_album.Count < 1) {
                return IMAGE_BLACK;
            }
            //increment counter if end not reached - otherwise return a random old photo
            if (_albumNewCursor < _album.Count) {
                _albumNewCursor++;
            }
            if (_albumNewCursor >= _album.Count) {
                return GetOldPhoto();
            }

            if (_album.ElementAt(_albumNewCursor).Value.Exists) {
                return _album.ElementAt(_albumNewCursor).Value.FullName;
            }
            else {
                return IMAGE_BLACK;
            }
        }

        public String GetOldPhoto() {
            if (_album.Count < 1) {
                return IMAGE_BLACK;
            }

            int cursor = _rand.Next(0,(Math.Min(10,Math.Max(_album.Count-1,0))));
            if (_albumNewCursor >= 10) {
                cursor = (int)Math.Floor(_rand.NextDouble()*_albumNewCursor);
            }
            return _album.ElementAt(cursor).Value.FullName;

        }

        public String Message {
            get { return _message; }
        }

        public int AlbumPhotoCount {
            get { return _album.Count; }
        }

        public String AlbumPath {
            get { return _albumDirectory; }
        }

        private void UpdateMessage(String message) {
                _message = message;
                Notify("Message");
        }

        private void CopyPhotosFromDevices() {
            try {
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                List<String> lastDrives = _drivesPresentLastCopy;
                List<string> currentDrives = new List<string>();


                foreach (DriveInfo d in allDrives) {
                    // only get files from drives that are ready and were not present last scan
                    if (d.IsReady == true) {
                        currentDrives.Add(d.Name);
                    }
                }

                // set present last drives to current drives (to provide some thread safety)
                _drivesPresentLastCopy = currentDrives;

                //now copy from those drives that were not present previously

                foreach (string path in currentDrives) {
                    if (!lastDrives.Contains(path)) {
                        CopyFilesFromPath(path);
                    }
                }

            }
            catch (Exception) {
                UpdateMessage("Could not get photos from devices");
            }

        }

        private void CopyFilesFromPath(String path) {
            UpdateMessage(String.Format("Searching {0}",path));
            try {
                List<FileInfo> driveFiles = new List<FileInfo>();

                // get any images on root of the drive
                driveFiles.AddRange(GetFilesRecursive(path, false));

                // get files recursivelly from DCIM folder
                driveFiles.AddRange(GetFilesRecursive(String.Format("{0}\\DCIM", path), true));


                //cull old files from list if using copy recent only
                if (_copyRecentOnly && driveFiles.Count > 0) {
                    //get most recently created file datetime
                    DateTime mostRecent = driveFiles[0].CreationTime;
                    foreach (FileInfo f in driveFiles) {
                        if (f.CreationTimeUtc > mostRecent) {
                            mostRecent = f.CreationTime;
                        }
                    }
                    DateTime cutOff = mostRecent.AddHours(-25);
                    List<FileInfo> recentFiles = new List<FileInfo>();
                    foreach (FileInfo f in driveFiles) {
                        if (f.CreationTimeUtc > cutOff) {
                            recentFiles.Add(f);
                        }
                    }

                    driveFiles = recentFiles;
                }

                //cull non image files and massive images
                String[] imageExtensions = {".jpg",".jpeg",".png",".bmp",".tiff",".tif",".gif" };
                List<FileInfo> validFiles = new List<FileInfo>();
                foreach(FileInfo file in driveFiles){
                    if(imageExtensions.Contains(file.Extension.ToLower()) && file.Length < MAX_FILE_SIZE) {
                        validFiles.Add(file);
                    }
                }
                driveFiles = validFiles;

                int copiedCount = 0;

                //sort the files so we can search for the 

                // now copy and hash each file
                foreach (FileInfo photo in driveFiles) {
                    //create hash string of md5 and file length concat
                    String hash = GetMd5Hash(photo) + photo.Length.ToString();

                    //copy file if not already in album
                    if (!_album.ContainsKey(hash)) {

                        copiedCount++;

                        String newFileLocation = String.Format("{0}\\{1}{2}", _albumDirectory, hash, photo.Extension);
                        //copy file to album directory named as hash string
                        photo.CopyTo(newFileLocation, false);

                        // add new file info to album list.
                        _album.Add(hash,new FileInfo(newFileLocation));
                    }
                }

                UpdateMessage(String.Format("Copied {0} Photos",copiedCount));

            }
            catch (Exception) {
                UpdateMessage( String.Format("Could not get photos from {0}", path) );
            }

        }

        private String GetMd5Hash(FileInfo file) {
            StringBuilder hashTextHex = new StringBuilder();
                // load file and compute hash to byte array
                FileStream fs = file.OpenRead();
                int quarter = 50*1024;

                if (file.Length < quarter) { //work for small files too
                    quarter = (int)file.Length;
                }

                byte[] firstPart = new byte[quarter];
                fs.Read(firstPart, 0, quarter);
                fs.Flush();
                fs.Close();

                byte[] hash = md5Hash.ComputeHash(firstPart);
                // convert bytes to hex string
                for (int i = 0; i < hash.Length; i++) {
                    hashTextHex.Append(hash[i].ToString("x2"));
                }

            return hashTextHex.ToString();
        }

        private List<FileInfo> GetFilesRecursive(String directory, bool recurse) {
            List<FileInfo> allFiles = new List<FileInfo>();

            //create path reference
            DirectoryInfo dir = new DirectoryInfo(directory);
            // if path doesn't exist return empty list
            if (!dir.Exists) {
                return allFiles;
            }

            allFiles.AddRange(dir.GetFiles());

            //if recursive get file from each directory
            if (recurse) {
                DirectoryInfo[] directories = dir.GetDirectories();
                foreach (DirectoryInfo d in directories) {
                    allFiles.AddRange(GetFilesRecursive(d.FullName, true));
                }
            }

            return allFiles;
        }

    }
}
