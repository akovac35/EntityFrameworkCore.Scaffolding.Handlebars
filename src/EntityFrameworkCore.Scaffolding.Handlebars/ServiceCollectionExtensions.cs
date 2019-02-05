﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EntityFrameworkCore.Scaffolding.Handlebars;
using EntityFrameworkCore.Scaffolding.Handlebars.Helpers;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.Design
{
    /// <summary>
    /// Helper methods for configuring code generation for Entity Framework Core using Handlebars templates.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     <para>
        ///         Registers the Handlebars scaffolding generator as a service in the <see cref="IServiceCollection" />.
        ///         This allows you to customize generated DbContext and entity type classes by modifying the Handlebars 
        ///         templates in the CodeTemplates folder.
        ///     </para>
        ///     <para>
        ///         Has <paramref name="configureOptions" /> that allow you to choose whether to generate only the DbContext class, 
        ///         only entity type classes, or both DbContext and entity type classes (the default).  It also allows you to exclude tables from the generation.
        ///         This can be useful when placing model classes in a separate class library.
        ///     </para>
        /// </summary>
        /// <param name="services"> The <see cref="IServiceCollection" /> to add services to. </param>
        /// <param name="configureOptions">Method for configuring options for reverse engineering classes from an existing database.</param>
        /// <returns>The same service collection so that multiple calls can be chained.</returns>
        public static IServiceCollection AddHandlebarsScaffolding(this IServiceCollection services,
            Action<HandlebarsScaffoldingOptions> configureOptions)
        {
            var options = new HandlebarsScaffoldingOptions();
            configureOptions(options);

            Type dbContextGeneratorImpl;
            var dbContextGeneratorType = typeof(ICSharpDbContextGenerator);
            if (options.ScaffoldingGeneration == ScaffoldingGeneration.DbContextOnly
                || options.ScaffoldingGeneration == ScaffoldingGeneration.DbContextAndEntities)
                dbContextGeneratorImpl = typeof(HbsCSharpDbContextGenerator);
            else
                dbContextGeneratorImpl = typeof(NullCSharpDbContextGenerator);
            services.AddSingleton(dbContextGeneratorType, dbContextGeneratorImpl);

            Type entityGeneratorImpl;
            var entityGeneratorType = typeof(ICSharpEntityTypeGenerator);
            if (options.ScaffoldingGeneration == ScaffoldingGeneration.EntitiesOnly
                || options.ScaffoldingGeneration == ScaffoldingGeneration.DbContextAndEntities)
                entityGeneratorImpl = typeof(HbsCSharpEntityTypeGenerator);
            else
                entityGeneratorImpl = typeof(NullCSharpEntityTypeGenerator);
            services.AddSingleton(entityGeneratorType, entityGeneratorImpl);

            services.AddSingleton<ITemplateFileService, FileSystemTemplateFileService>();
            services.AddSingleton<IDbContextTemplateService, HbsDbContextTemplateService>();
            services.AddSingleton<IEntityTypeTemplateService, HbsEntityTypeTemplateService>();
            services.AddSingleton<IModelCodeGenerator, HbsCSharpModelGenerator>();
            services.AddSingleton<IReverseEngineerScaffolder, HbsReverseEngineerScaffolder>();
            services.AddSingleton<IEntityTypeTransformationService, HbsEntityTypeTransformationService>();
            services.AddSingleton<IHbsHelperService, HbsHelperService>(provider =>
            {
                var helpers = new Dictionary<string, Action<TextWriter, Dictionary<string, object>, object[]>>
                {
                    {Constants.SpacesHelper, HandlebarsHelpers.SpacesHelper}
                };
                return new HbsHelperService(helpers);
            });
            return services;
        }

        /// <summary>
        /// Register Handlebars helpers.
        ///     <para>
        ///         Note: You must first call AddHandlebarsScaffolding before calling AddHandlebarsHelpers.
        ///     </para>
        /// </summary>
        /// <param name="services"> The <see cref="IServiceCollection" /> to add services to. </param>
        /// <param name="handlebarsHelpers">Handlebars helpers.</param>
        /// <returns>The same service collection so that multiple calls can be chained.</returns>
        public static IServiceCollection AddHandlebarsHelpers(this IServiceCollection services,
            params (string helperName, Action<TextWriter, Dictionary<string, object>, object[]> helperFunction)[] handlebarsHelpers)
        {
            services.AddSingleton<IHbsHelperService>(provider =>
            {
                var helpers = new Dictionary<string, Action<TextWriter, Dictionary<string, object>, object[]>>
                {
                    {Constants.SpacesHelper, HandlebarsHelpers.SpacesHelper}
                };
                handlebarsHelpers.ToList().ForEach(h => helpers.Add(h.helperName, h.helperFunction));
                return new HbsHelperService(helpers);
            });
            return services;
        }

        /// <summary>
        /// Register Handlebars transformers.
        ///     <para>
        ///         Note: You must first call AddHandlebarsScaffolding before calling AddHandlebarsTransformers.
        ///     </para>
        /// </summary>
        /// <param name="services"> The <see cref="IServiceCollection" /> to add services to. </param>
        /// <param name="entityNameTransformer">Entity name transformer.</param>
        /// <param name="entityFileNameTransformer">Entity file name transformer.</param>
        /// <param name="constructorTransformer"></param>
        /// <param name="propertyTransformer">Property name transformer.</param>
        /// <param name="navPropertyTransformer">Navigation property name transformer.</param>
        /// <returns>The same service collection so that multiple calls can be chained.</returns>
        public static IServiceCollection AddHandlebarsTransformers(this IServiceCollection services,
            Func<string, string> entityNameTransformer = null,
            Func<string, string> entityFileNameTransformer = null,
            Func<EntityPropertyInfo, EntityPropertyInfo> constructorTransformer = null,
            Func<EntityPropertyInfo, EntityPropertyInfo> propertyTransformer = null,
            Func<EntityPropertyInfo, EntityPropertyInfo> navPropertyTransformer = null)
        {
            services.AddSingleton<IEntityTypeTransformationService>(provider =>
                new HbsEntityTypeTransformationService(
                    entityNameTransformer,
                    entityFileNameTransformer,
                    constructorTransformer,
                    propertyTransformer,
                    navPropertyTransformer));
            return services;
        }
    }
}