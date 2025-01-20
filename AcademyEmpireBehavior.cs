using System;
using System.Collections.Generic;
using System.Security;
using SandBox.View.Conversation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static TaleWorlds.CampaignSystem.CampaignBehaviors.LordConversationsCampaignBehavior;


namespace Academy
{
    public class AcademyEmpireBehavior : CampaignBehaviorBase
    {
        
        public override void RegisterEvents()
        {
            // CampaignEvents.OnWorkshopOwnerChanged.AddNonSerializedListener(this, OnWorkshopOwnerChanged);
            CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, DailyTickTownEvent);
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }
        CharacterObject academyMaster;

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            this.AddDialogs(starter);
            academyMaster = MBObjectManager.Instance.GetObject<CharacterObject>("academy_master");
        }

        TextObject textObject = new TextObject("Upgrade cost: {TOTAL_GOLD}.", null);

        private void AddDialogs(CampaignGameStarter starter)
        {
            // Tavernkeeper lines 
            {
                starter.AddPlayerLine("tavernkeeper_talk_ask_academy_training", "tavernkeeper_talk", "tavernkeeper_academy_training",
                "Do you know where I can get training for myself or my troops?", null, null);

                starter.AddDialogLine("tavernkeeper_talk_academy_training_a", "tavernkeeper_academy_training", "tavernkeeper_talk",
                    "Ah, you have heard about our famous academy. You should seek out the master in town for more information.",
                    () => (Settlement.CurrentSettlement.Town.StringId == "town_comp_B1"), null);

                starter.AddDialogLine("tavernkeeper_talk_academy_training_b", "tavernkeeper_academy_training", "tavernkeeper_talk",
                    "You need to seek out the academy in Marunath. They will help you get the training you need.",
                    () => (!(Settlement.CurrentSettlement.Town.StringId == "town_comp_B1")), null);

            }




            // Academy Master
            {
                // Master: "Welcome. Are you hear to learn more about our academy?"
                starter.AddDialogLine("academy_master_intro", "start", "academy_master_introduction",
                    "Welcome. Are you hear to learn more about our academy?",
                    () => CharacterObject.OneToOneConversationCharacter == academyMaster && 1 == 1, null);

                // Player: Option A "Yes, I am looking for a way to train my troops, can you help?"
                starter.AddPlayerLine("academy_master_player_ask", "academy_master_introduction", "academy_master_training_request",
                    "Yes, I am looking for a way to train my troops, can you help?",
                    null, () =>
                    {
                        customPriceOffer(starter);
                    });


                // Player: Option B "No, thank you, I don't have any troops in need of training."
                starter.AddPlayerLine("academy_master_player_ask", "academy_master_introduction", "academy_master_training_not_request",
                    "No, thank you, I don't have any troops in need of training.",
                    null, null);
                // Master: Option B response "Suit yourself."
                starter.AddDialogLine("academy_master_talk_02", "academy_master_training_not_request", "end",
                    "Very well, seek me out if your troops ever need training.", null, null);

                // Player: Accept the offer of training
                starter.AddPlayerLine("academy_master_training", "academy_master_training_offer", "academy_master_training_purchased",
                    "It is worth the cost. I must have only the best in my ranks!", null, () =>
                    {
                        // Get the training
                        troopsUpgrade(getUpgradeCost());

                    }, 100, (out TextObject explanation) =>
                    {
                        if (Hero.MainHero.Gold < getUpgradeCost())
                        {
                            explanation = new TextObject("Not enough money.");
                            return false;
                        }
                        else
                        {
                            explanation = TextObject.Empty;
                            return true;
                        }
                    });

                starter.AddDialogLine("academy_master_thanks_for_business", "academy_master_training_purchased", "end",
                    "Thank you for your business!", null, null);

                starter.AddPlayerLine("academy_master_training_refused", "academy_master_training_offer", "academy_master_training_declined",
                    "No thank you.", null, null);

                starter.AddDialogLine("academy_master_your_loss", "academy_master_training_declined", "end",
                    "Your loss", null, null);
            }

        }

        int customOfferCounter = 0;
        private void customPriceOffer(CampaignGameStarter starter)
        {
            customOfferCounter++;

            string convoID = "academy_master_training_" + customOfferCounter;
            string requestID = "academy_master_training_request";
            string offerID = "academy_master_training_offer";

            TextObject textObject = new TextObject("I can train your troops, but it's not free. Based on the quality and number of troops, it will cost you {AMOUNT}.");
            textObject.SetTextVariable("AMOUNT", getUpgradeCost().ToString());

            starter.AddDialogLine(
                    convoID, // ID of the dialog
                    requestID, // Input state
                    offerID, // Output state
                    textObject.ToString(),
                    () => CharacterObject.OneToOneConversationCharacter == academyMaster && getUpgradeCost() > 0 && ("academy_master_training_" + customOfferCounter) == convoID, // Condition for this dialogue
                    null);
            
        }

        int offeredUpgradeCost = 0;
        private int getUpgradeCost()
        {
            var partyRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();
            int totalUpgradeCost = 0;

            for (int i = 0; i < partyRoster.Count; i++)
            {
                var soldier = partyRoster[i];

                if (!(soldier.Character.IsHero))
                {
                    //InformationManager.DisplayMessage(new InformationMessage(
                        //String.Format("Soldier: {0}, Tier: {1}, Level {2}", soldier.Character.Name, soldier.Character.Tier, soldier.Character.Level)));
                    // Non hero soldier slot
                    
                    var numberOfSoldiers = soldier.Number;
                    var costToUpgrade = (100 + (75 * soldier.Character.Tier)) * numberOfSoldiers;
                    totalUpgradeCost += costToUpgrade;
                }
            }
            offeredUpgradeCost = totalUpgradeCost;
            textObject.SetTextVariable("TOTAL_GOLD", totalUpgradeCost);
            
            return totalUpgradeCost;
        }
        

        private void troopsUpgrade(int upgradeCost)
        {
            var partyRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();

            for (int i = 0; i < partyRoster.Count; i++)
            {
                var soldier = partyRoster[i];

                if (!(soldier.Character.IsHero))
                {
                    var numberOfSoldiers = soldier.Number;

                    var xpToLevel = soldier.Character.GetUpgradeXpCost(MobileParty.MainParty.Party, i);
                    MobileParty.MainParty.MemberRoster.AddXpToTroop(xpToLevel * numberOfSoldiers, soldier.Character);
                }
            }

            // Pay for the training, according to offered rate
            Hero.MainHero.ChangeHeroGold(-upgradeCost);
        }

        private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
        {
            Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center");
            if (!(CampaignMission.Current.Location == locationWithId && CampaignTime.Now.IsDayTime)) return;

            // Spawn the worker
            Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
            Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(academyMaster.Race, "_settlement");
            int minValue;
            int maxValue;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(academyMaster, out minValue, out maxValue, "");

            foreach (Workshop workshop in settlement.Town.Workshops)
            {
                int num;
                unusedUsablePointCount.TryGetValue(workshop.Tag, out num);
                if (num > 0f)
                {
                        LocationCharacter locationCharacter = new LocationCharacter(new AgentData(
                            new SimpleAgentOrigin(academyMaster)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue)), 
                            new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), 
                            workshop.Tag, true, LocationCharacter.CharacterRelations.Neutral, null, true, false, null, false, false, true);
                        locationWithId.AddCharacter(locationCharacter);
                }
            }
        }

        private void DailyTickTownEvent(Town town)
        {
            foreach (var workshop in town.Workshops)
            {
                InformationManager.DisplayMessage(new InformationMessage(String.Format("{0} has a workshop {1}", town.Name, workshop.Name)));
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}