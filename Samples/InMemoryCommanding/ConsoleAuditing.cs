﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AccidentalFish.Commanding;
using AccidentalFish.DependencyResolver.MicrosoftNetStandard;
using InMemoryCommanding.Actors;
using InMemoryCommanding.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace InMemoryCommanding
{
    internal class ConsoleAuditor : ICommandAuditor
    {
        public Task Audit<TCommand>(TCommand command, ICommandContext context) where TCommand : class
        {
            ConsoleColor previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Type: {command.GetType()}");
            Console.WriteLine($"Correlation ID: {context.CorrelationId}");
            Console.WriteLine($"Depth: {context.Depth}");
            foreach (KeyValuePair<string, object> enrichedProperty in context.AdditionalProperties)
            {
                Console.WriteLine($"{enrichedProperty.Key}: {enrichedProperty.Value}");
            }
            Console.ForegroundColor = previousColor;
            return Task.FromResult(0);
        }
    }

    internal class ConsoleAuditorFactory : ICommandAuditorFactory
    {
        public ICommandAuditor Create<TCommand>() where TCommand : class
        {
            return new ConsoleAuditor();
        }
    }

    static class ConsoleAuditing
    {
        private static int _counter = -1;

        public static async Task Run()
        {
            ICommandDispatcher dispatcher = Configure();
            ChainCommand command = new ChainCommand();
            await dispatcher.DispatchAsync(command);
            Console.WriteLine("\nPress a key to continue...");
        }

        private static ICommandDispatcher Configure()
        {
            // we use an enricher that simply updates a counter each time enrichment occurs
            // as enrichment only occurs when the context is created this will start at 0 when the console auditing example is first run and
            // will increment by 1 on each subsequent run
            IReadOnlyDictionary<string, object> Enricher(IReadOnlyDictionary<string, object> existing) => new Dictionary<string, object> {{"Counter", Interlocked.Increment(ref _counter)}};

            MicrosoftNetStandardDependencyResolver resolver = new MicrosoftNetStandardDependencyResolver(new ServiceCollection());
            Options options = new Options
            {
                CommandActorContainerRegistration = type => resolver.Register(type, type),
                Reset = true, // we reset the registry because we allow repeat runs, in a normal app this isn't required
                Enrichers = new[]
                    {(Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>>) Enricher}
            };
            resolver.UseCommanding(options) 
                .Register<ChainCommand, ChainCommandActor>()
                .Register<OutputToConsoleCommand, OutputWorldToConsoleCommandActor>()
                .Register<OutputToConsoleCommand, OutputBigglesToConsoleCommandActor>();
            resolver.Register<ICommandAuditorFactory, ConsoleAuditorFactory>();
            resolver.BuildServiceProvider();
            return resolver.Resolve<ICommandDispatcher>();
        }
    }
}
