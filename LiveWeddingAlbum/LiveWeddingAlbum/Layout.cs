using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveWeddingAlbum {
    public enum LayoutType : int {
        One = 1,
        Two = 2,
        Four = 4,
        Six = 6,
        Nine = 9,
        Sixteen = 16
    }

    class Layout {

        double _screenWidth, _screenHeight;
        double _photoWidth, _photoHeight;
        LayoutType _type;
        Point[] _points;
        int _cursor;
        Random _rand = new Random();


        public Layout(double screenWidth, double screenHeight, LayoutType type) {
            _type = type;
            _screenHeight = screenHeight;
            _screenWidth = screenWidth;


            switch (type) {
                case LayoutType.One:
                    _photoWidth = _screenWidth;
                    _photoHeight = _screenHeight;
                    _points = new[] {
                                MakePoint(0,0,false,false) 
                                    };
                    break;
                case LayoutType.Two:
                    _photoWidth = _screenWidth/2;
                    _photoHeight = _screenHeight;
                    _points = new[] {
                                new Point(0,0,false,true,_screenHeight/2,_screenWidth/2),
                                MakePoint(0,0,false,false),
                                MakePoint(0,1,false,false)
                                    };
                    break;
                default:
                case LayoutType.Four:
                    _photoWidth = _screenWidth/2;
                    _photoHeight = screenHeight/2;
                    _points = new[] {
                                MakePoint(0,0,true,true),
                                MakePoint(0,1,true,false),
                                MakePoint(0,0,true,false),
                                MakePoint(1,1,false,false),
                                MakePoint(1,0,false,false)

                                    };
                    break;
                case LayoutType.Six:
                    _photoWidth = _screenWidth/3;
                    _photoHeight = screenHeight/2;
                    _points = new[] {
                                MakePoint(0,0,true,true),
                                MakePoint(0,2,true,false),
                                MakePoint(1,2,false,false),
                                MakePoint(0,0,true,false),
                                MakePoint(1,0,false,false),
                                MakePoint(0,1,true,true),
                                MakePoint(0,0,true,false),
                                MakePoint(1,0,true,false),
                                MakePoint(0,2,true,false),
                                MakePoint(1,2,true,false)

                                    };
                    break;
                case LayoutType.Nine:
                    _photoWidth = _screenWidth/3;
                    _photoHeight = screenHeight/3;
                    _points = new[] {

                                MakePoint(0,0,true,true),
                                MakePoint(0,2,true,false),
                                MakePoint(2,0,false,false),


                                MakePoint(1,1,true,true),
                                MakePoint(0,1,true,false),
                                MakePoint(0,2,true,false),

                                
                                MakePoint(1,0,true,true),
                                MakePoint(0,0,true,false),                                
                                MakePoint(2,2,true,false),                                


                                MakePoint(0,1,true,true),
                                MakePoint(2,0,false,false),
                                MakePoint(2,1,false,false),
                                    };
                    break;
                case LayoutType.Sixteen:
                    _photoWidth = _screenWidth / 4;
                    _photoHeight = screenHeight / 4;
                    _points = new[] {

                                MakePoint(3,3,false,false),
                                MakePoint(2,3,true,false),
                                MakePoint(1,3,true,false),
                                MakePoint(0,3,true,false),

                                MakePoint(3,2,false,false),
                                MakePoint(2,2,true,true),
                                MakePoint(1,2,true,true),
                                MakePoint(0,2,true,true),

                                MakePoint(3,1,false,false),
                                MakePoint(2,1,true,true),
                                MakePoint(1,1,true,true),
                                MakePoint(0,1,true,true),

                                MakePoint(3,0,false,false),
                                MakePoint(2,0,true,true),
                                MakePoint(1,0,true,true),
                                MakePoint(0,0,true,true)

                                    };
                    break;
            }
        }

        private Point MakePoint(double top, double left, bool portrait, bool big) {
            return new Point(top, left, portrait, big, _photoHeight,_photoWidth );
        }

        private int IncrementCursor() {
            _cursor = (_cursor + 1) % _points.Length;
            return _cursor;
        }

        private Point GetSmall() {
            //increment cursor and return point at new cursor position
            return _points[IncrementCursor()];
        }

        private Point GetBig() {
            //search for next big capable point
            for (int p = 0; p < _points.Length; p++) {
                if (_points[IncrementCursor()].Big) {
                        return _points[_cursor];
                }
            }
            //if no big points found then return nothing.
            return null;
        }


        private Point GetPortrait() {


            //search for next big capable point
            for (int p = 0; p < _points.Length; p++) {
                if (_points[IncrementCursor()].Portrait) {
                        return _points[_cursor];
                }
            }
            //if no portrait points found then return a small one.
            return null;
        }

        public Point GetPoint(PhotoType type) {
            Point thePoint = null;
            switch (type) { 
                case PhotoType.Big:
                    thePoint = GetBig();
                    break;
                case PhotoType.Portrait:
                    thePoint = GetPortrait();
                    break;
                case PhotoType.Notify:
                    double notifySize = _screenWidth/2;
                    double notifyHeight = 200; //just have to hope that the broder is less than 66 pixels
                    thePoint = new Point((_screenHeight - notifyHeight) / notifySize, 0.0001, false, false, notifySize, notifySize);
                    break;
            }

            if (thePoint == null) {
                thePoint = GetSmall();
            }

            return thePoint;
        
        }



    }

    class Point {
        public double Top, Left;
        public bool Portrait, Big;
        public double MaxWidth, MaxHeight;

        public Point(double top, double left, bool portrait, bool big, double height, double width) {
            Top = Math.Floor(top*height);
            Left = Math.Floor(left*width);
            Portrait = portrait;
            Big = big;

            //we can only determine the width image should be
            MaxWidth = width;
            MaxHeight = height;

        }

    }

}
