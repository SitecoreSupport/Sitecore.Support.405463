namespace Sitecore.Support.Pipelines.Blocks
{
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.EntityViews;
    using Sitecore.Commerce.Plugin.BusinessUsers;
    using Sitecore.Framework.Pipelines;
    using Sitecore.Support.Commands;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [PipelineDisplayName("BusinessUsers.Block.DoActionSelectLocalizableProperty")]
    public class DoActionSelectLocalizablePropertyBlock : DoActionLocalizePropertyBlock
    {
        public DoActionSelectLocalizablePropertyBlock(LocalizeEntityPropertyCommandSupport localizeEntityPropertyCommand)
            : base(localizeEntityPropertyCommand)
        {
            base.LocalizePropertyCommand = localizeEntityPropertyCommand;
        }

        public override async Task<EntityView> Run(EntityView entityView, CommercePipelineExecutionContext context)
        {
            if (string.IsNullOrEmpty(entityView?.Action) || !entityView.Action.Equals(context.GetPolicy<KnownBusinessUsersActionsPolicy>().SelectLocalizableProperty, StringComparison.OrdinalIgnoreCase))
            {
                return entityView;
            }
            if (!(await Validate(entityView, context)))
            {
                return entityView;
            }
            bool isItemView = !string.IsNullOrEmpty(entityView.ItemId);
            List<string> list = isItemView ? base.Policy.GetItemComponentPolicyByView(entityView.Name)?.Properties.ToList() : (base.Policy.ActionView.Equals(entityView.Name, StringComparison.OrdinalIgnoreCase) ? base.Policy.Properties.ToList() : base.Policy.GetComponentPolicyByView(entityView.Name)?.Properties.ToList());
            base.Property.IsReadOnly = true;
            base.Property.Policies = new List<Policy>
        {
            new AvailableSelectionsPolicy
            {
                List = list?.Select((string p) => new Selection
                {
                    DisplayName = p,
                    Name = p
                }).ToList()
            }
        };
            List<Parameter> localizedValues = new List<Parameter>();
            if (base.Entity.HasComponent<LocalizedEntityComponent>())
            {
                LocalizationEntity localizationEntity = await base.LocalizePropertyCommand.GetLocalizationEntity(context.CommerceContext, base.Entity);
                if (localizationEntity != null)
                {
                    if (!isItemView)
                    {
                        if (localizationEntity.ContainsProperty(base.Property.Value) && base.Policy.ActionView.Equals(entityView.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            localizedValues = localizationEntity.GetPropertyValues(base.Property.Value);
                        }
                        else
                        {
                            LocalizeEntityComponentPolicy componentPolicyByView = base.Policy.GetComponentPolicyByView(entityView.Name);
                            if (componentPolicyByView != null)
                            {
                                string componentId = localizationEntity.GetComponentIdSupport(entityView.ItemId, entityView.Name, componentPolicyByView, base.Entity.Components, context.Logger);
                                localizedValues = localizationEntity.GetComponentPropertyValues(componentPolicyByView.Path, componentId, base.Property.Value);
                            }
                        }
                    }
                    else
                    {
                        LocalizeEntityComponentPolicy itemComponentPolicyByView = base.Policy.GetItemComponentPolicyByView(entityView.Name);
                        if (itemComponentPolicyByView != null)
                        {
                            string componentId2 = localizationEntity.GetComponentIdSupport(entityView.ItemId, entityView.Name, itemComponentPolicyByView, base.Entity.Components, context.Logger);
                            localizedValues = localizationEntity.GetComponentPropertyValues(itemComponentPolicyByView.Path, componentId2, base.Property.Value);
                        }
                    }
                }
            }
            base.Languages.ForEach(delegate (string l)
            {
                EntityView entityView2 = new EntityView
                {
                    Name = context.GetPolicy<KnownBusinessUsersViewsPolicy>().LocalizedValue,
                    EntityId = base.Entity.Id
                };
                entityView2.Properties.Add(new ViewProperty
                {
                    Name = "Language",
                    RawValue = l,
                    IsReadOnly = true,
                    IsRequired = false,
                    IsHidden = false
                });
                entityView2.Properties.Add(new ViewProperty
                {
                    Name = "LocalizedValue",
                    RawValue = (localizedValues.FirstOrDefault((Parameter v) => v.Key.Equals(l, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty),
                    IsRequired = false
                });
                entityView.ChildViews.Add(entityView2);
            });
            entityView.UiHint = "Grid";
            context.CommerceContext.AddModel(new MultiStepActionModel(context.GetPolicy<KnownBusinessUsersActionsPolicy>().LocalizeProperty));
            return entityView;
        }
    }

}
