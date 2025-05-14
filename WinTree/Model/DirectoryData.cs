using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeMap.Model
{
    public class DirectoryData
    {
        public DirectoryData()
        {
            IconsAndColors = new();
        }
        public ExplorerItem RootFolder {  get; set; }
        public List<FileExtensionAndSize> FileExtensionAndSizeList { get; set; }
        public List<FileExtensionAndSize> FileExtensionsOrderedBySizeList { get; set; }
        public IconsAndColors IconsAndColors { get; set; }
    }
}
