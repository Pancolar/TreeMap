using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage.Search;
using System.Numerics;
using System.Collections.ObjectModel;

namespace TreeMap.Model
{
    internal partial class Graphing : ObservableObject
    {
        // Shading 
        const int lightHeight = 50;
        const float ambientLight = 0.2f;
        private static readonly Vector3 surfaceNormal = new Vector3(0, 0, 1);

        public static void GraphInit(GraphingData graphingDataInstance, DirectoryData directoryDataInstance, double bitmapHeight, double bitmapWidth)
        {
            graphingDataInstance.BitmapHeight = bitmapHeight;
            graphingDataInstance.BitmapWidth = bitmapWidth;
            if (graphingDataInstance.GraphRootFolder == null)
            {
                Debug.WriteLine("No GraphRootFolder to graph.");
            }
            ExplorerItem folder = graphingDataInstance.GraphRootFolder;
            if (folder != null && !graphingDataInstance.IsGraphing)
            {
                graphingDataInstance.IsGraphing = true;
                folder.Top = 0;
                folder.Left = 0;
                folder.Height = graphingDataInstance.BitmapHeight;
                folder.Width = graphingDataInstance.BitmapWidth;
                folder.AreaInPX = graphingDataInstance.BitmapHeight * graphingDataInstance.BitmapWidth;
                GraphRecurseArrangeFiles(folder);
                GraphDrawWin2DInit(graphingDataInstance, folder);
            }
            else
            {
                graphingDataInstance.GraphInvalid = false;
            }
        }

        private static void GraphRecurseArrangeFiles(ExplorerItem folder)
        {
            // ARRANGE CHILDREN IN THE DIRECTORIES BY SIZE FOR EACH SUBDIRECTORY RECURSIVELY
            GraphArrangeChildren(folder);
            foreach (ExplorerItem item in folder.Children)
            {
                if (item.Type == ExplorerItem.ExplorerItemType.Folder)
                {
                    GraphRecurseArrangeFiles(item);
                }
            }
        }

        private static void GraphArrangeChildren(ExplorerItem folder)
        {
            List<Row> rows = new();
            int childrenUsed = 0;
            while (childrenUsed < folder.Children.Count)
            {
                Row row = GraphArrangeRowOfChildren(folder, childrenUsed);
                childrenUsed += row.ChildrenUsed;
                rows.Add(row);
            }
            GraphCalcRowCoordinates(folder, rows);
        }

        private static Row GraphArrangeRowOfChildren(ExplorerItem folder, int rowFirstChild)
        {
            // DONT CALCULATE IF FOLDER IS EMPTY
            if (folder.ItemSize == 0)
            {
                return new Row { ChildrenUsed = folder.Children.Count - rowFirstChild, RowHeight = 0.0 };
            }
            double minSideRatio = 0.4;
            double folderWidth = folder.Width;
            double folderArea = folder.AreaInPX;
            double rowHeight = 0;
            double usedRowArea = 0;
            int childrenUsed = 0;
            //Log("children: ");
            for (int i = rowFirstChild; i < folder.Children.Count; i++)
            {
                ExplorerItem item = folder.Children[i];
                // DONT CALCULATE IF FILE HAS 0 BYTES
                if (item.ItemSize == 0)
                {
                    //Log(">file empty");
                    childrenUsed = folder.Children.Count - rowFirstChild;
                    break;
                }
                item.AreaInPX = folderArea * item.SizePercentage;
                usedRowArea += item.AreaInPX;
                double theoreticalRowHeight = usedRowArea / folderWidth;
                double theoreticalItemWidth = item.AreaInPX / usedRowArea * folderWidth;

                if (theoreticalItemWidth / theoreticalRowHeight < minSideRatio && childrenUsed != 0)
                {
                    break;
                }
                //Log(">file or folder: " + folder.Children[i].Name);
                rowHeight = theoreticalRowHeight;
                childrenUsed++;
            }
            return new Row { ChildrenUsed = childrenUsed, RowHeight = rowHeight };
        }

        private static void GraphCalcRowCoordinates(ExplorerItem folder, List<Row> rows)
        {
            // FOLDER WITH ROWS, EACH ROW WITH CHILDREN WITH KNOWN HEIGHT AND UNKNOWN WIDTH
            double totalRowHeight = 0;
            int childrenDone = 0;
            double newRowWidth = 0;
            double itemWidth;

            // CACULATE AND SAVE COORDINATES OF EACH ITEM TO TOP, LEFT, BOTTOM, RIGHT PROPERTIES
            for (int i = 0; i < rows.Count; i++)
            {
                for (int j = 0; j < rows[i].ChildrenUsed; j++, childrenDone++)
                {
                    if (folder.ItemSize == 0 || rows[i].RowHeight == 0)
                    {
                        itemWidth = 0;
                    }
                    else
                    {
                        itemWidth = folder.Children[childrenDone].SizePercentage / rows[i].RowHeight * folder.AreaInPX;
                    }
                    folder.Children[childrenDone].Top = folder.Top + totalRowHeight;
                    folder.Children[childrenDone].Left = folder.Left + newRowWidth;
                    folder.Children[childrenDone].Height = rows[i].RowHeight;
                    folder.Children[childrenDone].Width = itemWidth;
                    newRowWidth += folder.Children[childrenDone].Width;
                }
                totalRowHeight += rows[i].RowHeight;
                newRowWidth = 0;
            }
        }

        private static async void GraphDrawWin2DInit(GraphingData graphingData, ExplorerItem folder)
        {
            if (folder.Width != 0)
            {
                // ADD ELEMENTS TO GRAPHIC
                Debug.WriteLine("Creating BM");
                graphingData.bm = new Bitmap((int)folder.Width + 1, (int)folder.Height + 1);
                Debug.WriteLine("Created BM");
                Graphics g = Graphics.FromImage(graphingData.bm);

                //GraphDrawWin2D(folder, bm, g);
                await Task.Run(() => GraphDrawWin2D(folder, graphingData.bm, g));
                foreach (ExplorerItem item in folder.Children)
                {
                    if (item.Type == ExplorerItem.ExplorerItemType.Folder)
                    {
                        //GraphDrawWin2D(item, bm, g);
                        await Task.Run(() => GraphDrawWin2D(item, graphingData.bm, g));
                    }
                }
                //graphingData.Bm = graphingData.bm;
                // RENDERING TO UI
                BitmapImage bitmapImage = new BitmapImage();
                using (MemoryStream stream = new MemoryStream())
                {
                    graphingData.bm.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Position = 0;
                    await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                }
                graphingData.Bmimg = bitmapImage;
                graphingData.IsGraphing = false;
                graphingData.GraphInvalid = false;
                // ADD SELECTION RECTANGLE
                //DrawSelectionRectangle(SelectedItem);
            }
        }



        public static async void FileTypeBarInit(DirectoryData directoryData, ObservableCollection<FileTypeInfo> colorizedFileTypes)
        {
            int i = Math.Min(directoryData.IconsAndColors.fileColorsCount, directoryData.FileExtensionsOrderedBySizeList.Count());
            for (int j = 0; j < i; j++)
            {
                Debug.Write(i + " " + j);
                FileExtensionAndSize feas = directoryData.FileExtensionsOrderedBySizeList[j];
                Bitmap bm = DrawFileTypeBM(IconsAndColors.fileColors[j]);

                BitmapImage bitmapImage = new BitmapImage();
                using (MemoryStream stream = new MemoryStream())
                {
                    bm.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Position = 0;
                    await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                }
                colorizedFileTypes.Add(new FileTypeInfo { bitmapImage = bitmapImage, fileExtensionAndSize = feas });
            }
        }

        private static Bitmap DrawFileTypeBM(System.Drawing.Color c)
        {
            int FiletypeBMWidth = 20;
            int FiletypeBMHeight = 20;
            Vector3 lightSource = new Vector3((float)(FiletypeBMWidth / 2), (float)(FiletypeBMHeight / 2), lightHeight);
            lightSource.Z = (int)(FiletypeBMWidth / 3);
            Vector3 center;
            Vector3 centerToLight;
            double light;
            Vector3 pointToLight;

            Bitmap bm = new Bitmap(FiletypeBMWidth, FiletypeBMHeight);

            System.Drawing.Rectangle bounds = new(0, 0, FiletypeBMWidth, FiletypeBMHeight);

            center = new Vector3((float)(FiletypeBMWidth), (float)(FiletypeBMHeight / 2), 0);
            centerToLight = lightSource - center;
            centerToLight.Z = 0;

            for (int ix = 0; ix < FiletypeBMWidth; ix++)
            {
                for (int iy = 0; iy < FiletypeBMHeight; iy++)
                {
                    light = 0;
                    // Explanation: pointToLight = lightSource - point;
                    pointToLight = lightSource;
                    pointToLight.X -= ix;
                    pointToLight.Y -= iy;

                    // This "shifts" the point being drawn closer to the light, scaled by the center of the current file 
                    // allows distiction between files in a folder
                    pointToLight -= Vector3.Multiply(centerToLight, 0.8f);

                    // Directional light. Is 1 if the light and the pixel are in the same spot.
                    // Gets to 0 the further away the pixel is from the light source.
                    // *0.8f so directional + ambient light = 1 at most
                    light += Vector3.Dot(Vector3.Normalize(pointToLight), surfaceNormal) * 0.8f;

                    // Ambient light
                    light += ambientLight;
                    System.Drawing.Color lightColor = System.Drawing.Color.FromArgb(255, Math.Min((int)(c.R * light), 255), Math.Min((int)(c.G * light), 255), Math.Min((int)(c.B * light), 255));
                    bm.SetPixel((int)ix, (int)iy, lightColor);
                }
            }
            return bm;
        }

        private static void GraphDrawWin2D(ExplorerItem folder, Bitmap bm, Graphics g)
        {
            Vector3 lightSource = new Vector3((float)(folder.Left + folder.Width / 2), (float)(folder.Top + folder.Height / 2), lightHeight);
            lightSource.Z = (int)(Math.Max(folder.Width, folder.Height) / 6);
            Vector3 center;
            Vector3 centerToLight;
            double light;
            Vector3 pointToLight;

            foreach (ExplorerItem item in folder.Children)
            {
                if (item.Type == ExplorerItem.ExplorerItemType.File)
                {
                    if (item.ItemSize != 0)
                    {
                        System.Drawing.Rectangle bounds = new((int)item.Left, (int)item.Top, (int)item.Width, (int)item.Height);
                        System.Drawing.Color c = item.FileColor;

                        //g.FillRectangle(new SolidBrush(c), bounds);
                        if (item.AreaInPX > 1)
                        {
                            // g.FillRectangle was here
                            lightSource.Z = (int)(Math.Max(item.Width, item.Height) / 3);

                            center = new Vector3((float)(item.Left + item.Width / 2), (float)(item.Top + item.Height / 2), 0);
                            centerToLight = lightSource - center;
                            centerToLight.Z = 0;
                            for (int ix = (int)item.Left; ix < item.Left + item.Width; ix++)
                            {
                                for (int iy = (int)item.Top; iy < item.Top + item.Height; iy++)
                                {
                                    light = 0;
                                    // Explanation: pointToLight = lightSource - point;
                                    pointToLight = lightSource;
                                    pointToLight.X -= ix;
                                    pointToLight.Y -= iy;

                                    // This "shifts" the point being drawn closer to the light, scaled by the center of the current file 
                                    // allows distiction between files in a folder
                                    //TODO Scale 0.8f to folder size.
                                    pointToLight -= Vector3.Multiply(centerToLight, 0.8f);

                                    // Directional light. Is 1 if the light and the pixel are in the same spot.
                                    // Gets to 0 the further away the pixel is from the light source.
                                    // *0.8f so directional + ambient light = 1 at most
                                    light += Vector3.Dot(Vector3.Normalize(pointToLight), surfaceNormal) * 0.8f;

                                    // Ambient light
                                    light += ambientLight;

                                    /* WinDirStat implementation without offset
                                    double h4 = 4 * lightHeight;

                                    double wf = h4 / item.Width;
                                    var var2 = wf * item.Width / 2;
                                    var var0 = -wf;

                                    double hf = h4 / item.Height;
                                    var var3 = hf * item.Height / 2;
                                    var var1 = -hf;


                                    double nx = -(1 * var0 * (ix + 0.5) + var2);
                                    double ny = -(1 * var1 * (iy + 0.5) + var3);
                                    double cosa = (nx * 0 + ny * 0 + lightHeight) / Math.Sqrt(nx * nx + ny * ny + 1.0);
                                    if (cosa > 1.0)
                                        cosa = 1.0;

                                    light = 0.8f * cosa;
                                    if (light < 0)
                                        light = 0;

                                    light += 0.2f;
                                    */

                                    System.Drawing.Color lightColor = System.Drawing.Color.FromArgb(255, Math.Min((int)(c.R * light), 255), Math.Min((int)(c.G * light), 255), Math.Min((int)(c.B * light), 255));
                                    bm.SetPixel((int)ix, (int)iy, lightColor);
                                }
                            }
                        }
                        else
                        {
                            //g.DrawRectangle(new Pen(c), bounds);
                        }
                    }
                }
                if (item.Type == ExplorerItem.ExplorerItemType.Folder)
                {
                    GraphDrawWin2D(item, bm, g);
                }
            }
        }

        public static void DrawSelectionRectangle(ExplorerItem tempSelectedItem, Selection selectionInstance)
        {
            if (tempSelectedItem == null) { return; }
            if (tempSelectedItem.Height < 10 || tempSelectedItem.Width < 10)
            {
                //TODO Replace 2 with stroke variable
                selectionInstance.RectangleCollection.Add(new SelectionRectangle(
                    (int)tempSelectedItem.Height + 2 * 2,
                    (int)tempSelectedItem.Width + 2 * 2,
                    (int)tempSelectedItem.Top - 2,
                    (int)tempSelectedItem.Left - 2));
            }
            else
            {
                //TODO Replace 2 with stroke variable
                selectionInstance.RectangleCollection.Add(new SelectionRectangle(
                    (int)tempSelectedItem.Height,
                    (int)tempSelectedItem.Width,
                    (int)tempSelectedItem.Top,
                    (int)tempSelectedItem.Left));
            }
        }
    }
}
