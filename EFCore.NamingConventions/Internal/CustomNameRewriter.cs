using System;

namespace EFCore.NamingConventions.Internal;

public class CustomNameRewriter : INameRewriter
{
    private readonly Func<string, string> _mapper;

    public CustomNameRewriter(Func<string, string> mapper)
        => _mapper = mapper;

    public string RewriteName(string name)
        => _mapper.Invoke(name);
}
