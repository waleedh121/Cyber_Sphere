using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.Results;

namespace CyberSphere.Application.Exceptions
{

    public abstract class ApplicationException : Exception
    {
        protected ApplicationException(string message) : base(message) { }
    }

    public class NotFoundException : ApplicationException
    {
        public NotFoundException(string entity, object key)
            : base($"{entity} with key '{key}' was not found.") { }
    }

    public class ConflictException : ApplicationException
    {
        public ConflictException(string message) : base(message) { }
    }

    public class UnauthorizedException : ApplicationException
    {
        public UnauthorizedException(string message = "Invalid credentials.") : base(message) { }
    }

    public class ForbiddenException : ApplicationException
    {
        public ForbiddenException(string message = "Access denied.") : base(message) { }
    }

    public class ValidationException : ApplicationException
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("One or more validation errors occurred.")
        {
            Errors = errors;
        }
    }

    public class BadRequestException : ApplicationException
    {
        public BadRequestException(string message) : base(message) { }
    }

    public class ServiceUnavailableException : ApplicationException
    {
        public ServiceUnavailableException(string message) : base(message) { }
    }

}