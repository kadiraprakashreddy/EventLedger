using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountService.Domain.Exceptions;

public class InvalidTransactionException : DomainException
{
    public InvalidTransactionException(string message) : base(message) { }
}
