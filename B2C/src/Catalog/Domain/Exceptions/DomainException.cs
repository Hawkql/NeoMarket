using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Exceptions
{
    /// <summary>Базовый класс доменных исключений</summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
        protected DomainException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Ресурс не найден — маппится в 404</summary>
    public sealed class NotFoundException : DomainException
    {
        public string ResourceName { get; }
        public string ResourceId { get; }

        public NotFoundException(string resourceName, string resourceId)
            : base($"{resourceName} with id '{resourceId}' was not found.")
        {
            ResourceName = resourceName;
            ResourceId = resourceId;
        }
    }

    /// <summary>Некорректные входные данные — маппится в 400</summary>
    public sealed class ValidationException : DomainException
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; }

        public ValidationException(string field, string message)
            : base($"Validation failed: {field} — {message}")
        {
            Errors = new Dictionary<string, string[]> { [field] = [message] };
        }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("Validation failed.")
        {
            Errors = new Dictionary<string, string[]>(errors);
        }
    }

    /// <summary>Нарушена иерархия категорий — маппится в 422</summary>
    public sealed class OrphanNodeException : DomainException
    {
        public OrphanNodeException(string categoryId)
            : base($"Category hierarchy is broken at id '{categoryId}'.") { }
    }

    /// <summary>Upstream-сервис недоступен — маппится в 503</summary>
    public sealed class UpstreamUnavailableException : DomainException
    {
        public string ServiceName { get; }

        public UpstreamUnavailableException(string serviceName, Exception? inner = null)
            : base($"Upstream service '{serviceName}' is temporarily unavailable.", inner!)
        {
            ServiceName = serviceName;
        }
    }

    /// <summary>Переданы взаимоисключающие параметры — маппится в 400</summary>
    public sealed class AmbiguousParameterException : DomainException
    {
        public AmbiguousParameterException(params string[] paramNames)
            : base($"Only one of [{string.Join(", ", paramNames)}] must be provided.") { }
    }

    /// <summary>Обязательный параметр не передан — маппится в 400</summary>
    public sealed class MissingParameterException : DomainException
    {
        public MissingParameterException(string paramName)
            : base($"Parameter '{paramName}' must be provided.") { }
    }
}
