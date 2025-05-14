using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeMap.Model
{
    public class SelectionRectangle
    {
        public SelectionRectangle(int _height, int _width, int _top, int _left)
        {
            Height = _height;
            Width = _width;
            Top = _top;
            Left = _left;
        }
        public int Height;
        public int Width;
        public int Top;
        public int Left;
    }
}
