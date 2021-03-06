﻿using Microsoft.WindowsAzure.Storage.Queue;

namespace AzureFromTheTrenches.Commanding.AzureStorage.Implementation
{
    internal class CloudAuditQueueProvider : ICloudAuditQueueProvider
    {
        public CloudAuditQueueProvider(CloudQueue queue, CloudQueue deadLetterQueue)
        {
            Queue = queue;
            DeadLetterQueue = deadLetterQueue;
        }

        public CloudQueue Queue { get; }

        public CloudQueue DeadLetterQueue { get; }
    }
}
