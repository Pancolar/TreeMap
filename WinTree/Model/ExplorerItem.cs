using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeMap.Model
{
    public partial class ExplorerItem : ObservableObject
    {
        //public event PropertyChangedEventHandler PropertyChanged;
        public enum ExplorerItemType { Folder, File };
        public string Path { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public System.Drawing.Color FileColor { get; set; }
        public BitmapImage ItemIcon { get; set; }

        [ObservableProperty]
        public long itemSize;
        public double SizePercentage { get; set; }
        //public string PercentageString { get; set; }
        //private double top { get; set; }
        //private double left { get; set; }
        //private double height { get; set; }
        //private double width { get; set; }
        public double AreaInPX { get; set; }
        public ExplorerItemType Type { get; set; }
        public ExplorerItem Parent { get; set; }
        public ObservableCollection<ExplorerItem> Children { get; set; } = new();

        public double Top { get; set; }
        public double Left { get; set; }
        public double Height { get; set; } //MVVM should be Height
        public double Width { get; set; } //MVVM should be Width
    }
}
