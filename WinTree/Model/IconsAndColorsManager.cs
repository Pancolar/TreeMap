using Etier.IconHelper;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeMap.Model
{
    internal class IconsAndColorsManager
    {
        public const uint MaxFileIconCount = 300;

        public static void FetchIcons(DirectoryData directoryData)
        {
            FileIconsSetup(directoryData);
            FileIconsStreamAsync(directoryData);
            FolderIconSetup(directoryData);
            FolderIconStreamAsync(directoryData);
        }

        public static void MapToExplorerItems(DirectoryData directoryData)
        {
            RecurseMapToExplorerItem(directoryData, directoryData.RootFolder);
        }

        private static void RecurseMapToExplorerItem(DirectoryData directoryData, ExplorerItem currentFolder)
        {
            //rootFolder.ItemIcon = FolderIcon;
            foreach (ExplorerItem item in currentFolder.Children)
            {
                if (item.Type == ExplorerItem.ExplorerItemType.File)
                {
                    int extensionIndex = directoryData.FileExtensionsOrderedBySizeList.FindIndex(i => i.type == item.Extension);
                    if (extensionIndex == -1)
                    {
                        Debug.WriteLine("Exception in MapExtensionColors for file " + item.Extension);
                    }
                    else if (extensionIndex < directoryData.IconsAndColors.fileColorsCount) //Item is one of the main extensions and receives a color.
                    {
                        try
                        {
                            item.FileColor = IconsAndColors.fileColors[extensionIndex];
                        }
                        catch //(Exception ex)
                        {
                            item.FileColor = System.Drawing.Color.FromArgb(255, 255, 255, 255);
                            Debug.WriteLine(extensionIndex + " " + directoryData.IconsAndColors.fileColorsCount);
                            Debug.WriteLine(item.Path);
                        }
                        if (extensionIndex < directoryData.IconsAndColors.fileIconsCount)
                        {
                            item.ItemIcon = directoryData.IconsAndColors.fileIcons[extensionIndex];
                        }
                    }
                    else
                    {
                        item.FileColor = System.Drawing.Color.FromArgb(255, 170, 170, 170);
                        if (extensionIndex < directoryData.IconsAndColors.fileIconsCount)
                        {
                            item.ItemIcon = directoryData.IconsAndColors.fileIcons[extensionIndex];
                        }
                    }
                }
                if (item.Type == ExplorerItem.ExplorerItemType.Folder)
                {
                    item.ItemIcon = directoryData.IconsAndColors.folderIcon;
                    RecurseMapToExplorerItem(directoryData, item);
                }
            }
            directoryData.RootFolder.ItemIcon = directoryData.IconsAndColors.folderIcon;
        }

        private static void FileIconsSetup(DirectoryData directoryData)
        {
            int j = Math.Min(directoryData.FileExtensionsOrderedBySizeList.Count, 300);
            for (int i = 0; i < j; i++)
            {
                BitmapImage bitmapImage = new();
                directoryData.IconsAndColors.fileIcons.Add(bitmapImage);
            }
        }

        private static async void FileIconsStreamAsync(DirectoryData directoryData)
        {
            Bitmap tempBitmap;

            int j = Math.Min(directoryData.FileExtensionsOrderedBySizeList.Count, 300);
            for (int i = 0; i < j; i++)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    tempBitmap = Etier.IconHelper.IconReader.GetFileIcon(directoryData.FileExtensionsOrderedBySizeList[i].type, IconReader.IconSize.Large, false).ToBitmap();

                    tempBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Position = 0;
                    await directoryData.IconsAndColors.fileIcons[i].SetSourceAsync(stream.AsRandomAccessStream());
                }
            }
        }

        private static void FolderIconSetup(DirectoryData directoryData)
        {
            BitmapImage bitmapImage = new();
            directoryData.IconsAndColors.folderIcon = bitmapImage;
        }

        private static async void FolderIconStreamAsync(DirectoryData directoryData)
        {
            Bitmap tempBitmap;

            using (MemoryStream stream = new MemoryStream())
            {
                tempBitmap = Etier.IconHelper.IconReader.GetFolderIconEx().ToBitmap();

                tempBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                await directoryData.IconsAndColors.folderIcon.SetSourceAsync(stream.AsRandomAccessStream());
            }
        }
    }
}
