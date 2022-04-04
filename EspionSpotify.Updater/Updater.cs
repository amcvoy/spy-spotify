﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EspionSpotify.Updater.Models.GitHub;
using EspionSpotify.Updater.Utilities;
using Ionic.Zip;

namespace EspionSpotify.Updater
{
    internal static class Updater
    {
        private const string UPDATER_DIRECTORY = "Updater";
        private const string UPDATER_TMP_DIRECTORY = "tmp_updater";
        private const string APP = "spytify.exe";
        internal static readonly string ProjectDirectory = AppDomain.CurrentDomain.BaseDirectory + @"..\";
        private static readonly string UpdaterTempFullPath = $"{ProjectDirectory}{UPDATER_TMP_DIRECTORY}";
        internal static readonly string AppFullPath = $"{ProjectDirectory}{APP}";

        internal static async Task ProcessUpdateAsync()
        {
            DeleteTempFiles();

            try
            {
                var releases = await GitHub.GetReleases();

                if (!releases.Any()) return;

                foreach (var release in releases)
                {
                    Console.WriteLine("Updating to release {0}...", release.tag_name);
                    var fileName = await DownloadUpdateAsync(release);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        ExtractDownloadedAsset(fileName);
                        File.Delete(fileName);
                    }
                }

                Console.WriteLine("Successfully updated!");
                Process.Start(new ProcessStartInfo(AppFullPath));
            }
            catch
            {
                Process.Start(new ProcessStartInfo(GitHub.LATEST_RELEASE_LINK));
                LeaveConsole();
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private static async Task<string> DownloadUpdateAsync(Release release)
        {
            try
            {
                var asset = GitHub.GetZipAssetFromRelease(release);
                return await Web.DownloadFileAsync(asset.browser_download_url, release.tag_name);
            }
            catch (ReleaseAssetNotFoundException ex)
            {
                Console.WriteLine(
                    "Something went wrong during the download. Make sure it was not blocked by your Antivirus Software.");
                Console.WriteLine("Error Message: {0}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong during the download.");
                Console.WriteLine("Error Message: {0}", ex.Message);
                throw;
            }
        }

        private static void ExtractDownloadedAsset(string fileName)
        {
            ZipFile zip = null;
            try
            {
                zip = ZipFile.Read(fileName);
                Console.WriteLine("Extracting...");

                foreach (var entry in zip.ToList())
                    entry.Extract(
                        (Path.GetDirectoryName(entry.FileName) ?? "").Equals(UPDATER_DIRECTORY)
                            ? UpdaterTempFullPath
                            : ProjectDirectory, ExtractExistingFileAction.OverwriteSilently);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine(
                    "An error occurred while extracting. Make sure Spytify is not running or Antivirus Software is interfering.");
                Console.WriteLine("You can extract the file manually at: {0}", fileName);
                Console.WriteLine("Error Message: {0}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while extracting");
                Console.WriteLine("Error Message: {0}", ex.Message);
                throw;
            }
            finally
            {
                DeleteTempFiles();
                zip?.Dispose();
            }
        }

        internal static void LeaveConsole()
        {
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static void DeleteTempFiles()
        {
            foreach (var filename in Directory
                         .EnumerateFiles(ProjectDirectory, "*.*", SearchOption.AllDirectories)
                         .Where(s => new[] {".tmp", ".pending-overwrite"}.Any(ext => s.ToLower().EndsWith(ext))))
                try
                {
                    File.Delete(filename);
                }
                catch
                {
                    // ignored
                }
        }
    }
}