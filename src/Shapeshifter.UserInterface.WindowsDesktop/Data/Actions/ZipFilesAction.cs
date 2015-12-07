﻿namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Clipboard.Interfaces;
    using Services.Files.Interfaces;

    class ZipFilesAction: IZipFilesAction
    {
        readonly IAsyncFilter asyncFilter;

        readonly IFileManager fileManager;

        readonly IClipboardInjectionService clipboardInjectionService;

        public ZipFilesAction(
            IAsyncFilter asyncFilter,
            IFileManager fileManager,
            IClipboardInjectionService clipboardInjectionService)
        {
            this.asyncFilter = asyncFilter;
            this.fileManager = fileManager;
            this.clipboardInjectionService = clipboardInjectionService;
        }

        public string Description => "Compress the clipboard contents into a ZIP-file and copy it.";

        public byte Order => 75;

        public string Title => "Copy as compressed folder";

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage package)
        {
            var supportedData = await GetSupportedData(package);
            return supportedData.Any();
        }

        async Task<IReadOnlyCollection<IClipboardData>> GetSupportedData(
            IClipboardDataPackage package)
        {
            var supportedData = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync);
            return supportedData;
        }

        static async Task<bool> CanPerformAsync(
            IClipboardData data)
        {
            return
                data is IClipboardFileData ||
                data is IClipboardFileCollectionData;
        }

        public async Task PerformAsync(
            IClipboardDataPackage processedData)
        {
            var supportedDataCollection = await GetSupportedData(processedData);
            var firstSupportedData = supportedDataCollection.First();

            var zipFilePath = ZipData(firstSupportedData);
            clipboardInjectionService.InjectFiles(zipFilePath);
        }

        string ZipFileCollectionData(params IClipboardFileData[] fileDataItems)
        {
            if (fileDataItems == null)
            {
                throw new ArgumentNullException(nameof(fileDataItems));
            }

            if (fileDataItems.Length == 0)
            {
                throw new ArgumentException(
                    "There must be at least one item to compress.",
                    nameof(fileDataItems));
            }

            var filePaths = fileDataItems
                .Select(x => x.FullPath)
                .ToArray();
            var commonPath = FindCommonFolder(filePaths);
            var directoryName = Path.GetFileName(commonPath);
            var directoryPath = fileManager.PrepareFolder(directoryName);
            CopyFilesToTemporaryFolder(fileDataItems, directoryPath);

            var zipFile = ZipDirectory(directoryPath);
            return zipFile;
        }

        static string FindCommonFolder(IReadOnlyCollection<string> paths)
        {
            var pathSimilarityIndex = GetPathSegmentsInCommonCount(paths);

            var firstPath = paths.First();
            var segments = GetPathSegments(firstPath);

            var commonPath = Path.Combine(
                segments
                    .Take(pathSimilarityIndex)
                    .ToArray());

            return commonPath;
        }

        static int GetPathSegmentsInCommonCount(IReadOnlyCollection<string> paths)
        {
            var commonIndex = 0;
            foreach (var originPath in paths)
            {
                var originSegments = GetPathSegments(originPath);
                for (var index = 0; index < originSegments.Length; index++)
                {
                    var originSegment = originSegments[index];
                    foreach (var referencePath in paths)
                    {
                        var referenceSegments = GetPathSegments(referencePath);
                        if (referenceSegments.Length < originSegments.Length)
                        {
                            return commonIndex;
                        }

                        var referenceSegment = referenceSegments[index];
                        if (originSegment != referenceSegment)
                        {
                            return commonIndex;
                        }
                    }
                    commonIndex++;
                }
            }

            return commonIndex;
        }

        static string[] GetPathSegments(string originPath)
        {
            return originPath.Split('\\', '/');
        }

        static void CopyFilesToTemporaryFolder(
            IEnumerable<IClipboardFileData> fileDataItems,
            string directory)
        {
            foreach (var fileData in fileDataItems)
            {
                CopyFileToTemporaryFolder(directory, fileData);
            }
        }

        static void CopyFileToTemporaryFolder(string directory, IClipboardFileData fileData)
        {
            var destinationFilePath = Path.Combine(directory, fileData.FileName);
            DeleteFileIfExists(destinationFilePath);
            File.Copy(fileData.FullPath, destinationFilePath);
        }

        static void DeleteFileIfExists(string destinationFilePath)
        {
            if (File.Exists(destinationFilePath))
            {
                File.Delete(destinationFilePath);
            }
        }

        string ZipDirectory(string directory)
        {
            var directoryName = Path.GetFileName(directory);
            var compressedFolderDirectory = fileManager.PrepareFolder($"Compressed folders");
            var zipFile = Path.Combine(compressedFolderDirectory, $"{directoryName}.zip");

            DeleteFileIfExists(zipFile);
            ZipFile.CreateFromDirectory(directory, zipFile);

            return zipFile;
        }

        string ZipData(IClipboardData data)
        {
            var clipboardFileData = data as IClipboardFileData;
            if (clipboardFileData != null)
            {
                return ZipFileCollectionData(clipboardFileData);
            }

            var clipboardFileCollectionData = data as IClipboardFileCollectionData;
            if (clipboardFileCollectionData != null)
            {
                return ZipFileCollectionData(
                    clipboardFileCollectionData
                        .Files
                        .ToArray());
            }

            throw new InvalidOperationException("Unknown data format.");
        }
    }
}