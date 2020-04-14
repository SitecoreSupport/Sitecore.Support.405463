namespace Sitecore.Support.Pipelines.Blocks
{
    // Sitecore.Commerce.Plugin.BusinessUsers.DoActionLocalizePropertyBlock
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.EntityViews;
    using Sitecore.Commerce.Plugin.BusinessUsers;
    using Sitecore.Commerce.Plugin.Shops;
    using Sitecore.Framework.Pipelines;
    using Sitecore.Support.Commands;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [PipelineDisplayName("BusinessUsers.Block.DoActionLocalizeProperty")]
    public class DoActionLocalizePropertyBlock : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        protected LocalizeEntityPropertyCommandSupport LocalizePropertyCommand
        {
            get;
            set;
        }

        protected CommerceEntity Entity
        {
            get;
            set;
        }

        protected ViewProperty Property
        {
            get;
            set;
        }

        protected LocalizeEntityPolicy Policy
        {
            get;
            set;
        }

        protected List<string> Languages
        {
            get;
            set;
        }

        public DoActionLocalizePropertyBlock(LocalizeEntityPropertyCommandSupport localizePropertyEntityPropertyCommand)
            : base((string)null)
        {
            LocalizePropertyCommand = localizePropertyEntityPropertyCommand;
        }

        public override async Task<EntityView> Run(EntityView entityView, CommercePipelineExecutionContext context)
        {
            if (string.IsNullOrEmpty(entityView?.Action) || !entityView.Action.Equals(context.GetPolicy<KnownBusinessUsersActionsPolicy>().LocalizeProperty, StringComparison.OrdinalIgnoreCase))
            {
                return entityView;
            }
            if (!(await Validate(entityView, context)))
            {
                return entityView;
            }
            await LocalizePropertyCommand.Process(context.CommerceContext, Entity, entityView, Policy, Property.Value, Languages);
            return entityView;
        }

        protected virtual async Task<bool> Validate(EntityView entityView, CommercePipelineExecutionContext context)
        {
            if (entityView == null)
            {
                return false;
            }
            Entity = context.CommerceContext.GetObjects<CommerceEntity>().FirstOrDefault((CommerceEntity p) => p.Id.Equals(entityView.EntityId, StringComparison.OrdinalIgnoreCase));
            if (Entity == null)
            {
                return false;
            }
            KnownResultCodes policy = context.GetPolicy<KnownResultCodes>();
            Property = entityView.Properties.FirstOrDefault((ViewProperty p) => p.Name.Equals("LocalizableProperty", StringComparison.OrdinalIgnoreCase));
            string propertyName = Property?.Value;
            if (string.IsNullOrEmpty(propertyName))
            {
                string text = (Property == null) ? "LocalizableProperty" : Property.DisplayName;
                await context.CommerceContext.AddMessage(policy.ValidationError, "InvalidOrMissingPropertyValue", new object[1]
                {
                text
                }, "Invalid or missing value for property 'LocalizableProperty'.");
                return false;
            }
            Type type = Entity.GetType();
            Policy = LocalizeEntityPolicy.GetPolicyByType(context.CommerceContext, type);
            if (Policy == null)
            {
                await context.CommerceContext.AddMessage(policy.ValidationError, "LocalizedEntityPolicyNotFound", new object[1]
                {
                type.FullName
                }, "A LocalizedEntityPolicy for type '" + type.FullName + "' was not found.");
                return false;
            }
            List<string> allProperties = Policy.ActionView.Equals(entityView.Name, StringComparison.OrdinalIgnoreCase) ? Policy.Properties.ToList() : new List<string>();
            Policy.ComponentsPolicies.Where((LocalizeEntityComponentPolicy c) => c.ActionView.Equals(entityView.Name, StringComparison.OrdinalIgnoreCase)).ForEach(delegate (LocalizeEntityComponentPolicy c)
            {
                allProperties.AddRange(c.Properties);
            });
            if (!allProperties.Any((string p) => p.Equals(propertyName, StringComparison.OrdinalIgnoreCase)))
            {
                await context.CommerceContext.AddMessage(policy.ValidationError, "InvalidOrMissingPropertyValue", new object[1]
                {
                Property.DisplayName
                }, "Invalid or missing value for property '" + Property.Name + "'.");
                return false;
            }
            Shop shop = context.CommerceContext.GetObjects<Shop>().FirstOrDefault();
            if (shop == null || !shop.Languages.Any())
            {
                return false;
            }
            Languages = shop.Languages;
            return true;
        }
    }

}
