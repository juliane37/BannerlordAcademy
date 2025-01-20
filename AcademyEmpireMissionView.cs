using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;


namespace Academy
{
    [DefaultView]
    public class AcademyEmpireMissionView : MissionView
    {
        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            

            if (Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Q))
            {
                doSomething();
            }
        }

        private void doSomething()
        {
            InformationManager.DisplayMessage(new InformationMessage("Hello world"));
            // This is just a placeholder function
            // Check mission mode is a battle or stealth
            if (!(Mission.Mode is MissionMode.Battle or MissionMode.Stealth)) return;

            // Check we have a masters robe
            var itemRoster = MobileParty.MainParty.ItemRoster;
            var masterRobe = MBObjectManager.Instance.GetObject<ItemObject>("master_robe");
            if (itemRoster.GetItemNumber(masterRobe) <= 0) return; // End if no robe

            // If succesful then remove the robe
            itemRoster.AddToCounts(masterRobe, -1);

            // Print a thank you message for funsies and add some health
            var oldHealth = Mission.MainAgent.Health;
            if (Mission.MainAgent.Health + 20 > Mission.MainAgent.HealthLimit)
            {
                Mission.MainAgent.Health = Mission.MainAgent.HealthLimit;
            } else
            {
                Mission.MainAgent.Health += 20;
            }
            Mission.MainAgent.Health += 20;
            InformationManager.DisplayMessage(new InformationMessage(String.Format("Thanks for returning the robe. Here is {0} health!", Mission.MainAgent.Health - oldHealth)));

        }
    }
}