using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Etier.IconHelper
{
	/// <summary>
	/// Provides static methods to read system icons for both folders and files.
	/// </summary>
	/// <example>
	/// <code>IconReader.GetFileIcon("c:\\general.xls");</code>
	/// </example>
	public class IconReader
	{
		/// <summary>
		/// Options to specify the size of icons to return.
		/// </summary>
		public enum IconSize
		{
			/// <summary>
			/// Specify large icon - 32 pixels by 32 pixels.
			/// </summary>
			Large = 0,
			/// <summary>
			/// Specify small icon - 16 pixels by 16 pixels.
			/// </summary>
			Small = 1
		}
        
		/// <summary>
		/// Options to specify whether folders should be in the open or closed state.
		/// </summary>
		public enum FolderType
		{
			/// <summary>
			/// Specify open folder.
			/// </summary>
			Open = 0,
			/// <summary>
			/// Specify closed folder.
			/// </summary>
			Closed = 1
		}

		/// <summary>
		/// Returns an icon for a given file - indicated by the name parameter.
		/// </summary>
		/// <param name="name">Pathname for file.</param>
		/// <param name="size">Large or small</param>
		/// <param name="linkOverlay">Whether to include the link icon</param>
		/// <returns>System.Drawing.Icon</returns>
		public static System.Drawing.Icon GetFileIcon(string name, IconSize size, bool linkOverlay)
		{
			Shell32.SHFILEINFO shfi = new Shell32.SHFILEINFO();
			uint flags = Shell32.SHGFI_ICON | Shell32.SHGFI_USEFILEATTRIBUTES;

			if (true == linkOverlay) flags += Shell32.SHGFI_LINKOVERLAY;

			/* Check the size specified for return. */
			if (IconSize.Small == size)
			{
				flags += Shell32.SHGFI_SMALLICON ;
			} 
			else 
			{
				flags += Shell32.SHGFI_LARGEICON ;
			}

			Shell32.SHGetFileInfo(	name, 
				Shell32.FILE_ATTRIBUTE_NORMAL, 
				ref shfi, 
				(uint) System.Runtime.InteropServices.Marshal.SizeOf(shfi), 
				flags );

			// Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly
			System.Drawing.Icon icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(shfi.hIcon).Clone();
			User32.DestroyIcon( shfi.hIcon );       // Cleanup
			return icon;
		}

		/// <summary>
		/// Used to access system folder icons.
		/// </summary>
		/// <param name="size">Specify large or small icons.</param>
		/// <param name="folderType">Specify open or closed FolderType.</param>
		/// <returns>System.Drawing.Icon</returns>
		public static System.Drawing.Icon GetFolderIcon( IconSize size, FolderType folderType )
		{
			// Need to add size check, although errors generated at present!
			uint flags = Shell32.SHGFI_ICON | Shell32.SHGFI_USEFILEATTRIBUTES;

			if (FolderType.Open == folderType)
			{
				flags += Shell32.SHGFI_OPENICON;
			}
			
			if (IconSize.Small == size)
			{
				flags += Shell32.SHGFI_SMALLICON;
			} 
			else 
			{
				flags += Shell32.SHGFI_LARGEICON;
			}

			// Get the folder icon
			Shell32.SHFILEINFO shfi = new Shell32.SHFILEINFO();
			Shell32.SHGetFileInfo(	null, 
				Shell32.FILE_ATTRIBUTE_DIRECTORY, 
				ref shfi, 
				(uint) System.Runtime.InteropServices.Marshal.SizeOf(shfi), 
				flags );

			System.Drawing.Icon.FromHandle(shfi.hIcon);	// Load the icon from an HICON handle

			// Now clone the icon, so that it can be successfully stored in an ImageList
			System.Drawing.Icon icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(shfi.hIcon).Clone();

			User32.DestroyIcon( shfi.hIcon );		// Cleanup
			return icon;
		}

        public static System.Drawing.Icon GetFolderIconEx()
        {
            Shell32.SHSTOCKICONINFO sii = new Shell32.SHSTOCKICONINFO();
            sii.cbSize = (UInt32)Marshal.SizeOf(typeof(Shell32.SHSTOCKICONINFO));
            sii.hIcon = IntPtr.Zero;

            Shell32.SHGetStockIconInfo(Shell32.SHSTOCKICONID.SIID_FOLDER,
                 Shell32.SHGSI.SHGSI_ICON | Shell32.SHGSI.SHGSI_SMALLICON,
                 ref sii);

            System.Drawing.Icon.FromHandle(sii.hIcon); // Load the icon from an HICON handle

            // Now clone the icon, so that it can be successfully stored in an ImageList
            System.Drawing.Icon icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(sii.hIcon).Clone();
            
            User32.DestroyIcon(sii.hIcon);     // Cleanup
            return icon;
        }
    }

	/// <summary>
	/// Wraps necessary Shell32.dll structures and functions required to retrieve Icon Handles using SHGetFileInfo. Code
	/// courtesy of MSDN Cold Rooster Consulting case study.
	/// </summary>
	/// 

	// This code has been left largely untouched from that in the CRC example. The main changes have been moving
	// the icon reading code over to the IconReader type.
	public class Shell32  
	{		
		public const int 	MAX_PATH = 256;
		[StructLayout(LayoutKind.Sequential)]
			public struct SHITEMID
		{
			public ushort cb;
			[MarshalAs(UnmanagedType.LPArray)]
			public byte[] abID;
		}

		[StructLayout(LayoutKind.Sequential)]
			public struct ITEMIDLIST
		{
			public SHITEMID mkid;
		}

		[StructLayout(LayoutKind.Sequential)]
			public struct BROWSEINFO 
		{ 
			public IntPtr		hwndOwner; 
			public IntPtr		pidlRoot; 
			public IntPtr 		pszDisplayName;
			[MarshalAs(UnmanagedType.LPTStr)] 
			public string 		lpszTitle; 
			public uint 		ulFlags; 
			public IntPtr		lpfn; 
			public int			lParam; 
			public IntPtr 		iImage; 
		}

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHSTOCKICONINFO
        {
            public uint cbSize;
            public IntPtr hIcon;
            public int iSysIconIndex;
            public int iIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szPath;
        }

        // Browsing for directory.
        public const uint BIF_RETURNONLYFSDIRS   =	0x0001;
		public const uint BIF_DONTGOBELOWDOMAIN  =	0x0002;
		public const uint BIF_STATUSTEXT         =	0x0004;
		public const uint BIF_RETURNFSANCESTORS  =	0x0008;
		public const uint BIF_EDITBOX            =	0x0010;
		public const uint BIF_VALIDATE           =	0x0020;
		public const uint BIF_NEWDIALOGSTYLE     =	0x0040;
		public const uint BIF_USENEWUI           =	(BIF_NEWDIALOGSTYLE | BIF_EDITBOX);
		public const uint BIF_BROWSEINCLUDEURLS  =	0x0080;
		public const uint BIF_BROWSEFORCOMPUTER  =	0x1000;
		public const uint BIF_BROWSEFORPRINTER   =	0x2000;
		public const uint BIF_BROWSEINCLUDEFILES =	0x4000;
		public const uint BIF_SHAREABLE          =	0x8000;

		[StructLayout(LayoutKind.Sequential)]
			public struct SHFILEINFO
		{ 
			public const int NAMESIZE = 80;
			public IntPtr	hIcon; 
			public int		iIcon; 
			public uint	dwAttributes; 
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
			public string szDisplayName; 
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=NAMESIZE)]
			public string szTypeName; 
		};

		public const uint SHGFI_ICON				= 0x000000100;     // get icon
		public const uint SHGFI_DISPLAYNAME			= 0x000000200;     // get display name
		public const uint SHGFI_TYPENAME          	= 0x000000400;     // get type name
		public const uint SHGFI_ATTRIBUTES        	= 0x000000800;     // get attributes
		public const uint SHGFI_ICONLOCATION      	= 0x000001000;     // get icon location
		public const uint SHGFI_EXETYPE           	= 0x000002000;     // return exe type
		public const uint SHGFI_SYSICONINDEX      	= 0x000004000;     // get system icon index
		public const uint SHGFI_LINKOVERLAY       	= 0x000008000;     // put a link overlay on icon
		public const uint SHGFI_SELECTED          	= 0x000010000;     // show icon in selected state
		public const uint SHGFI_ATTR_SPECIFIED    	= 0x000020000;     // get only specified attributes
		public const uint SHGFI_LARGEICON         	= 0x000000000;     // get large icon
		public const uint SHGFI_SMALLICON         	= 0x000000001;     // get small icon
		public const uint SHGFI_OPENICON          	= 0x000000002;     // get open icon
		public const uint SHGFI_SHELLICONSIZE     	= 0x000000004;     // get shell size icon
		public const uint SHGFI_PIDL              	= 0x000000008;     // pszPath is a pidl
		public const uint SHGFI_USEFILEATTRIBUTES 	= 0x000000010;     // use passed dwFileAttribute
		public const uint SHGFI_ADDOVERLAYS       	= 0x000000020;     // apply the appropriate overlays
		public const uint SHGFI_OVERLAYINDEX      	= 0x000000040;     // Get the index of the overlay

		public const uint FILE_ATTRIBUTE_DIRECTORY  = 0x00000010;  
		public const uint FILE_ATTRIBUTE_NORMAL     = 0x00000080;  

		[DllImport("Shell32.dll")]
		public static extern IntPtr SHGetFileInfo(
			string pszPath,
			uint dwFileAttributes,
			ref SHFILEINFO psfi,
			uint cbFileInfo,
			uint uFlags
			);

        [Flags]
        public enum SHSTOCKICONID : uint
        {
            /// <summary>Document of a type with no associated application.</summary>
            SIID_DOCNOASSOC = 0,
            /// <summary>Document of a type with an associated application.</summary>
            SIID_DOCASSOC = 1,
            /// <summary>Generic application with no custom icon.</summary>
            SIID_APPLICATION = 2,
            /// <summary>Folder (generic, unspecified state).</summary>
            SIID_FOLDER = 3,
            /// <summary>Folder (open).</summary>
            SIID_FOLDEROPEN = 4,
            /// <summary>5.25-inch disk drive.</summary>
            SIID_DRIVE525 = 5,
            /// <summary>3.5-inch disk drive.</summary>
            SIID_DRIVE35 = 6,
            /// <summary>Removable drive.</summary>
            SIID_DRIVEREMOVE = 7,
            /// <summary>Fixed drive (hard disk).</summary>
            SIID_DRIVEFIXED = 8,
            /// <summary>Network drive (connected).</summary>
            SIID_DRIVENET = 9,
            /// <summary>Network drive (disconnected).</summary>
            SIID_DRIVENETDISABLED = 10,
            /// <summary>CD drive.</summary>
            SIID_DRIVECD = 11,
            /// <summary>RAM disk drive.</summary>
            SIID_DRIVERAM = 12,
            /// <summary>The entire network.</summary>
            SIID_WORLD = 13,
            /// <summary>A computer on the network.</summary>
            SIID_SERVER = 15,
            /// <summary>A local printer or print destination.</summary>
            SIID_PRINTER = 16,
            /// <summary>The Network virtual folder (FOLDERID_NetworkFolder/CSIDL_NETWORK).</summary>
            SIID_MYNETWORK = 17,
            /// <summary>The Search feature.</summary>
            SIID_FIND = 22,
            /// <summary>The Help and Support feature.</summary>
            SIID_HELP = 23,
            /// <summary>Overlay for a shared item.</summary>
            SIID_SHARE = 28,
            /// <summary>Overlay for a shortcut.</summary>
            SIID_LINK = 29,
            /// <summary>Overlay for items that are expected to be slow to access.</summary>
            SIID_SLOWFILE = 30,
            /// <summary>The Recycle Bin (empty).</summary>
            SIID_RECYCLER = 31,
            /// <summary>The Recycle Bin (not empty).</summary>
            SIID_RECYCLERFULL = 32,
            /// <summary>Audio CD media.</summary>
            SIID_MEDIACDAUDIO = 40,
            /// <summary>Security lock.</summary>
            SIID_LOCK = 47,
            /// <summary>A virtual folder that contains the results of a search.</summary>
            SIID_AUTOLIST = 49,
            /// <summary>A network printer.</summary>
            SIID_PRINTERNET = 50,
            /// <summary>A server shared on a network.</summary>
            SIID_SERVERSHARE = 51,
            /// <summary>A local fax printer.</summary>
            SIID_PRINTERFAX = 52,
            /// <summary>A network fax printer.</summary>
            SIID_PRINTERFAXNET = 53,
            /// <summary>A file that receives the output of a Print to file operation.</summary>
            SIID_PRINTERFILE = 54,
            /// <summary>A category that results from a Stack by command to organize the contents of a folder.</summary>
            SIID_STACK = 55,
            /// <summary>Super Video CD (SVCD) media.</summary>
            SIID_MEDIASVCD = 56,
            /// <summary>A folder that contains only subfolders as child items.</summary>
            SIID_STUFFEDFOLDER = 57,
            /// <summary>Unknown drive type.</summary>
            SIID_DRIVEUNKNOWN = 58,
            /// <summary>DVD drive.</summary>
            SIID_DRIVEDVD = 59,
            /// <summary>DVD media.</summary>
            SIID_MEDIADVD = 60,
            /// <summary>DVD-RAM media.</summary>
            SIID_MEDIADVDRAM = 61,
            /// <summary>DVD-RW media.</summary>
            SIID_MEDIADVDRW = 62,
            /// <summary>DVD-R media.</summary>
            SIID_MEDIADVDR = 63,
            /// <summary>DVD-ROM media.</summary>
            SIID_MEDIADVDROM = 64,
            /// <summary>CD+ (enhanced audio CD) media.</summary>
            SIID_MEDIACDAUDIOPLUS = 65,
            /// <summary>CD-RW media.</summary>
            SIID_MEDIACDRW = 66,
            /// <summary>CD-R media.</summary>
            SIID_MEDIACDR = 67,
            /// <summary>A writable CD in the process of being burned.</summary>
            SIID_MEDIACDBURN = 68,
            /// <summary>Blank writable CD media.</summary>
            SIID_MEDIABLANKCD = 69,
            /// <summary>CD-ROM media.</summary>
            SIID_MEDIACDROM = 70,
            /// <summary>An audio file.</summary>
            SIID_AUDIOFILES = 71,
            /// <summary>An image file.</summary>
            SIID_IMAGEFILES = 72,
            /// <summary>A video file.</summary>
            SIID_VIDEOFILES = 73,
            /// <summary>A mixed file.</summary>
            SIID_MIXEDFILES = 74,
            /// <summary>Folder back.</summary>
            SIID_FOLDERBACK = 75,
            /// <summary>Folder front.</summary>
            SIID_FOLDERFRONT = 76,
            /// <summary>Security shield. Use for UAC prompts only.</summary>
            SIID_SHIELD = 77,
            /// <summary>Warning.</summary>
            SIID_WARNING = 78,
            /// <summary>Informational.</summary>
            SIID_INFO = 79,
            /// <summary>Error.</summary>
            SIID_ERROR = 80,
            /// <summary>Key.</summary>
            SIID_KEY = 81,
            /// <summary>Software.</summary>
            SIID_SOFTWARE = 82,
            /// <summary>A UI item, such as a button, that issues a rename command.</summary>
            SIID_RENAME = 83,
            /// <summary>A UI item, such as a button, that issues a delete command.</summary>
            SIID_DELETE = 84,
            /// <summary>Audio DVD media.</summary>
            SIID_MEDIAAUDIODVD = 85,
            /// <summary>Movie DVD media.</summary>
            SIID_MEDIAMOVIEDVD = 86,
            /// <summary>Enhanced CD media.</summary>
            SIID_MEDIAENHANCEDCD = 87,
            /// <summary>Enhanced DVD media.</summary>
            SIID_MEDIAENHANCEDDVD = 88,
            /// <summary>High definition DVD media in the HD DVD format.</summary>
            SIID_MEDIAHDDVD = 89,
            /// <summary>High definition DVD media in the Blu-ray Disc™ format.</summary>
            SIID_MEDIABLURAY = 90,
            /// <summary>Video CD (VCD) media.</summary>
            SIID_MEDIAVCD = 91,
            /// <summary>DVD+R media.</summary>
            SIID_MEDIADVDPLUSR = 92,
            /// <summary>DVD+RW media.</summary>
            SIID_MEDIADVDPLUSRW = 93,
            /// <summary>A desktop computer.</summary>
            SIID_DESKTOPPC = 94,
            /// <summary>A mobile computer (laptop).</summary>
            SIID_MOBILEPC = 95,
            /// <summary>The User Accounts Control Panel item.</summary>
            SIID_USERS = 96,
            /// <summary>Smart media.</summary>
            SIID_MEDIASMARTMEDIA = 97,
            /// <summary>CompactFlash media.</summary>
            SIID_MEDIACOMPACTFLASH = 98,
            /// <summary>A cell phone.</summary>
            SIID_DEVICECELLPHONE = 99,
            /// <summary>A digital camera.</summary>
            SIID_DEVICECAMERA = 100,
            /// <summary>A digital video camera.</summary>
            SIID_DEVICEVIDEOCAMERA = 101,
            /// <summary>An audio player.</summary>
            SIID_DEVICEAUDIOPLAYER = 102,
            /// <summary>Connect to network.</summary>
            SIID_NETWORKCONNECT = 103,
            /// <summary>The Network and Internet Control Panel item.</summary>
            SIID_INTERNET = 104,
            /// <summary>A compressed file with a .zip file name extension.</summary>
            SIID_ZIPFILE = 105,
            /// <summary>The Additional Options Control Panel item.</summary>
            SIID_SETTINGS = 106,
            /// <summary>High definition DVD drive (any type - HD DVD-ROM, HD DVD-R, HD-DVD-RAM) that uses the HD DVD format.</summary>
            /// <remarks>Windows Vista with SP1 and later.</remarks>
            SIID_DRIVEHDDVD = 132,
            /// <summary>High definition DVD drive (any type - BD-ROM, BD-R, BD-RE) that uses the Blu-ray Disc format.</summary>
            /// <remarks>Windows Vista with SP1 and later.</remarks>
            SIID_DRIVEBD = 133,
            /// <summary>High definition DVD-ROM media in the HD DVD-ROM format.</summary>
            /// <remarks>Windows Vista with SP1 and later.</remarks>
            SIID_MEDIAHDDVDROM = 134,
            /// <summary>High definition DVD-R media in the HD DVD-R format.</summary>
            /// <remarks>Windows Vista with SP1 and later.</remarks>
            SIID_MEDIAHDDVDR = 135,
            /// <summary>High definition DVD-RAM media in the HD DVD-RAM format.</summary>
            /// <remarks>Windows Vista with SP1 and later.</remarks>
            SIID_MEDIAHDDVDRAM = 136,
            /// <summary>High definition DVD-ROM media in the Blu-ray Disc BD-ROM format.</summary>
            /// <remarks>Windows Vista with SP1 and later.</remarks>
            SIID_MEDIABDROM = 137,
            /// <summary>High definition write-once media in the Blu-ray Disc BD-R format.</summary>
            /// <remarks>Windows Vista with SP1 and later.</remarks>
            SIID_MEDIABDR = 138,
            /// <summary>High definition read/write media in the Blu-ray Disc BD-RE format.</summary>
            /// <remarks>Windows Vista with SP1 and later.</remarks>
            SIID_MEDIABDRE = 139,
            /// <summary>A cluster disk array.</summary>
            /// <remarks>Windows Vista with SP1 and later.</remarks>
            SIID_CLUSTEREDDRIVE = 140,
            /// <summary>The highest valid value in the enumeration.</summary>
            /// <remarks>Values over 160 are Windows 7-only icons.</remarks>
            SIID_MAX_ICONS = 175
        }
        public enum SHGSI : int
        {
            SHGSI_ICON = 0x100,
            SHGSI_ICONLOCATION = 0,
            SHGSI_LARGEICON = 0,
            SHGSI_LINKOVERLAY = 0x8000,
            SHGSI_SELECTED = 0x10000,
            SHGSI_SHELLICONSIZE = 4,
            SHGSI_SMALLICON = 1,
            SHGSI_SYSICONINDEX = 0x4000
        }

        [DllImport("Shell32.dll")]
        public static extern Int32 SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI uFlags, ref SHSTOCKICONINFO psii);

	}

    /// <summary>
    /// Wraps necessary functions imported from User32.dll. Code courtesy of MSDN Cold Rooster Consulting example.
    /// </summary>
    public class User32
	{
		/// <summary>
		/// Provides access to function required to delete handle. This method is used internally
		/// and is not required to be called separately.
		/// </summary>
		/// <param name="hIcon">Pointer to icon handle.</param>
		/// <returns>N/A</returns>
		[DllImport("User32.dll")]
		public static extern int DestroyIcon( IntPtr hIcon );
	}
}

