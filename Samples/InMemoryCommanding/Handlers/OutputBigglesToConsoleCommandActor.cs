﻿using System;
using System.Threading;
using System.Threading.Tasks;
using AzureFromTheTrenches.Commanding.Abstractions;
using InMemoryCommanding.Commands;
using InMemoryCommanding.Results;

namespace InMemoryCommanding.Handlers
{
    class OutputBigglesToConsoleCommandHandler : ICancellableCommandHandler<OutputToConsoleCommand, CountResult>
    {
        public Task<CountResult> ExecuteAsync(OutputToConsoleCommand command, CountResult previousResult, CancellationToken cancellationToken)
        {
            Console.WriteLine($"{command.Message} Biggles");
            CountResult result = previousResult ?? new CountResult();
            result.Count++;
            return Task.FromResult(result);
        }
    }
}
