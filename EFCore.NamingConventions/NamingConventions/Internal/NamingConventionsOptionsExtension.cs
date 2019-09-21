using System.Collections.Generic;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using JetBrains.Annotations;

namespace EFCore.NamingConventions.Internal
{
    public class NamingConventionsOptionsExtension : IDbContextOptionsExtension
    {
        DbContextOptionsExtensionInfo _info;
        NamingConvention _namingConvention;

        public NamingConventionsOptionsExtension() {}

        protected NamingConventionsOptionsExtension([NotNull] NamingConventionsOptionsExtension copyFrom)
            => _namingConvention = copyFrom._namingConvention;

        public virtual DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

        protected virtual NamingConventionsOptionsExtension Clone() => new NamingConventionsOptionsExtension(this);

        internal virtual NamingConvention NamingConvention => _namingConvention;

        public virtual NamingConventionsOptionsExtension WithoutNaming()
        {
            var clone = Clone();
            clone._namingConvention = NamingConvention.None;
            return clone;
        }

        public virtual NamingConventionsOptionsExtension WithSnakeCaseNamingConvention()
        {
            var clone = Clone();
            clone._namingConvention = NamingConvention.SnakeCase;
            return clone;
        }

        public void Validate(IDbContextOptions options) {}

        public void ApplyServices(IServiceCollection services)
            => services.AddEntityFrameworkNamingConventions();

        sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            string _logFragment;

            public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) {}

            new NamingConventionsOptionsExtension Extension
                => (NamingConventionsOptionsExtension)base.Extension;

            public override bool IsDatabaseProvider => false;

            public override string LogFragment
                => _logFragment ??= Extension._namingConvention switch
                {
                    NamingConvention.SnakeCase => "using snake-case naming ",
                    _ => ""
                };

            public override long GetServiceProviderHashCode() => Extension._namingConvention.GetHashCode();

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
                => debugInfo["Naming:" + nameof(NamingConventionsExtensions.UseSnakeCaseNamingConvention)]
                    = Extension._namingConvention.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }
    }
}
