﻿using System;
using System.IO;
using System.Linq;

namespace BackupUtilityCore.Tasks
{
    public sealed class BackupTaskSync : BackupTaskBase
    {
        protected override string BackupDescription => "SYNC";

        /// <summary>
        /// Syncs target directory with source directories.
        /// </summary>
        /// <returns>Number of new files backed up</returns>
        protected override int PerformBackup()
        {
            string targetDir = BackupSettings.TargetDirectory;

            AddToLog("Target DIR", targetDir);

            DirectoryInfo targetDirInfo = new DirectoryInfo(targetDir);

            // Check to create target directory
            if (!targetDirInfo.Exists)
            {
                targetDirInfo.Create();
            }

            int backupCount = 0;

            // Sync each source directory
            foreach (string sourceDir in BackupSettings.SourceDirectories)
            {
                AddToLog("Source DIR", sourceDir);

                // Get source path without root
                DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);
                string sourceSubDir = sourceDirInfo.FullName.Substring(sourceDirInfo.Root.Name.Length);

                // Append source sub-dir to target
                string targetSubDir = Path.Combine(targetDir, sourceSubDir);

                // Only remove files from within source directories, not entire target dir - 
                // may be other files in there that need to be kept.
                backupCount += SyncDirectories(sourceDir, targetSubDir);
            }

            return backupCount;
        }

        /// <summary>
        /// Remove directories/files from target that are not in source,
        /// then copies in new/updated files.
        /// </summary>
        private int SyncDirectories(string sourceDir, string targetDir)
        {
            AddToLog("Syncing DIR", sourceDir);

            DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDir);
            DirectoryInfo targetDirInfo = new DirectoryInfo(targetDir);

            //////////////////////////////////////////////////////
            // Remove directories from target not in source
            // (Remove before copying new files to free up space)
            //////////////////////////////////////////////////////

            // Get current source/target directories
            DirectoryInfo[] sourceDirectories = sourceDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).ToArray();
            DirectoryInfo[] targetDirectories = targetDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).ToArray();

            // Need to check for directories that exist in target but not in source
            foreach (DirectoryInfo target in targetDirectories)
            {
                // Only sub-dir name will match (ignore case), full paths are from different locations
                if (!sourceDirectories.Any(source => string.Compare(source.Name, target.Name, true) == 0))
                {
                    try
                    {
                        // Delete recursively
                        target.Delete(true);
                    }
                    catch (IOException ie)
                    {
                        AddToLog("SYNC ERROR", ie.Message);
                    }
                }
            }

            //////////////////////////////////////////////////////
            // Remove files from target not in source
            // (Remove before copying new files to free up space)
            //////////////////////////////////////////////////////

            // Get current source files
            var sourceFiles = Directory.EnumerateFiles(sourceDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly);

            // Get names only as lowercase for comparison
            string[] sourceFileNames = sourceFiles.Select(f => Path.GetFileName(f).ToLower()).ToArray();

            // Get full paths to target (maintain case, UNIX names can be case sensitive.)
            string[] targetFiles = Directory.EnumerateFiles(targetDirInfo.FullName, "*.*", SearchOption.TopDirectoryOnly).ToArray();

            // Need to check for files that exist in target but not in source
            foreach (string file in targetFiles)
            {
                // Compare names as lowercase
                string nameOnly = Path.GetFileName(file).ToLower();

                // Only name will match, full paths are from different locations
                if (!sourceFileNames.Contains(nameOnly))
                {
                    try
                    {
                        // Delete using full path
                        DeleteFile(file);
                    }
                    catch (IOException ie)
                    {
                        AddToLog("SYNC ERROR", ie.Message);
                    }
                }
            }

            //////////////////////////////////////////////////////
            // Copy files from source not in target
            // (Or replace old files)
            //////////////////////////////////////////////////////
            int backupCount = CopyFiles(sourceFiles, targetDir);

            /////////////////////////////////////////////
            // Sync sub directories
            /////////////////////////////////////////////
            foreach (DirectoryInfo subDirInfo in sourceDirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(d => !BackupSettings.IsDirectoryExcluded(d.Name)))
            {
                // Update directories
                sourceDir = Path.Combine(sourceDir, subDirInfo.Name);
                targetDir = Path.Combine(targetDir, subDirInfo.Name);

                // Recursive call
                backupCount += SyncDirectories(sourceDir, targetDir);
            }

            return backupCount;
        }

        private void DeleteFile(string filename)
        {
            FileInfo fileInfo = new FileInfo(filename);

            if (fileInfo.Exists)
            {
                // Make sure file is not read-only before we delete it.
                fileInfo.Attributes = FileAttributes.Normal;
                fileInfo.Delete();
            }
        }
    }
}
