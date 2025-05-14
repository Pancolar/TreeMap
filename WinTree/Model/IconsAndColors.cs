using Etier.IconHelper;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;

namespace TreeMap.Model
{
    public class IconsAndColors
    {
        public IconsAndColors()
        {
            fileColorsCount = fileColors.Count();
        }
        public BitmapImage folderIcon;
        public List<BitmapImage> fileIcons = new();
        public uint fileIconsCount;
        public int fileColorsCount;
        public static System.Drawing.Color[] fileColors =
        [
            System.Drawing.Color.FromArgb(255, 127, 127, 255),
            System.Drawing.Color.FromArgb(255, 127, 255, 127),
            System.Drawing.Color.FromArgb(255, 255, 127, 127),
            System.Drawing.Color.FromArgb(255, 255, 255, 0),
            System.Drawing.Color.FromArgb(255, 255, 0, 255),
            System.Drawing.Color.FromArgb(255, 0, 255, 255),
            System.Drawing.Color.FromArgb(255, 255, 159, 95),
            System.Drawing.Color.FromArgb(255, 95, 255, 159),
            System.Drawing.Color.FromArgb(255, 95, 159, 255)
        ];
    }
}
