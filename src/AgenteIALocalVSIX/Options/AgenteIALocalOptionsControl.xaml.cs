using System.Windows.Controls;

namespace AgenteIALocalVSIX.Options
{
    public partial class AgenteIALocalOptionsControl : UserControl
    {
        public AgenteIALocalOptionsControl(object dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }
    }
}
