﻿using System;
using System.Collections.Generic;
using System.Threading;
using FlagLib.IO;
using FlagSync.Core.FileSystem;

namespace FlagSync.Core
{
    public sealed class JobWorker
    {
        private Job currentJob;
        private readonly Queue<Job> jobQueue;
        private long totalWrittenBytes;
        private int proceededFiles;
        private FileCounterResult fileCounterResult;
        private bool performPreview;

        /// <summary>
        /// Occurs when a file has been proceeded.
        /// </summary>
        public event EventHandler<FileProceededEventArgs> ProceededFile;

        /// <summary>
        /// Occurs before a file gets deleted.
        /// </summary>
        public event EventHandler<FileDeletionEventArgs> DeletingFile;

        /// <summary>
        /// Occurs when a file has been deleted.
        /// </summary>
        public event EventHandler<FileDeletionEventArgs> DeletedFile;

        /// <summary>
        /// Occurs before a new file gets created.
        /// </summary>
        public event EventHandler<FileCopyEventArgs> CreatingFile;

        /// <summary>
        /// Occurs when a new file has been created.
        /// </summary>
        public event EventHandler<FileCopyEventArgs> CreatedFile;

        /// <summary>
        /// Occurs before a file gets modified.
        /// </summary>
        public event EventHandler<FileCopyEventArgs> ModifyingFile;

        /// <summary>
        /// Occurs when a file has been modified.
        /// </summary>
        public event EventHandler<FileCopyEventArgs> ModifiedFile;

        /// <summary>
        /// Occurs before a new directory gets created.
        /// </summary>
        public event EventHandler<DirectoryCreationEventArgs> CreatingDirectory;

        /// <summary>
        /// Occurs when a new directory has been created.
        /// </summary>
        public event EventHandler<DirectoryCreationEventArgs> CreatedDirectory;

        /// <summary>
        /// Occurs before a directory has been deleted.
        /// </summary>
        public event EventHandler<DirectoryDeletionEventArgs> DeletingDirectory;

        /// <summary>
        /// Occurs when a directory has been deleted.
        /// </summary>
        public event EventHandler<DirectoryDeletionEventArgs> DeletedDirectory;

        /// <summary>
        /// Occurs when a file copy error has occured.
        /// </summary>
        public event EventHandler<FileCopyErrorEventArgs> FileCopyError;

        /// <summary>
        /// Occurs when a file deletion error has occured.
        /// </summary>
        public event EventHandler<FileDeletionErrorEventArgs> FileDeletionError;

        /// <summary>
        /// Occurs when a directory deletion error has been catched.
        /// </summary>
        public event EventHandler<DirectoryDeletionEventArgs> DirectoryDeletionError;

        /// <summary>
        /// Occurs when the file copy progress has changed.
        /// </summary>
        public event EventHandler<DataTransferEventArgs> FileCopyProgressChanged;

        /// <summary>
        /// Occurs when the job worker has finished.
        /// </summary>
        public event EventHandler Finished;

        /// <summary>
        /// Occurs when the files had been counted.
        /// </summary>
        public event EventHandler FilesCounted;

        /// <summary>
        /// Occurs when a job has started.
        /// </summary>
        public event EventHandler<JobEventArgs> JobStarted;

        /// <summary>
        /// Occurs when a job has finished.
        /// </summary>
        public event EventHandler<JobEventArgs> JobFinished;

        /// <summary>
        /// Gets the total written bytes.
        /// </summary>
        /// <value>
        /// The total written bytes.
        /// </value>
        public long TotalWrittenBytes
        {
            get { return this.totalWrittenBytes; }
        }

        /// <summary>
        /// Gets the proceeded files.
        /// </summary>
        /// <value>
        /// The proceeded files.
        /// </value>
        public int ProceededFiles
        {
            get { return this.proceededFiles; }
        }

        /// <summary>
        /// Gets the file counter result.
        /// </summary>
        /// <value>
        /// The file counter result.
        /// </value>
        public FileCounterResult FileCounterResult
        {
            get { return this.fileCounterResult; }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="JobWorker"/> is paused.
        /// </summary>
        /// <value>
        /// true if paused; otherwise, false.
        /// </value>
        public bool IsPaused
        {
            get { return this.currentJob != null && this.currentJob.IsPaused; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobWorker"/> class.
        /// </summary>
        public JobWorker()
        {
            this.jobQueue = new Queue<Job>();
        }

        /// <summary>
        /// Stops the job worker.
        /// </summary>
        public void Stop()
        {
            if (currentJob != null)
            {
                this.currentJob.Stop();
                this.jobQueue.Clear();
            }
        }

        /// <summary>
        /// Pauses the job worker.
        /// </summary>
        public void Pause()
        {
            if (currentJob != null)
            {
                this.currentJob.Pause();
            }
        }

        /// <summary>
        /// Continues the job worker.
        /// </summary>
        public void Continue()
        {
            if (currentJob != null)
            {
                this.currentJob.Continue();
            }
        }

        /// <summary>
        /// Starts the specified jobs asynchronous.
        /// </summary>
        /// <param name="jobs">The jobs to start.</param>
        /// <param name="preview">if set to true, a preview will be performed.</param>
        public void StartAsync(IEnumerable<Job> jobs, bool preview)
        {
            ThreadPool.QueueUserWorkItem(callback => this.Start(jobs, preview));
        }

        /// <summary>
        /// Starts the specified jobs.
        /// </summary>
        /// <param name="jobs">The jobs to start.</param>
        /// <param name="preview">if set to true, a preview will be performed.</param>
        public void Start(IEnumerable<Job> jobs, bool preview)
        {
            this.totalWrittenBytes = 0;

            foreach (Job job in jobs)
            {
                this.jobQueue.Enqueue(job);
            }

            this.InternStart(preview);
        }

        /// <summary>
        /// Does the next job.
        /// </summary>
        private void DoNextJob()
        {
            if (this.jobQueue.Count > 0)
            {
                this.currentJob = this.jobQueue.Dequeue();
                this.InitializeJobEvents(this.currentJob);
                this.OnJobStarted(new JobEventArgs(this.currentJob));
                this.currentJob.Start(this.performPreview);
            }

            else
            {
                this.OnFinished(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Starts the job worker.
        /// </summary>
        private void InternStart(bool preview)
        {
            this.performPreview = preview;

            this.fileCounterResult = this.CountFiles();

            if (this.FilesCounted != null)
            {
                this.FilesCounted(this, new EventArgs());
            }

            this.DoNextJob();
        }

        /// <summary>
        /// Counts the files of all jobs together.
        /// </summary>
        /// <returns>
        /// The result of the counting.
        /// </returns>
        private FileCounterResult CountFiles()
        {
            var result = new FileCounterResult();

            foreach (Job job in this.jobQueue)
            {
                result += job.CountFiles();
            }

            return result;
        }

        /// <summary>
        /// Initializes the job events.
        /// </summary>
        /// <param name="job">The job.</param>
        private void InitializeJobEvents(Job job)
        {
            job.CreatedDirectory += currentJob_CreatedDirectory;
            job.CreatedFile += currentJob_CreatedFile;
            job.CreatingDirectory += currentJob_CreatingDirectory;
            job.CreatingFile += currentJob_CreatingFile;
            job.DeletedDirectory += currentJob_DeletedDirectory;
            job.DeletedFile += currentJob_DeletedFile;
            job.DeletingDirectory += currentJob_DeletingDirectory;
            job.DeletingFile += currentJob_DeletingFile;
            job.DirectoryDeletionError += currentJob_DirectoryDeletionError;
            job.FileCopyError += currentJob_FileCopyError;
            job.FileCopyProgressChanged += currentJob_FileCopyProgressChanged;
            job.FileDeletionError += currentJob_FileDeletionError;
            job.Finished += currentJob_Finished;
            job.ModifiedFile += currentJob_ModifiedFile;
            job.ModifyingFile += currentJob_ModifyingFile;
            job.ProceededFile += currentJob_ProceededFile;
        }

        /// <summary>
        /// Handles the ProceededFile event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileProceededEventArgs"/> instance containing the event data.</param>
        private void currentJob_ProceededFile(object sender, FileProceededEventArgs e)
        {
            if (this.ProceededFile != null)
            {
                this.ProceededFile(this, e);
            }

            this.proceededFiles++;
        }

        /// <summary>
        /// Handles the ModifyingFile event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileCopyEventArgs"/> instance containing the event data.</param>
        private void currentJob_ModifyingFile(object sender, FileCopyEventArgs e)
        {
            if (this.ModifyingFile != null)
            {
                this.ModifyingFile(this, e);
            }
        }

        /// <summary>
        /// Handles the ModifiedFile event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileCopyEventArgs"/> instance containing the event data.</param>
        private void currentJob_ModifiedFile(object sender, FileCopyEventArgs e)
        {
            if (this.ModifiedFile != null)
            {
                this.ModifiedFile(this, e);
            }
        }

        /// <summary>
        /// Handles the FileDeletionError event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileDeletionErrorEventArgs"/> instance containing the event data.</param>
        private void currentJob_FileDeletionError(object sender, FileDeletionErrorEventArgs e)
        {
            if (this.FileDeletionError != null)
            {
                this.FileDeletionError(this, e);
            }
        }

        /// <summary>
        /// Handles the FileCopyProgressChanged event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagLib.IO.DataTransferEventArgs"/> instance containing the event data.</param>
        private void currentJob_FileCopyProgressChanged(object sender, DataTransferEventArgs e)
        {
            if (this.FileCopyProgressChanged != null)
            {
                this.FileCopyProgressChanged(this, e);
            }
        }

        /// <summary>
        /// Handles the FileCopyError event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileCopyErrorEventArgs"/> instance containing the event data.</param>
        private void currentJob_FileCopyError(object sender, FileCopyErrorEventArgs e)
        {
            if (this.FileCopyError != null)
            {
                this.FileCopyError(this, e);
            }
        }

        /// <summary>
        /// Handles the DirectoryDeletionError event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.DirectoryDeletionEventArgs"/> instance containing the event data.</param>
        private void currentJob_DirectoryDeletionError(object sender, DirectoryDeletionEventArgs e)
        {
            if (this.DirectoryDeletionError != null)
            {
                this.DirectoryDeletionError(this, e);
            }
        }

        /// <summary>
        /// Handles the DeletingFile event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileDeletionEventArgs"/> instance containing the event data.</param>
        private void currentJob_DeletingFile(object sender, FileDeletionEventArgs e)
        {
            if (this.DeletingFile != null)
            {
                this.DeletingFile(this, e);
            }
        }

        /// <summary>
        /// Handles the DeletingDirectory event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.DirectoryDeletionEventArgs"/> instance containing the event data.</param>
        private void currentJob_DeletingDirectory(object sender, DirectoryDeletionEventArgs e)
        {
            if (this.DeletingDirectory != null)
            {
                this.DeletingDirectory(this, e);
            }
        }

        /// <summary>
        /// Handles the DeletedFile event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileDeletionEventArgs"/> instance containing the event data.</param>
        private void currentJob_DeletedFile(object sender, FileDeletionEventArgs e)
        {
            if (this.DeletedFile != null)
            {
                this.DeletedFile(this, e);
            }
        }

        /// <summary>
        /// Handles the DeletedDirectory event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.DirectoryDeletionEventArgs"/> instance containing the event data.</param>
        private void currentJob_DeletedDirectory(object sender, DirectoryDeletionEventArgs e)
        {
            if (this.DeletedDirectory != null)
            {
                this.DeletedDirectory(this, e);
            }
        }

        /// <summary>
        /// Handles the CreatingFile event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileCopyEventArgs"/> instance containing the event data.</param>
        private void currentJob_CreatingFile(object sender, FileCopyEventArgs e)
        {
            if (this.CreatingFile != null)
            {
                this.CreatingFile(this, e);
            }
        }

        /// <summary>
        /// Handles the CreatingDirectory event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.DirectoryCreationEventArgs"/> instance containing the event data.</param>
        private void currentJob_CreatingDirectory(object sender, DirectoryCreationEventArgs e)
        {
            if (this.CreatingDirectory != null)
            {
                this.CreatingDirectory(this, e);
            }
        }

        /// <summary>
        /// Handles the CreatedFile event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.FileCopyEventArgs"/> instance containing the event data.</param>
        private void currentJob_CreatedFile(object sender, FileCopyEventArgs e)
        {
            if (this.CreatedFile != null)
            {
                this.CreatedFile(this, e);
            }
        }

        /// <summary>
        /// Handles the CreatedDirectory event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FlagSync.Core.DirectoryCreationEventArgs"/> instance containing the event data.</param>
        private void currentJob_CreatedDirectory(object sender, DirectoryCreationEventArgs e)
        {
            if (this.CreatedDirectory != null)
            {
                this.CreatedDirectory(this, e);
            }
        }

        /// <summary>
        /// Handles the Finished event of the currentJob control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void currentJob_Finished(object sender, EventArgs e)
        {
            Job job = (Job)sender;

            this.OnJobFinished(new JobEventArgs(job));

            this.totalWrittenBytes += job.WrittenBytes;

            this.DoNextJob();
        }

        /// <summary>
        /// Raises the <see cref="Finished"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnFinished(EventArgs e)
        {
            if (this.Finished != null)
            {
                this.Finished(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="JobStarted"/> event.
        /// </summary>
        /// <param name="e">The <see cref="FlagSync.Core.JobEventArgs"/> instance containing the event data.</param>
        private void OnJobStarted(JobEventArgs e)
        {
            if (this.JobStarted != null)
            {
                this.JobStarted(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="JobFinished"/> event.
        /// </summary>
        /// <param name="e">The <see cref="FlagSync.Core.JobEventArgs"/> instance containing the event data.</param>
        private void OnJobFinished(JobEventArgs e)
        {
            if (this.JobFinished != null)
            {
                this.JobFinished(this, e);
            }
        }
    }
}