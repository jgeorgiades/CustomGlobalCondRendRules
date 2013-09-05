 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Rules;
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.Layouts;
using Sitecore.Data.Items;
using Sitecore.Data;

namespace CustomGlobalConditionalRenderingRules
{
    public class CustomEvaluateCondition : Sitecore.Pipelines.InsertRenderings.Processors.EvaluateConditions
    {
        protected override void Evaluate(Sitecore.Pipelines.InsertRenderings.InsertRenderingsArgs args, Sitecore.Data.Items.Item item)
        {
            RuleList<ConditionalRenderingsRuleContext> globalRules = this.GetGlobalRules(item);
            foreach (RenderingReference reference in new List<RenderingReference>((IEnumerable<RenderingReference>)args.Renderings))
            {
                string conditions = reference.Settings.Conditions;
                if (!string.IsNullOrEmpty(conditions))
                {
                    List<Item> conditionItems = this.GetConditionItems(item.Database, conditions);
                    if (conditionItems.Count > 0)
                    {
                        RuleList<ConditionalRenderingsRuleContext> rules = RuleFactory.GetRules<ConditionalRenderingsRuleContext>((IEnumerable<Item>)conditionItems, "Rule");
                        ConditionalRenderingsRuleContext renderingsRuleContext = new ConditionalRenderingsRuleContext(args.Renderings, reference);
                        renderingsRuleContext.Item = item;
                        ConditionalRenderingsRuleContext ruleContext = renderingsRuleContext;
                        rules.Run(ruleContext);
                    }
                }
                if (globalRules != null)
                {
                    ConditionalRenderingsRuleContext renderingsRuleContext = new ConditionalRenderingsRuleContext(args.Renderings, reference);
                    renderingsRuleContext.Item = item;
                    ConditionalRenderingsRuleContext ruleContext = renderingsRuleContext;
                    globalRules.Run(ruleContext);
                }

                GetCustomRules(args, item, reference);
            }
        }

        private static void GetCustomRules(Sitecore.Pipelines.InsertRenderings.InsertRenderingsArgs args, Sitecore.Data.Items.Item item, RenderingReference reference)
        {
            string configId = Sitecore.Configuration.Settings.GetSetting("customRulesConfigFolder");
            if (string.IsNullOrEmpty(configId))
            {
                Error("Sitecore setting 'customRulesConfigFolder' not found", args.ContextItem);
                return;
            }
            Database db = Sitecore.Context.ContentDatabase ?? Sitecore.Context.Database;
            Item configFolder = db.GetItem(configId);
            if (configFolder == null)
            {
                Error(string.Format("Config folder {0} not found", configId), args.ContextItem);
                return;
            }

            if (configFolder.HasChildren && configFolder.Children.Any(r => !string.IsNullOrEmpty(r["rendering"]) && new ID(r["rendering"]) == reference.RenderingID))
            {
                Item configItem = configFolder.Children.First(r => new ID(r["rendering"]) == reference.RenderingID);
                Item datasourceItem = db.GetItem(configItem["item"]);
                if (datasourceItem == null)
                {
                    Error(string.Format("No Item containing rules set on config item at {0}", configItem.Paths.FullPath), args.ContextItem);
                    return;
                }
                string fieldName = configItem["rule field"];
                RunPromoRule(args, item, reference, datasourceItem, fieldName);
            }
        }

        private static void Error(string message, object owner)
        {
            Sitecore.Diagnostics.Log.Info(string.Format("Error evaluating custom conditional renderings: {0}", message), owner); 
        }

        private static void RunPromoRule(Sitecore.Pipelines.InsertRenderings.InsertRenderingsArgs args, Sitecore.Data.Items.Item item, RenderingReference reference, Item promo, string fieldName)
        {
            if (promo != null && !string.IsNullOrEmpty(fieldName))
            {
                RuleList<ConditionalRenderingsRuleContext> rules = RuleFactory.GetRules<ConditionalRenderingsRuleContext>(promo.Fields[fieldName]);
                var ruleContext = new ConditionalRenderingsRuleContext(args.Renderings, reference);
                ruleContext.Item = item;
                rules.Run(ruleContext);
            }
        }
    }
}