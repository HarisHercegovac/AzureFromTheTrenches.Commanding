﻿using System;
using System.Linq;
using AzureFromTheTrenches.Commanding.Abstractions;
using AzureFromTheTrenches.Commanding.AzureFunctions.Builders;
using AzureFromTheTrenches.Commanding.AzureFunctions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AzureFromTheTrenches.Commanding.AzureFunctions
{
    public static class Runtime
    {
        public static readonly IServiceProvider ServiceProvider;
        private static readonly IServiceCollection ServiceCollection;

        static Runtime()
        {
            ServiceCollection = new ServiceCollection();
            CommandingDependencyResolverAdapter adapter = new CommandingDependencyResolverAdapter(
                (fromType, toInstance) => ServiceCollection.AddSingleton(fromType, toInstance),
                (fromType, toType) => ServiceCollection.AddTransient(fromType, toType),
                (resolveType) => ServiceProvider.GetService(resolveType)
            );

            // Find the configuration implementation
            ICommandRegistry commandRegistry;
            IFunctionAppConfiguration configuration = ConfigurationLocator.FindConfiguration();
            if (configuration is ICommandingConfigurator commandingConfigurator)
            {
                commandRegistry = commandingConfigurator.AddCommanding(adapter);
            }
            else
            {
                commandRegistry = adapter.AddCommanding();
            }

            // Register internal implementations
            ServiceCollection.AddTransient<ICommandClaimsBinder, CommandClaimsBinder>();
            ServiceCollection.AddTransient<ICommandDeserializer, CommandDeserializer>();

            // Invoke the builder process
            FunctionHostBuilder builder = new FunctionHostBuilder(ServiceCollection, commandRegistry);
            configuration.Build(builder);
            new PostBuildPatcher().Patch(builder, "");

            FunctionBuilder functionBuilder = (FunctionBuilder) builder.FunctionBuilder;
            AuthorizationBuilder authorizationBuilder = (AuthorizationBuilder) builder.AuthorizationBuilder;
            if (authorizationBuilder.TokenValidatorType != null)
            {
                ServiceCollection.AddTransient(typeof(ITokenValidator), authorizationBuilder.TokenValidatorType);
            }

            ICommandClaimsBinder commandClaimsBinder = authorizationBuilder.ClaimsMappingBuilder.Build(
                functionBuilder.GetHttpFunctionDefinitions().Select(x => x.CommandType).ToArray());
            ServiceCollection.AddSingleton(commandClaimsBinder);
            
            ServiceProvider = ServiceCollection.BuildServiceProvider();
        }

        public static ICommandDispatcher CommandDispatcher => ServiceProvider.GetService<ICommandDispatcher>();
    }
}
