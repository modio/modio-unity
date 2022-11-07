using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public partial class ProgressHandle
    {
        public async void CoupleToWebRequest(IModIoWebRequest webRequest,                                                         
                                             Func<bool> shouldShutDown)
        {            
            try
            {
                // We cache these outside of the while loop to create less garbage for the GC
                bool trackDownload = OperationType == ModManagementOperationType.Download;
                ulong lastUploadedBytes = 0;
                int updateRate = 10; // milliseconds between progress updates
                int sampleTimeInMilliseconds = 2000; // amount of time to calculate average  bytes/s
                int maxSamples = sampleTimeInMilliseconds / updateRate;
                ulong bytesForThisSample;
                ulong currentUploadedBytes;
                List<ulong> samples = new List<ulong>();
                ulong samplesTotalSize;
                long expectedDownloadSize = 0;

                //rewire this so that it works both up and down

                while(!Completed
                      && webRequest != null
                      && !webRequest.isDone
                      && !shouldShutDown())
                {
                    // Calculate kb/s
                    // First off, figure out if we're downloading or uploading
                    currentUploadedBytes = webRequest.uploadedBytes != 0
                        ? webRequest.uploadedBytes : webRequest.downloadedBytes;

                    bytesForThisSample = currentUploadedBytes - lastUploadedBytes;
                    lastUploadedBytes = currentUploadedBytes;

                    // Add this sample to the samples list
                    samples.Add(bytesForThisSample);
                    if(samples.Count > maxSamples)
                    {
                        samples.RemoveAt(0);
                    }

                    // Get the total samples size
                    samplesTotalSize = 0;
                    foreach(ulong p in samples)
                    { samplesTotalSize += p; }

                    // calculate the bytes per second average off of total sample size
                    if(samplesTotalSize != 0)
                    {
                        BytesPerSecond = (long)(samplesTotalSize / (ulong)(sampleTimeInMilliseconds/1000f));
                    }

                    Progress = trackDownload ? webRequest.downloadProgress : webRequest.uploadProgress;

                    if(trackDownload)
                    {
                        string contentLength = webRequest.GetResponseHeader("Content-Length");
                        expectedDownloadSize = BrokenDownloadProgressWorkaround(webRequest, this, expectedDownloadSize);
                    }

                    if(Progress < 0)
                    {
                        Progress = 0;
                    }

                    if(Progress >= 1f)
                    {
                        Progress = 1f;
                        break;
                    }

                    if(webRequest.isDone)
                    {
                        Progress = 1f;
                        break;
                    }

                    await Task.Delay(updateRate);
                }
            }
            catch(Exception e)
            {
                ModIO.Implementation.Logger.Log(ModIO.LogLevel.Warning,
                           $"ProgressHandle failed to stay paired with "
                               + $"WebRequest. Likely because the UnityWebRequest was"
                               + $" Disposed prematurely or finished during an awaited iteration. "
                               + $"(Exception: {e.Message})");
            }

            if(webRequest.isDone)
            {
                Completed = true;
                Progress = 1f;
            }
        }


        /// <summary>
        /// In Unity 2019.4.40f, progress is broken. This works around it by getting the response header and calculating
        /// the progress
        /// </summary>
        /// <returns></returns>
        private static long BrokenDownloadProgressWorkaround(IModIoWebRequest webRequest, ProgressHandle progressHandle, long expectedDownloadSize)
        {
            if(expectedDownloadSize == 0)
            {
                string contentLength = webRequest.GetResponseHeader("Content-Length");
                if(contentLength != string.Empty)
                {
                    expectedDownloadSize = long.Parse(contentLength);
                }
            }

            if(expectedDownloadSize != 0 && webRequest.downloadProgress < 1f)
            {
                var percentage = (decimal)webRequest.downloadedBytes / (decimal)expectedDownloadSize;
                progressHandle.Progress = Convert.ToSingle(percentage);
            }

            return expectedDownloadSize;
        }

    }
}
