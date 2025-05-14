using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TreeMap.Model
{
    internal class DirectoryDataFetcher
    {
        DirectoryData directoryData = new();
        //List<FileExtensionAndSize> fileExtensionAndSizeList = new();
        Settings settings = Settings.Instance;


        public DirectoryData GetData(string path)
        {
            //DirectoryData directoryData = new(); //TODO Declare here to make method static
            // ADD SELECTED (ROOT) DIRECTORY TO LIST
            ExplorerItem rootFolder = new()
            {
                Path = path,
                Name = new FileInfo(path).Name,
                ItemSize = 0,
                SizePercentage = 1,
                Type = ExplorerItem.ExplorerItemType.Folder,
                Parent = null,
                Children = { },
            };
            directoryData.RootFolder = rootFolder;
            directoryData.FileExtensionAndSizeList = new();
            // FileInfo(path).Name returns "" when path is "C:\"
            if (new DirectoryInfo(path).Root.FullName == path)
            {
                rootFolder.Name = path;
            }
            // ADD SUBRDIRECTORIES AND FILES TO LIST
            RecursivelyPopulateParallel2(rootFolder, path);

            return directoryData;
        }

        private void RecursivelyPopulateParallel2(ExplorerItem rootFolder, string path)
        {
            // FOLDERS
            long directorySize = 0;
            DirectoryInfo di = new(path);
            Parallel.ForEach(di.EnumerateDirectories("*", System.IO.SearchOption.TopDirectoryOnly), item =>
            {
                try
                {
                    ExplorerItem subFolder = new()
                    {
                        Path = item.FullName,
                        Name = item.Name,
                        ItemSize = 0, //RECTIFIED BY RECURSION INTO THIS DIRECTORY (BELOW)
                        Type = ExplorerItem.ExplorerItemType.Folder,
                        Parent = rootFolder,
                        Children = { }
                    };
                    lock (rootFolder.Children)
                    {
                        rootFolder.Children.Add(subFolder);
                    }
                    RecursivelyPopulateParallel2(subFolder, item.FullName);
                    //AFTER POPULATING CHILDREN, DIR SIZE IS KNOWN
                    directorySize += subFolder.ItemSize;
                }
                catch { }
            });

            // FILES
            DirectoryInfo fi = new(path);
            Parallel.ForEach(di.EnumerateFiles("*", System.IO.SearchOption.TopDirectoryOnly), item =>
            {
                try
                {
                    ExplorerItem file = new()
                    {
                        Path = item.FullName,
                        Name = item.Name,
                        Extension = item.Extension.ToLower(),
                        //FileOrFolderSize = 0,
                        ItemSize = GetFileSize(item), //RUN TIME 25%
                        Type = ExplorerItem.ExplorerItemType.File,
                        Parent = rootFolder
                    };
                    directoryData.FileExtensionAndSizeList.Add(new FileExtensionAndSize { type = file.Extension.ToLower(), filesize = file.ItemSize });
                    lock (rootFolder.Children)
                    {
                        rootFolder.Children.Add(file);
                    }
                    directorySize += file.ItemSize;
                }
                catch { }
            });
            // AFTER FILES ARE DONE, ADD TOGETHER SIZE AND ADD TO SIZE OF PARENT
            rootFolder.ItemSize = directorySize;
            // FIND OUT FILE SIZE PERCENTAGES, NOW THAT DIRECTORY SIZE IS KNOWN
            foreach (ExplorerItem item in rootFolder.Children)
            {
                item.SizePercentage = FileOrFolderSizePercentage(item.ItemSize, rootFolder.ItemSize);
            }
            // SORT LIST OF CHILDREN BY THEIR FILE OR FOLDER SIZES
            rootFolder.Children = new ObservableCollection<ExplorerItem>(rootFolder.Children.OrderByDescending(Children => Children.ItemSize).ToList());
        }

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GetCompressedFileSize")]
        static extern uint GetCompressedFileSizeAPI(string lpFileName, out uint lpFileSizeHigh);

        public long GetFileSize(FileInfo fi)
        {
            if (settings.UseFileSizeOnDisk)
            {
                //if (path != "")
                //{
                uint high;
                uint low;
                //TODO TRY USING GetCompressedFileSizeAPIW() here (Unicode version of the function)
                low = GetCompressedFileSizeAPI(fi.FullName, out high); //TODO RETURNS ERROR WHEN SPECIAL UTF8 CHARACTERS ARE USED
                int error = Marshal.GetLastWin32Error();
                if (low == 0xFFFFFFFF && error != 0)
                {
                    //Log("path" + path);
                    //throw new Win32Exception(error);
                    return 0;
                }
                else
                    return ((long)high << 32) + low;
                //}
                //return 0;
            }
            else
            {
                //return new FileInfo(path).Length;
                return fi.Length;
            }
        }

        private double FileOrFolderSizePercentage(long sizeInBytes, long sizeOfParentFolderInBytes)
        {
            if (sizeOfParentFolderInBytes != 0)
            {
                return (double)sizeInBytes / sizeOfParentFolderInBytes;
            }
            else { return 1; }
        }

        public static void OrderExtensionsBySize(DirectoryData directoryData)
        {
            Debug.WriteLine(directoryData.FileExtensionAndSizeList.Count); //TODO Throws exception sometimes.
            directoryData.FileExtensionsOrderedBySizeList = directoryData.FileExtensionAndSizeList.GroupBy(i => i.type).Select(j => new FileExtensionAndSize { type = j.Key, filesize = j.Sum(k => k.filesize) }).OrderByDescending(l => l.filesize).ToList();
            directoryData.IconsAndColors.fileIconsCount = (uint)Math.Min(directoryData.FileExtensionsOrderedBySizeList.Count, IconsAndColorsManager.MaxFileIconCount);
        }
    }
}
