using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Support.Commands
{
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Core.Commands;
    using Sitecore.Commerce.EntityViews;
    using Sitecore.Commerce.Plugin.BusinessUsers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class LocalizeEntityPropertyCommandSupport : LocalizeEntityPropertyCommand
    {
        private readonly IFindEntityPipeline _findPipeline;

        private readonly IPersistEntityPipeline _persistPipeline;

        public LocalizeEntityPropertyCommandSupport(IFindEntityPipeline findEntityPipeline, IPersistEntityPipeline persistEntityPipeline, IServiceProvider serviceProvider)
            : base(findEntityPipeline, persistEntityPipeline, serviceProvider)
        {
            _findPipeline = findEntityPipeline;
            _persistPipeline = persistEntityPipeline;
        }

        public override async Task<LocalizationEntity> GetLocalizationEntity(CommerceContext commerceContext, CommerceEntity entity)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                CommercePipelineExecutionContextOptions context = commerceContext.GetPipelineContextOptions();
                if (entity.HasComponent<LocalizedEntityComponent>())
                {
                    string id = entity.GetComponent<LocalizedEntityComponent>().Entity.EntityTarget;
                    LocalizationEntity localizationEntity = (await _findPipeline.Run(new FindEntityArgument(typeof(LocalizationEntity), id, entity.EntityVersion), context)) as LocalizationEntity;
                    if (localizationEntity != null)
                    {
                        return localizationEntity;
                    }
                    await context.CommerceContext.AddMessage(commerceContext.GetPolicy<KnownResultCodes>().Error, "EntityNotFound", new object[1]
                    {
                    id
                    }, "Entity '" + id + "' was not found.");
                    return null;
                }
                return new LocalizationEntity
                {
                    Id = $"{CommerceEntity.IdPrefix<LocalizationEntity>()}{Guid.NewGuid():N}"
                };
            }
        }

        public override async Task Process(CommerceContext commerceContext, CommerceEntity entity, EntityView entityView, LocalizeEntityPolicy policy, string propertyName, List<string> languages)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                KnownResultCodes errorCodes = commerceContext.GetPolicy<KnownResultCodes>();
                List<Model> localizedViews = entityView.ChildViews.Where((Model v) => v.Name.Equals(commerceContext.GetPolicy<KnownBusinessUsersViewsPolicy>().LocalizedValue, StringComparison.OrdinalIgnoreCase)).ToList();
                if (!localizedViews.Any())
                {
                    await commerceContext.AddMessage(errorCodes.ValidationError, "InvalidOrMissingPropertyValue", new object[1]
                    {
                    "LocalizedValues"
                    }, "Invalid or missing value for property 'LocalizedValues'.");
                }
                else
                {
                    LocalizationEntity localizationEntity = await GetLocalizationEntity(commerceContext, entity);
                    if (localizationEntity != null)
                    {
                        List<Parameter> localizations = new List<Parameter>();
                        bool flag = false;
                        string defaultLanguage = commerceContext.GetPolicy<GlobalEnvironmentPolicy>().DefaultLocale;
                        foreach (EntityView item in localizedViews.OfType<EntityView>())
                        {
                            string text = item.Properties.FirstOrDefault((ViewProperty p) => p.Name.Equals("Language", StringComparison.OrdinalIgnoreCase))?.Value;
                            if (string.IsNullOrEmpty(text) || !languages.Contains(text, StringComparer.OrdinalIgnoreCase))
                            {
                                await commerceContext.AddMessage(errorCodes.ValidationError, "InvalidOrMissingPropertyValue", new object[1]
                                {
                                "Language"
                                }, "Invalid or missing value for property 'Language'.");
                                flag = true;
                            }
                            else
                            {
                                ViewProperty viewProperty = item.Properties.FirstOrDefault((ViewProperty p) => p.Name.Equals("LocalizedValue", StringComparison.OrdinalIgnoreCase));
                                if (viewProperty == null)
                                {
                                    await commerceContext.AddMessage(errorCodes.ValidationError, "InvalidOrMissingPropertyValue", new object[1]
                                    {
                                    "LocalizedValue"
                                    }, "Invalid or missing value for property 'LocalizedValue'.");
                                    flag = true;
                                }
                                else
                                {
                                    string value = viewProperty.Value;
                                    if (string.IsNullOrEmpty(value) && text.Equals(defaultLanguage, StringComparison.OrdinalIgnoreCase))
                                    {
                                        await commerceContext.AddMessage(errorCodes.ValidationError, "InvalidOrMissingPropertyValueForLanguage", new object[2]
                                        {
                                        propertyName,
                                        defaultLanguage
                                        }, "Invalid or missing value for property '" + propertyName + "' for language '" + defaultLanguage + "'.");
                                        flag = true;
                                    }
                                    else
                                    {
                                        localizations.Add(new Parameter(text, value));
                                    }
                                }
                            }
                        }
                        if (!flag)
                        {
                            if (!localizations.Any())
                            {
                                await commerceContext.AddMessage(errorCodes.ValidationError, "PropertyLocalizationValuesNotFound", new object[1]
                                {
                                propertyName
                                }, "Localization values for property '" + propertyName + "' were not found.");
                            }
                            else
                            {
                                bool flag2 = !string.IsNullOrEmpty(entityView.ItemId);
                                LocalizeEntityComponentPolicy localizeEntityComponentPolicy = flag2 ? policy.GetItemComponentPolicyByView(entityView.Name) : policy.GetComponentPolicyByView(entityView.Name);
                                if (policy.Properties.Any((string p) => p.Equals(propertyName, StringComparison.OrdinalIgnoreCase)) && policy.ActionView.Equals(entityView.Name, StringComparison.OrdinalIgnoreCase) && !flag2)
                                {
                                    localizationEntity.AddOrUpdatePropertyValue(propertyName, localizations);
                                }
                                else if (localizeEntityComponentPolicy != null)
                                {
                                    string componentId = localizationEntity.GetComponentIdSupport(entityView.ItemId, entityView.Name, localizeEntityComponentPolicy, entity.Components, commerceContext.Logger);
                                    localizationEntity.AddOrUpdateComponentValue(localizeEntityComponentPolicy.Path, componentId, propertyName, localizations);
                                }
                                CommercePipelineExecutionContextOptions context = commerceContext.GetPipelineContextOptions();
                                await _persistPipeline.Run(new PersistEntityArgument(localizationEntity), context);
                                if (!entity.HasComponent<LocalizedEntityComponent>())
                                {
                                    entity.SetComponent(new LocalizedEntityComponent(localizationEntity.Id, localizationEntity.Name));
                                    await _persistPipeline.Run(new PersistEntityArgument(entity), context);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

}
