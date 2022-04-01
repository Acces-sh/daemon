using System.Collections.Generic;
using Daemon.Application.Interfaces;

namespace Daemon.Application.Wrappers;

public class Result : IResult
{
    public List<string>? Messages { get; set; }
    public bool Succeeded { get; set; }
}

public class Result<T> : Result, IResult<T>
{
    public T Data { get; set; }
}

public class ErrorResult<T> : Result<T>
{
    public string Source { get; set; }

    public string Exception { get; set; }

    public string ErrorId { get; set; }
    public string SupportMessage { get; set; }
    public int StatusCode { get; set; }
}
