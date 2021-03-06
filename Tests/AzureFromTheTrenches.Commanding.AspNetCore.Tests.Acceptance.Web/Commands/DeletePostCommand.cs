﻿using System;
using AzureFromTheTrenches.Commanding.Abstractions;

namespace AzureFromTheTrenches.Commanding.AspNetCore.Tests.Acceptance.Web.Commands
{
    public class DeletePostCommand : ICommand
    {
        [SecurityProperty]
        public Guid UserId { get; set; }

        public Guid PostId { get; set; }
    }
}
