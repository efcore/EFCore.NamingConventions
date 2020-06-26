using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EFCore.NamingConventions.Internal
{
    class UpperSnakeCaseNameRewriter : SnakeCaseNameRewriter
    {
        private readonly CultureInfo _culture;

        public UpperSnakeCaseNameRewriter(CultureInfo culture) : base(culture) => _culture = culture;

        protected override string RewriteName(string name) => base.RewriteName(name).ToUpper(_culture);
    }
}
