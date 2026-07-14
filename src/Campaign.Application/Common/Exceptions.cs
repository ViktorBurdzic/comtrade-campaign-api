using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Application.Common;

// base for expected business errors; the API maps these to HTTP status codes
public abstract class AppException : Exception
{
    public int StatusCode { get; }

    protected AppException(string message, int statusCode, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}

public sealed class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message, 400) { }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404) { }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message, 409) { }
}

public sealed class DailyLimitExceededException : AppException
{
    public DailyLimitExceededException(string agentUsername, DateOnly date, int limit)
        : base($"Agent '{agentUsername}' has already rewarded {limit} customers on {date:yyyy-MM-dd}. " +
               $"The daily limit is {limit} customers per agent.", 409)
    {
    }
}

public sealed class ExternalServiceException : AppException
{
    public ExternalServiceException(string message, Exception? inner = null)
        : base(message, 502, inner) { }
}
