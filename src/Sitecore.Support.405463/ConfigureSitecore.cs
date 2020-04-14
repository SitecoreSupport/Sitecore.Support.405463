// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigureSitecore.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2017
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Sitecore.Commerce.Plugin.Sample
{
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.EntityViews;
    using Sitecore.Framework.Configuration;
    using Sitecore.Framework.Pipelines.Definitions.Extensions;

    /// <summary>
    /// The configure sitecore class.
    /// </summary>
    public class ConfigureSitecore : IConfigureSitecore
    {
        /// <summary>
        /// The configure services.
        /// </summary>
        /// <param name="services">
        /// The services.
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);

            services.Sitecore().Pipelines(config => config
                 .ConfigurePipeline<IDoActionPipeline>
                 (
                     configure => 
                     {
                         //configure.Replace<Plugin.BusinessUsers.DoActionLocalizePropertyBlock, Support.Pipelines.Blocks.DoActionLocalizePropertyBlock>();
                         configure.Replace<BusinessUsers.DoActionSelectLocalizablePropertyBlock, Support.Pipelines.Blocks.DoActionSelectLocalizablePropertyBlock>();
                         configure.Replace<BusinessUsers.DoActionLocalizePropertyBlock, Support.Pipelines.Blocks.DoActionLocalizePropertyBlock>();
                                  //.Replace<BusinessUsers.DoActionSelectLocalizablePropertyBlock, Support.Pipelines.Blocks.DoActionSelectLocalizablePropertyBlock>();
                     }
                 ));

            services.RegisterAllCommands(assembly);
        }
    }
}