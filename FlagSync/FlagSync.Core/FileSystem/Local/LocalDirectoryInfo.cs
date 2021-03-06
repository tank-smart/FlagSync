﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FlagSync.Core.FileSystem.Base;

namespace FlagSync.Core.FileSystem.Local
{
    /// <summary>
    /// Represents a directory in the local filesystem.
    /// </summary>
    /// <remarks>
    /// The <see cref="LocalDirectoryInfo"/> class is basically a wrapper around
    /// the <see cref="System.IO.DirectoryInfo"/> class and abstracts it to be
    /// used as an <see cref="IDirectoryInfo"/>.
    /// </remarks>
    [DebuggerDisplay("{FullName}")]
    public class LocalDirectoryInfo : IDirectoryInfo
    {
        private readonly DirectoryInfo directoryInfo;

        /// <summary>
        /// Gets the parent directory.
        /// </summary>
        public IDirectoryInfo Parent
        {
            get { return new LocalDirectoryInfo(this.directoryInfo.Parent); }
        }

        /// <summary>
        /// Gets the full name of the directory.
        /// </summary>
        public string FullName
        {
            get { return this.directoryInfo.FullName; }
        }

        /// <summary>
        /// Gets the name of the directory.
        /// </summary>
        public string Name
        {
            get { return this.directoryInfo.Name; }
        }

        /// <summary>
        /// Gets a value indicating whether the directory exists.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the directory exists; otherwise, <c>false</c>.
        /// </value>
        public bool Exists
        {
            get { return this.directoryInfo.Exists; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDirectoryInfo"/> class.
        /// </summary>
        /// <param name="directoryInfo">The directory info to wrap.</param>
        public LocalDirectoryInfo(DirectoryInfo directoryInfo)
        {
            //Don't check for null here

            this.directoryInfo = directoryInfo;
        }

        /// <summary>
        /// Returns a list of all files in the directory.
        /// </summary>
        /// <returns>
        /// The files in the directory.
        /// </returns>
        /// <exception cref="System.UnauthorizedAccessException">
        /// The exception that is thrown if the directory is locked
        ///   </exception>
        public IEnumerable<IFileInfo> GetFiles()
        {
            return this.directoryInfo
                .GetFiles()
                .Select(file => (IFileInfo)new LocalFileInfo(file));
        }

        /// <summary>
        /// Returns a list of all directories in the directory.
        /// </summary>
        /// <returns>
        /// The directories in the directory.
        /// </returns>
        /// <exception cref="System.UnauthorizedAccessException">
        /// The exception that is thrown if the directory is locked.
        ///   </exception>
        public IEnumerable<IDirectoryInfo> GetDirectories()
        {
            return this.directoryInfo
                .GetDirectories()
                .Select(directory => (IDirectoryInfo)new LocalDirectoryInfo(directory));
        }
    }
}