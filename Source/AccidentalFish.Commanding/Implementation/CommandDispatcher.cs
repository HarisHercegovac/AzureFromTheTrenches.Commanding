﻿using System;
using System.Threading.Tasks;
using AccidentalFish.Commanding.Model;

namespace AccidentalFish.Commanding.Implementation
{
    internal class CommandDispatcher : ICommandDispatcher
    {
        private readonly ICommandRegistry _commandRegistry;
        private readonly ICommandScopeManager _commandScopeManager;
        private readonly ICommandAuditorFactory _commandAuditorFactory;

        public CommandDispatcher(ICommandRegistry commandRegistry,
            ICommandExecuter commandExecuter,
            ICommandScopeManager commandScopeManager,
            ICommandAuditorFactory commandAuditorFactory)
        {
            _commandRegistry = commandRegistry;
            _commandScopeManager = commandScopeManager;
            _commandAuditorFactory = commandAuditorFactory;
            AssociatedExecuter = commandExecuter;
        }

        public async Task<CommandResult<TResult>> DispatchAsync<TCommand, TResult>(TCommand command) where TCommand : class
        {
            ICommandContext context = _commandScopeManager.Enter();
            try
            {
                CommandResult<TResult> dispatchResult = null;
                ICommandExecuter executer = null;
                ICommandDispatcher dispatcher = null;

                ICommandAuditor auditor = _commandAuditorFactory.Create<TCommand>();
                if (auditor != null)
                {
                    await auditor.Audit(command, context);
                }

                try
                {
                    Func<ICommandDispatcher> dispatcherFunc = _commandRegistry.GetCommandDispatcherFactory<TCommand>();
                    if (dispatcherFunc != null)
                    {
                        dispatcher = dispatcherFunc();
                        dispatchResult = await dispatcher.DispatchAsync<TCommand, TResult>(command);
                        executer = dispatcher.AssociatedExecuter;
                    }

                    if (dispatchResult != null && dispatchResult.DeferExecution)
                    {
                        return new CommandResult<TResult>(default(TResult), true);
                    }
                }
                catch (Exception ex)
                {
                    throw new CommandDispatchException<TCommand>(command, context.Copy(), dispatcher?.GetType() ?? GetType(), "Error occurred during command dispatch", ex);
                }
                
                if (executer == null)
                {
                    executer = AssociatedExecuter;
                }
                return new CommandResult<TResult>(await executer.ExecuteAsync<TCommand, TResult>(command), false);
            }
            finally
            {
                _commandScopeManager.Exit();
            }
        }

        public Task<CommandResult<NoResult>> DispatchAsync<TCommand>(TCommand command) where TCommand : class
        {
            return DispatchAsync<TCommand, NoResult>(command);
        }

        public ICommandExecuter AssociatedExecuter { get; }
    }
}
