using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Randomize
{
    class Drive
    {
        public string DriveLetter { get; set; }

        public string DriveName { get; set; }

        public override string ToString()
        {
            return String.Format("{0} \"{1}\"", DriveLetter, DriveName);
        }
    }

    class DriveHelper
    {
        public static IEnumerable<Drive> GetRemovableDrives()
        {
            var usbDrives
                = DriveInfo
                    .GetDrives()
                    .Where(eachDriveInfo => eachDriveInfo.DriveType == DriveType.Removable)
                    .Select(eachDriveInfo => new Drive
                    {
                        DriveLetter = eachDriveInfo.Name,
                        DriveName = eachDriveInfo.VolumeLabel
                    });

            if (usbDrives.Any())
            {
                return usbDrives;
            }

            throw new DriveNotFoundException();
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
