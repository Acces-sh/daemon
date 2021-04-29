using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Accessh.Configuration;
using Accessh.Configuration.Enums;
using Accessh.Configuration.Exception;
using Accessh.Configuration.Interfaces;
using Serilog;

namespace Accessh.Daemon.Services
{
    /// <summary>
    /// Authorized key file handler
    /// </summary>
    public class FileService : IFileService
    {
        private const string FileHeader = "############################ ACCES.SH ############################";
        private const string FileFooter = "########################## END ACCES.SH ##########################";
        private const string FileName = "authorized_keys";
        private readonly string _keyFilePath;

        public FileService(KeyConfiguration keyConfiguration, AppConfiguration appConfiguration)
        {
            if (appConfiguration.Mode == Mode.Docker)
            {
                var separator = "\\";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
                {
                    separator = "/";
                }

                _keyFilePath = Directory.GetCurrentDirectory() + separator + FileName;
            }
            else
            {
                _keyFilePath = keyConfiguration.AuthorizedKeyFilePath;
            }
        }

        /// <summary>
        /// Checks if the file can be read or written.
        /// </summary>
        /// <exception cref="FileNotFoundException">Authorized key file not found.</exception>
        /// <exception cref="FilePermissionException">Authorized keu file can't be read or written.</exception>
        /// <exception cref="Exception">Multiple exception caused by directory/file reading</exception>
        public void CheckPermissions()
        {
            using var fs = new FileStream(_keyFilePath, FileMode.Open);
            var canRead = fs.CanRead;
            var canWrite = fs.CanWrite;

            fs.Close();

            if (canRead == false || canWrite == false)
                throw new FilePermissionException();
        }

        /// <summary>
        /// Add the ssh keys between Acces.sh tags in authorized_key file.
        /// </summary>
        /// <param name="keys">Keys to add</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException">I/O error</exception>
        private async Task Add(IList<string> keys)
        {
            var lines = (await File.ReadAllLinesAsync(_keyFilePath)).ToList();
            var isHeaderFound = false;
            var isFooterFound = false;
            await using var stream = new StreamWriter(_keyFilePath);

            foreach (var line in lines)
            {
                if (isHeaderFound == false && string.Equals(line.Trim(), FileHeader))
                {
                    isHeaderFound = true;
                    await stream.WriteLineAsync(line);
                    foreach (var key in keys)
                    {
                        await stream.WriteLineAsync(key);
                    }

                    continue;
                }

                if (isFooterFound == false && string.Equals(line.Trim(), FileFooter))
                {
                    isFooterFound = true;
                }

                await stream.WriteLineAsync(line);
            }
        }

        /// <summary>
        /// Remove ssh keys between Acces.sh tags in authorized_key file.
        /// </summary>
        /// <param name="keys">Keys to remove</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException">I/O error</exception>
        private async Task Remove(IList<string> keys)
        {
            var lines = (await File.ReadAllLinesAsync(_keyFilePath)).ToList();
            var isHeaderFound = false;
            var isFooterFound = false;
            await using var stream = new StreamWriter(_keyFilePath);

            foreach (var line in lines)
            {
                if (isHeaderFound == false && string.Equals(line.Trim(), FileHeader))
                {
                    isHeaderFound = true;
                }

                if (isFooterFound == false && string.Equals(line.Trim(), FileFooter))
                {
                    isFooterFound = true;
                }

                if (isHeaderFound && isFooterFound == false)
                {
                    if (keys.Contains(line))
                    {
                        continue;
                    }
                }

                await stream.WriteLineAsync(line);
            }
        }

        /// <summary>
        /// Remove all keys between Acces.sh tags in authorized_key file.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException">I/O error</exception>
        public async Task RemoveAll()
        {
            var lines = File.ReadAllLines(_keyFilePath).ToList();
            var isHeaderFound = false;
            var isFooterFound = false;
            await using var stream = new StreamWriter(_keyFilePath);

            foreach (var line in lines)
            {
                if (isHeaderFound == false && string.Equals(line.Trim(), FileHeader))
                {
                    isHeaderFound = true;
                    continue;
                }

                if (isFooterFound == false && string.Equals(line.Trim(), FileFooter))
                {
                    isFooterFound = true;
                    continue;
                }

                if (isHeaderFound && isFooterFound == false)
                {
                    continue;
                }

                await stream.WriteLineAsync(line);
            }

            await stream.WriteLineAsync(FileHeader);
            await stream.WriteLineAsync(FileFooter);
        }

        #region Jobs

        /// <summary>
        /// Add keys job
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public async Task AddKeysJob(IList<string> keys)
        {
            await RemoveAll();
            await Add(keys);
        }

        /// <summary>
        /// Remove keys job
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public async Task RemoveKeysJob(IList<string> keys)
        {
            await Remove(keys);
        }

        #endregion
    }
}
