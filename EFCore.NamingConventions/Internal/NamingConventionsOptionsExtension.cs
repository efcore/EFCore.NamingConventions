using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.NamingConventions.Internal;

public class NamingConventionsOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;
    private NamingConvention _namingConvention;
    private CultureInfo? _culture;
    private bool _ignoreMigrationTable;

    public NamingConventionsOptionsExtension() {}
    protected NamingConventionsOptionsExtension(NamingConventionsOptionsExtension copyFrom)
    {
        _namingConvention = copyFrom._namingConvention;
        _culture = copyFrom._culture;
        _ignoreMigrationTable = copyFrom._ignoreMigrationTable;
    }

    public virtual DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    protected virtual NamingConventionsOptionsExtension Clone() => new(this);

    internal virtual NamingConvention NamingConvention => _namingConvention;
    internal virtual CultureInfo? Culture => _culture;
    internal virtual bool IgnoreMigrationTable => _ignoreMigrationTable;

    public virtual NamingConventionsOptionsExtension WithoutNaming()
    {
        var clone = Clone();
        clone._namingConvention = NamingConvention.None;
        return clone;
    }

    public virtual NamingConventionsOptionsExtension WithSnakeCaseNamingConvention(CultureInfo? culture = null, bool ignoreMigrationTable = false)
    {
        var clone = Clone();
        clone._namingConvention = NamingConvention.SnakeCase;
        clone._culture = culture;
        clone._ignoreMigrationTable = ignoreMigrationTable;
        return clone;
    }

    public virtual NamingConventionsOptionsExtension WithLowerCaseNamingConvention(CultureInfo? culture = null, bool ignoreMigrationTable = false)
    {
        var clone = Clone();
        clone._namingConvention = NamingConvention.LowerCase;
        clone._culture = culture;
        clone._ignoreMigrationTable = ignoreMigrationTable;
        return clone;
    }

    public virtual NamingConventionsOptionsExtension WithUpperCaseNamingConvention(CultureInfo? culture = null, bool ignoreMigrationTable = false)
    {
        var clone = Clone();
        clone._namingConvention = NamingConvention.UpperCase;
        clone._culture = culture;
        clone._ignoreMigrationTable = ignoreMigrationTable;
        return clone;
    }

    public virtual NamingConventionsOptionsExtension WithUpperSnakeCaseNamingConvention(CultureInfo? culture = null, bool ignoreMigrationTable = false)
    {
        var clone = Clone();
        clone._namingConvention = NamingConvention.UpperSnakeCase;
        clone._culture = culture;
        clone._ignoreMigrationTable = ignoreMigrationTable;
        return clone;
    }

    public virtual NamingConventionsOptionsExtension WithCamelCaseNamingConvention(CultureInfo? culture = null, bool ignoreMigrationTable = false)
    {
        var clone = Clone();
        clone._namingConvention = NamingConvention.CamelCase;
        clone._culture = culture;
        clone._ignoreMigrationTable = ignoreMigrationTable;
        return clone;
    }

    public void Validate(IDbContextOptions options) {}

    public void ApplyServices(IServiceCollection services)
        => services.AddEntityFrameworkNamingConventions();

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        private string? _logFragment;

        public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) {}

        private new NamingConventionsOptionsExtension Extension
            => (NamingConventionsOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment
        {
            get
            {
                if (_logFragment == null)
                {
                    var builder = new StringBuilder();

                    builder.Append(Extension._namingConvention switch
                    {
                        NamingConvention.None => "using default naming",
                        NamingConvention.SnakeCase => "using snake-case naming",
                        NamingConvention.LowerCase => "using lower case naming",
                        NamingConvention.UpperCase => "using upper case naming",
                        NamingConvention.UpperSnakeCase => "using upper snake-case naming",
                        NamingConvention.CamelCase => "using camel-case naming",
                        _ => throw new ArgumentOutOfRangeException("Unhandled enum value: " + Extension._namingConvention)
                    });

                    if (Extension._ignoreMigrationTable)
                    {
                        builder
                            .Append(" ignoring the migrations table");
                    }

                    if (Extension._culture is null)
                    {
                        builder
                            .Append(" (culture=")
                            .Append(Extension._culture)
                            .Append(")");
                    }

                    _logFragment = builder.ToString();
                }

                return _logFragment;
            }
        }

        public override int GetServiceProviderHashCode()
        {
            var hashCode = Extension._namingConvention.GetHashCode();
            hashCode = (hashCode * 3) ^ (Extension._culture?.GetHashCode() ?? 0);
            hashCode = (hashCode * 7) ^ (Extension._ignoreMigrationTable.GetHashCode());
            return hashCode;
        }

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["Naming:UseNamingConvention"]
                = Extension._namingConvention.GetHashCode().ToString(CultureInfo.InvariantCulture);

            debugInfo["Naming:IgnoreMigrationTable"]
                    = Extension._ignoreMigrationTable.GetHashCode().ToString(CultureInfo.InvariantCulture);

            if (Extension._culture != null)
            {
                debugInfo["Naming:Culture"]
                    = Extension._culture.GetHashCode().ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
