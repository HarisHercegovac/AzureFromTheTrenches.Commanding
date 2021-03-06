﻿using AzureFromTheTrenches.Commanding.Abstractions;
using AzureFromTheTrenches.Commanding.Abstractions.Model;

namespace AzureStorageQueueCommanding.Commands
{
    public class OutputToConsoleCommand : ICommand<DeferredCommandResult>
    {
        public string Message { get; set; }
    }
}
