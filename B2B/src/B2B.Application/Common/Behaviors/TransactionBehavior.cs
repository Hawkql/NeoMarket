using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Interface;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2B.Application.common.Behaviors
{
    public class TransactionBehavior<TRequest, TResponse> :IPipelineBehavior<TRequest,TResponse> 
        where TRequest : notnull
    {
        private readonly ITransactionManager _transactionManager;
        private readonly ILogger<TransactionBehavior<TRequest,TResponse>> _logger;
        public TransactionBehavior(ITransactionManager transactionManager,
            ILogger<TransactionBehavior<TRequest, TResponse>> logger
            )
        {
            _transactionManager = transactionManager;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            var requestName =typeof(TRequest).Name;
            var isCommand = requestName.EndsWith("Command");
            if(!isCommand)
                return await next();
            _logger.LogInformation("Beginning transaction for {Request}", requestName);
            await _transactionManager.BeginAsync(ct);
            try
            {
                var response = await next();
                await _transactionManager.CommitAsync(ct);
                _logger.LogInformation("Committed transaction for {Request}", requestName);
                return response;
            }
            catch (Exception ex)
            {
                await _transactionManager.RollbackAsync(ct);
                _logger.LogWarning(ex, "Rolled back transaction for {Request}", requestName);
                throw;
            }
        }
    }
}
