using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.OsTrader.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OsEngine.Robots.Engines
{
  
    public class NewEngineSerg : BotPanel
    {
        public NewEngineSerg(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            // Create tabs
            TabCreate(BotTabType.Simple);

            Description = OsLocalization.Description.DescriptionLabel28;
        }

        // The name of the robot in OsEngine
        public override string GetNameStrategyType()
        {
            return "NewEngineSerg";
        }

        // Show settings GUI
        public override void ShowIndividualSettingsDialog()
        {
            MessageBox.Show(OsLocalization.Trader.Label57);
        }
    }
}
