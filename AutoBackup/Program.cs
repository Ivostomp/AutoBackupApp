using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBackup
{
    class Program
    {
        private static int MaxBackupCount = 25;

        static void Main(string[] args) {
            #if DEBUG
                //args = new string[] { @"C:\Users\Ivo\Documents\TestFolder", @"C:\Users\Ivo\Desktop\Backuptest", "5"};
                //args = new string[] { @"C:\Users\Ivo\Documents\TestFolder", @"C:\Users\Ivo\Documents\TestFolder\Backup", "5"};
                args = new string[] { @"C:\Users\Ivo\Documents\TestFolder", @"C:\Users\Ivo\Documents\TestFolderBackup", "5"};
            #else
                if (args.Count() < 2) { Console.WriteLine("Please insert executable arguments"); Console.WriteLine("EXAMPLE: AutoBackup.exe [orginDir] [backupDestinationDir] [maxAmountofbackup(optional)]"); Console.ReadLine(); return; }
            #endif

            try {
                var sourceUrl = args[0];
                var backupDir = args[1];

                if (backupDir.Contains($@"{sourceUrl}\")) {
                    Console.WriteLine("Please set destination directory to other location"); Console.WriteLine("(Cannot be inside origin directory)"); Console.ReadLine(); return; }

                DirectoryInfo dirOrigin = new DirectoryInfo(sourceUrl);
                if (dirOrigin.GetFiles("*", SearchOption.AllDirectories).Count() == 0) { return; }

                if (args.Count() > 2) { MaxBackupCount = int.Parse(args[2]); }

                if (!Directory.Exists(backupDir)) { Directory.CreateDirectory(backupDir); }

                if (!CheckFilesOnModified(sourceUrl, backupDir)) { return; }

                RemoveDirOnMaxCount(backupDir);

                var dirCurrentTime = $@"{backupDir}\Backup-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}";
                if (!Directory.Exists(dirCurrentTime)) { Directory.CreateDirectory(dirCurrentTime); }

                DirectoryCopy(sourceUrl, dirCurrentTime, true);

            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        private static void RemoveDirOnMaxCount(string backupDir) {
            DirectoryInfo backupDirInfo = new DirectoryInfo(backupDir);
            var backupDirDirectories = backupDirInfo.GetDirectories();
            if (backupDirDirectories.Count() >= MaxBackupCount || MaxBackupCount == 0) {
                var res = backupDirDirectories.OrderBy(o => o.LastWriteTime);
                res.First().Delete(true);
            }
        }

        private static bool CheckFilesOnModified(string sourceDirName, string backLocation) {
            DirectoryInfo dir = new DirectoryInfo(backLocation);
            var backupDirsCollection = dir.GetDirectories();
            if (backupDirsCollection == null || backupDirsCollection.Count() == 0) { return true; }
            var lastDir = backupDirsCollection?.OrderByDescending(o => o.LastWriteTime)?.First();

            var backupFiles = lastDir?.GetFiles("*", SearchOption.AllDirectories);
            //var backupDirs = lastDir?.GetDirectories("*", SearchOption.AllDirectories);

            var dirBackupLastModifiedFile = new DateTime();
            //var dirBackupLastModifiedDir = new DateTime();

            if (backupFiles != null && backupFiles.Count() > 0) {
                dirBackupLastModifiedFile = backupFiles.OrderByDescending(o => o.LastWriteTime).Select(s => s.LastWriteTime).First(); }
            //if (backupDirs != null && backupDirs.Count() > 0) {
            //    dirBackupLastModifiedDir = backupDirs.OrderByDescending(o => o.LastWriteTime).Select(s => s.LastWriteTime).First(); }

            DirectoryInfo dirOrigin = new DirectoryInfo(sourceDirName);
            var originFiles = dirOrigin?.GetFiles("*", SearchOption.AllDirectories);
            var originDirs = dirOrigin?.GetDirectories("*", SearchOption.AllDirectories);

            var dirOriginLastModifiedFile = new DateTime();
            //var dirOriginLastModifiedDir = new DateTime();

            if (originFiles != null && originFiles.Count() > 0) {
                dirOriginLastModifiedFile = originFiles.Select(s => s.LastWriteTime).Distinct().OrderByDescending(o => o).First(); }
            //if (originDirs != null && originDirs.Count() > 0) {
            //    dirOriginLastModifiedDir = originDirs.Select(s => s.LastWriteTime).Distinct().OrderByDescending(o => o).First(); }

            var ofNames = originFiles.Select(s => s.FullName.Replace(sourceDirName, ""));
            var bfNames = backupFiles.Select(s => s.FullName.Replace(lastDir.FullName, ""));

            var movedFiles01 = bfNames.Except(ofNames).Count();

            return (dirBackupLastModifiedFile != dirOriginLastModifiedFile) || (movedFiles01 > 0) /*|| (dirBackupLastModifiedDir != dirOriginLastModifiedDir)*/ ;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs) {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists) {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName)) {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files) {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs) {
                foreach (DirectoryInfo subdir in dirs.Where(q => q.Name != "Backup")) {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
