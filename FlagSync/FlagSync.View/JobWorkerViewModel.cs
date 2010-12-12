﻿using System;
using System.Collections.Generic;
using System.IO;
using FlagLib.Collections;
using FlagLib.Patterns;
using FlagSync.Core;

namespace FlagSync.View
{
    public class JobWorkerViewModel : ViewModelBase<JobWorkerViewModel>
    {
        #region Members

        private JobWorker jobWorker;
        private JobSetting currentJobSetting;
        private long countedBytes;
        private long proceededBytes;
        private int countedFiles;
        private int proceededFiles;
        private bool isCounting;
        private bool isRunning;
        private string statusMessages = String.Empty;
        private string lastStatusMessage = String.Empty;

        #endregion Members

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the job worker is counting.
        /// </summary>
        /// <value>true if the job worker is counting; otherwise, false.</value>
        public bool IsCounting
        {
            get { return this.isCounting; }
            private set
            {
                if (this.IsCounting != value)
                {
                    this.isCounting = value;
                    this.OnPropertyChanged(vm => vm.IsCounting);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whetherthe job worker is started.
        /// </summary>
        /// <value>true if the job worker is started; otherwise, false.</value>
        public bool IsRunning
        {
            get { return this.isRunning; }
            set
            {
                if (this.IsRunning != value)
                {
                    this.isRunning = value;
                    this.OnPropertyChanged(vm => vm.IsRunning);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the job worker is paused.
        /// </summary>
        /// <value>true if the job worker is paused; otherwise, false.</value>
        public bool IsPaused
        {
            get { return this.jobWorker.Paused; }
        }

        /// <summary>
        /// Gets the counted bytes.
        /// </summary>
        /// <value>The counted bytes.</value>
        public long CountedBytes
        {
            get { return this.countedBytes; }
            private set
            {
                if (this.CountedBytes != value)
                {
                    this.countedBytes = value;
                    this.OnPropertyChanged(vm => vm.CountedBytes);
                }
            }
        }

        /// <summary>
        /// Gets the proceeded bytes.
        /// </summary>
        /// <value>The proceeded bytes.</value>
        public long ProceededBytes
        {
            get { return this.proceededBytes; }
            private set
            {
                if (this.ProceededBytes != value)
                {
                    this.proceededBytes = value;
                    this.OnPropertyChanged(vm => vm.ProceededBytes);
                }
            }
        }

        /// <summary>
        /// Gets the counted files.
        /// </summary>
        /// <value>The counted files.</value>
        public int CountedFiles
        {
            get { return this.countedFiles; }
            private set
            {
                if (this.CountedFiles != value)
                {
                    this.countedFiles = value;
                    this.OnPropertyChanged(vm => vm.CountedFiles);
                }
            }
        }

        /// <summary>
        /// Gets the proceeded files.
        /// </summary>
        /// <value>The proceeded files.</value>
        public int ProceededFiles
        {
            get { return this.proceededFiles; }
            private set
            {
                if (this.ProceededFiles != value)
                {
                    this.proceededFiles = value;
                    this.OnPropertyChanged(vm => vm.ProceededFiles);
                }
            }
        }

        /// <summary>
        /// Gets the job settings of the current running job.
        /// </summary>
        /// <value>The job settings of the current running job.</value>
        public JobSetting CurrentJobSettings
        {
            get { return this.currentJobSetting; }
            private set
            {
                if (this.CurrentJobSettings != value)
                {
                    this.currentJobSetting = value;
                    this.OnPropertyChanged(vm => vm.CurrentJobSettings);
                }
            }
        }

        /// <summary>
        /// Gets the log messages.
        /// </summary>
        /// <value>The log messages.</value>
        public ThreadSafeObservableCollection<LogMessage> LogMessages { get; private set; }

        /// <summary>
        /// Gets the status messages.
        /// </summary>
        /// <value>The status messages.</value>
        public string StatusMessages
        {
            get { return this.statusMessages; }
            private set
            {
                if (this.statusMessages != value)
                {
                    this.statusMessages = value;
                    this.OnPropertyChanged(vm => vm.StatusMessages);
                }
            }
        }

        /// <summary>
        /// Gets the last status message.
        /// </summary>
        /// <value>The last status message.</value>
        public string LastStatusMessage
        {
            get { return this.lastStatusMessage; }
            private set
            {
                if (this.statusMessages != value)
                {
                    this.lastStatusMessage = value;
                    this.OnPropertyChanged(vm => vm.LastStatusMessage);
                }
            }
        }

        /// <summary>
        /// Gets the pause or continue string.
        /// </summary>
        /// <value>The pause or continue string.</value>
        public string PauseOrContinueString
        {
            get { return this.IsPaused ? "Continue" : "Pause"; }
        }

        #endregion Properties

        #region Constructor

        /// <summary>
        /// Jobs the worker view model.
        /// </summary>
        public JobWorkerViewModel()
        {
            this.LogMessages = new ThreadSafeObservableCollection<LogMessage>();

            this.ResetJobWorker();
        }

        #endregion Constructor

        #region Public methods

        /// <summary>
        /// Resets the job worker.
        /// </summary>
        public void ResetJobWorker()
        {
            this.jobWorker = new JobWorker();
            this.jobWorker.JobStarted += new EventHandler<JobEventArgs>(jobWorker_JobStarted);
            this.jobWorker.FileProceeded += new EventHandler<FileProceededEventArgs>(jobWorker_FileProceeded);
            this.jobWorker.FilesCounted += new EventHandler(jobWorker_FilesCounted);
            this.jobWorker.DirectoryCreated += new EventHandler<DirectoryCreationEventArgs>(jobWorker_DirectoryCreated);
            this.jobWorker.DirectoryDeleted += new EventHandler<DirectoryDeletionEventArgs>(jobWorker_DirectoryDeleted);
            this.jobWorker.FileDeleted += new EventHandler<FileDeletionEventArgs>(jobWorker_FileDeleted);
            this.jobWorker.FoundModifiedFile += new EventHandler<FileCopyEventArgs>(jobWorker_FoundModifiedFile);
            this.jobWorker.FoundNewerFile += new EventHandler<FileCopyEventArgs>(jobWorker_FoundNewerFile);
            this.jobWorker.JobFinished += new EventHandler<JobEventArgs>(jobWorker_JobFinished);
            this.jobWorker.Finished += new EventHandler(jobWorker_Finished);

            this.ResetMessages();
            this.ResetBytes();
        }

        /// <summary>
        /// Starts the job worker.
        /// </summary>
        /// <param name="jobSettings">The job settings.</param>
        /// <param name="preview">if set to true, a preview will be performed.</param>
        public void StartJobWorker(IEnumerable<JobSetting> jobSettings, bool preview)
        {
            this.jobWorker.Start(jobSettings, preview);
            this.IsCounting = true;
            this.IsRunning = true;
            this.AddStatusMessage("Starting jobs.");
            this.AddStatusMessage("Counting files...");
        }

        /// <summary>
        /// Pauses the job worker.
        /// </summary>
        public void PauseJobWorker()
        {
            this.jobWorker.Pause();
            this.OnPropertyChanged(vm => vm.PauseOrContinueString);
            this.AddStatusMessage("Paused jobs.");
        }

        /// <summary>
        /// Continues the job worker.
        /// </summary>
        public void ContinueJobWorker()
        {
            this.jobWorker.Continue();
            this.OnPropertyChanged(vm => vm.PauseOrContinueString);
            this.AddStatusMessage("Continue jobs.");
            this.AddStatusMessage("Proceeding job: " + this.CurrentJobSettings.Name + "...");
        }

        /// <summary>
        /// Stops the job worker.
        /// </summary>
        public void StopJobWorker()
        {
            this.jobWorker.Stop();
            this.IsRunning = false;
            this.ResetBytes();
            this.AddStatusMessage("Stopped all jobs.");
        }

        #endregion Public methods

        #region Private methods

        /// <summary>
        /// Resets the proceeded and counted bytes to avoid that the statusbar is filled at startup of the application.
        /// </summary>
        private void ResetBytes()
        {
            this.ProceededBytes = 0;
            this.CountedBytes = 1024;
        }

        /// <summary>
        /// Resets the status and log messages.
        /// </summary>
        private void ResetMessages()
        {
            this.LogMessages.Clear();
            this.StatusMessages = String.Empty;
            this.lastStatusMessage = String.Empty;
        }

        /// <summary>
        /// Adds a status message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void AddStatusMessage(string message)
        {
            this.StatusMessages += message + Environment.NewLine;
            this.LastStatusMessage = message;
        }

        /// <summary>
        /// Adds the log message.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="action">The action.</param>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="targetPath">The target path.</param>
        private void AddLogMessage(string type, string action, string sourcePath, string targetPath)
        {
            this.LogMessages.Add(new LogMessage(type, action, sourcePath, targetPath));
        }

        /// <summary>
        /// Handles the Finished event of the jobWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void jobWorker_Finished(object sender, EventArgs e)
        {
            this.IsRunning = false;
            this.OnPropertyChanged(vm => vm.PauseOrContinueString);
            this.AddStatusMessage("Finished all jobs.");
        }

        /// <summary>
        /// Handles the JobFinished event of the jobWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.JobEventArgs"/> instance containing the event data.</param>
        private void jobWorker_JobFinished(object sender, JobEventArgs e)
        {
            this.AddStatusMessage("Finished job: " + e.Job.Name);
        }

        /// <summary>
        /// Handles the JobStarted event of the jobWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.JobEventArgs"/> instance containing the event data.</param>
        private void jobWorker_JobStarted(object sender, JobEventArgs e)
        {
            this.CurrentJobSettings = e.Job;
            this.AddStatusMessage("Proceeding job: " + e.Job.Name + "...");
        }

        /// <summary>
        /// Handles the FileProceeded event of the jobWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileProceededEventArgs"/> instance containing the event data.</param>
        private void jobWorker_FileProceeded(object sender, FileProceededEventArgs e)
        {
            this.ProceededFiles++;
            this.ProceededBytes += e.File.Length;
        }

        /// <summary>
        /// Handles the FilesCounted event of the jobWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void jobWorker_FilesCounted(object sender, EventArgs e)
        {
            this.IsCounting = false;
            this.CountedBytes = this.jobWorker.FileCounterResult.CountedBytes;
            this.CountedFiles = this.jobWorker.FileCounterResult.CountedFiles;

            this.AddStatusMessage("Finished file counting.");
        }

        /// <summary>
        /// Handles the FoundNewerFile event of the jobWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileCopyEventArgs"/> instance containing the event data.</param>
        private void jobWorker_FoundNewerFile(object sender, FileCopyEventArgs e)
        {
            this.AddLogMessage("File", "Created", e.File.FullName, e.TargetDirectory.FullName);
        }

        /// <summary>
        /// Handles the FoundModifiedFile event of the jobWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileCopyEventArgs"/> instance containing the event data.</param>
        private void jobWorker_FoundModifiedFile(object sender, FileCopyEventArgs e)
        {
            this.AddLogMessage("File", "Modified", e.File.FullName, Path.Combine(e.TargetDirectory.FullName, e.File.Name));
        }

        /// <summary>
        /// Handles the FileDeleted event of the jobWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileDeletionEventArgs"/> instance containing the event data.</param>
        private void jobWorker_FileDeleted(object sender, FileDeletionEventArgs e)
        {
            this.AddLogMessage("File", "Deleted", e.File.FullName, String.Empty);
        }

        /// <summary>
        /// Handles the DirectoryDeleted event of the jobWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.DirectoryDeletionEventArgs"/> instance containing the event data.</param>
        private void jobWorker_DirectoryDeleted(object sender, DirectoryDeletionEventArgs e)
        {
            this.AddLogMessage("Directory", "Deleted", e.Directory.FullName, String.Empty);
        }

        /// <summary>
        /// Handles the DirectoryCreated event of the jobWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.DirectoryCreationEventArgs"/> instance containing the event data.</param>
        private void jobWorker_DirectoryCreated(object sender, DirectoryCreationEventArgs e)
        {
            this.AddLogMessage("Directory", "Created", e.Directory.FullName, e.TargetDirectory.FullName);
        }

        #endregion Private methods
    }
}