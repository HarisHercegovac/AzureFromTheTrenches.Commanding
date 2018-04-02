﻿using System;
using System.Threading.Tasks;
using AzureFromTheTrenches.Commanding.Abstractions;
using AzureFromTheTrenches.Commanding.AspNetCore.Tests.Acceptance.Web.Commands;
using AzureFromTheTrenches.Commanding.AspNetCore.Tests.Acceptance.Web.Commands.Responses;

namespace AzureFromTheTrenches.Commanding.AspNetCore.Tests.Acceptance.Web.Handlers
{
    public class GetPostQueryHandler : ICommandHandler<GetPostQuery, Post>
    {
        public Task<Post> ExecuteAsync(GetPostQuery command, Post previousResult)
        {
            throw new NotImplementedException();
        }
    }
}