// © 2017 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;

namespace Sitecore.Support
{
    public static class LocalizationEntityExtensions
    {
        public static string GetComponentIdSupport(this LocalizationEntity localizationEntity, string itemId, string viewName, LocalizeEntityComponentPolicy componentPolicy, IList<Component> components, ILogger logger)
        {
            var componentId = string.Empty;
            var component = componentPolicy.IsItemComponent
                ? localizationEntity.GetComponentByIdSupport(itemId, components, logger)
                : components.FirstOrDefault(x => x.GetType().Name.Equals(componentPolicy.Path, StringComparison.OrdinalIgnoreCase));

            if (component == null)
            {
                return componentId;
            }

            if (componentPolicy.Path.EndsWith(component.GetType().Name, StringComparison.OrdinalIgnoreCase))
            {
                componentId = component.Id;
            }
            else
            {
                foreach (Component childComponent in component.ChildComponents.Where(childComponent => componentPolicy.Path.EndsWith(childComponent.GetType().Name, StringComparison.OrdinalIgnoreCase) && componentPolicy.ActionView.Equals(viewName, StringComparison.OrdinalIgnoreCase)))
                {
                    componentId = childComponent.Id;
                    break;
                }
            }

            return componentId;
        }

        public static Component GetComponentByIdSupport(this LocalizationEntity localizationEntity, string componentId, IList<Component> components, ILogger logger)
        {
            if (string.IsNullOrEmpty(componentId) || components == null || !components.Any())
            {
                return null;
            }

            var matchingComponent = components.FirstOrDefault(c => c.Id.Equals(componentId, StringComparison.OrdinalIgnoreCase));
            if (matchingComponent != null)
            {
                return matchingComponent;
            }

            foreach (var childComponent in components.Where(c => c.ChildComponents.Any()))
            {
                matchingComponent = localizationEntity.GetComponentByIdSupport(componentId, childComponent.ChildComponents, logger);

                // Fix 405463
                if (matchingComponent != null) break;
                //
            }

            return matchingComponent;
        }
    }


}

