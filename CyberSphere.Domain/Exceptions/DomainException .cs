using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Exceptions
{

    /// <summary>Base for all domain-layer exceptions.</summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
    }

    ///// <summary>Thrown when a requested resource does not exist.</summary>
    //public class NotFoundException : DomainException
    //{
    //    public NotFoundException(string entityName, object key)
    //        : base($"{entityName} with key '{key}' was not found.") { }
    //}

    /// <summary>Thrown when a business rule is violated.</summary>
    public class BusinessRuleException : DomainException
    {
        public BusinessRuleException(string message) : base(message) { }
    }

    ///// <summary>Thrown when authentication credentials are invalid.</summary>
    //public class UnauthorizedException : DomainException
    //{
    //    public UnauthorizedException(string message = "Invalid credentials.")
    //        : base(message) { }
    //}

    ///// <summary>Thrown when a conflict exists (e.g. duplicate email).</summary>
    //public class ConflictException : DomainException
    //{
    //    public ConflictException(string message) : base(message) { }
    //}
}
