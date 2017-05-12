﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccidentalFish.Commanding;
using AccidentalFish.Commanding.Model;
using AccidentalFish.DependencyResolver.MicrosoftNetStandard;
using InMemoryCommanding.Commands;
using InMemoryCommanding.Results;
using Microsoft.Extensions.DependencyInjection;

namespace InMemoryCommanding
{
    class StackDispatcher : ICommandDispatcher
    {
        private readonly Stack<object> _commandStack;

        public StackDispatcher(Stack<object> commandStack)
        {
            _commandStack = commandStack;
        }

        public Task<CommandResult<TResult>> DispatchAsync<TCommand, TResult>(TCommand command) where TCommand : class
        {
            _commandStack.Push(command);
            return Task.FromResult(new CommandResult<TResult>(default(TResult), true));
        }

        public ICommandExecuter AssociatedExecuter => null;
    }

    static class PushToStackWithDispatcher
    {
        public static async Task Run()
        {
            Stack<object> stack = new Stack<object>();
            ICommandDispatcher dispatcher = Configure(stack);
            await dispatcher.DispatchAsync<OutputToConsoleCommand, CountResult>(new OutputToConsoleCommand { Message = "Hello" });
            await dispatcher.DispatchAsync<OutputToConsoleCommand, CountResult>(new OutputToConsoleCommand { Message = "World" });

            while (stack.Any())
            {
                OutputToConsoleCommand command = (OutputToConsoleCommand) stack.Pop();
                Console.WriteLine(command.Message);
            }

            Console.WriteLine("\nPress a key to continue...");
        }

        private static ICommandDispatcher Configure(Stack<object> stack)
        {
            MicrosoftNetStandardDependencyResolver resolver = new MicrosoftNetStandardDependencyResolver(new ServiceCollection());
            resolver.UseCommanding(type => resolver.Register(type, type))
                .Register<OutputToConsoleCommand>(new StackDispatcher(stack));
            resolver.BuildServiceProvider();
            return resolver.Resolve<ICommandDispatcher>();
        }
    }
}
