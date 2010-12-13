﻿using FlagLib.Patterns;

namespace FlagSync.View
{
    public class LogMessage : ViewModelBase<LogMessage>
    {
        private int progress;

        /// <summary>
        /// Gets or sets the type (file or directory).
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; private set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>The action.</value>
        public string Action { get; private set; }

        /// <summary>
        /// Gets or sets the path of file A.
        /// </summary>
        /// <value>The path of file A.</value>
        public string SourcePath { get; private set; }

        /// <summary>
        /// Gets or sets the path of file B.
        /// </summary>
        /// <value>The path of file B.</value>
        public string TargetPath { get; private set; }

        /// <summary>
        /// Gets or sets the current progress of the file (0 - 100).
        /// </summary>
        /// <value>The current progress of the file.</value>
        public int Progress
        {
            get { return this.progress; }
            set
            {
                if (this.Progress != value)
                {
                    this.progress = value;
                    this.OnPropertyChanged(vm => vm.Progress);
                    this.OnPropertyChanged(vm => vm.IsInProgress);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current file is in progress.
        /// </summary>
        /// <value>true if the current file is in progress; otherwise, false.</value>
        public bool IsInProgress
        {
            get { return this.Progress != 100; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessage"></see> class.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="action">The action.</param>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="targetPath">The target path.</param>
        public LogMessage(string type, string action, string sourcePath, string targetPath, int initialProgress)
        {
            this.Type = type;
            this.Action = action;
            this.SourcePath = sourcePath;
            this.TargetPath = targetPath;
            this.Progress = initialProgress;
        }
    }
}