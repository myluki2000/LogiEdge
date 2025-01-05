using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace LogiEdge.Shared.Conventions
{
    public class AutoIncludeAttributeConvention(ProviderConventionSetBuilderDependencies dependencies)
        : NavigationAttributeConventionBase<AutoIncludeAttribute>(dependencies),
            INavigationAddedConvention, ISkipNavigationAddedConvention
    {
        public override void ProcessNavigationAdded(IConventionNavigationBuilder navigationBuilder, AutoIncludeAttribute attribute,
            IConventionContext<IConventionNavigationBuilder> context)
        {
            navigationBuilder.AutoInclude(attribute.AutoInclude, true);
        }

        public override void ProcessSkipNavigationAdded(IConventionSkipNavigationBuilder skipNavigationBuilder, AutoIncludeAttribute attribute,
            IConventionContext<IConventionSkipNavigationBuilder> context)
        {
            skipNavigationBuilder.AutoInclude(attribute.AutoInclude, true);
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AutoIncludeAttribute(bool autoInclude = true) : Attribute
    {
        public bool AutoInclude { get; set; } = autoInclude;
    }
}
