namespace MedSestriManipulations
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Shell.SetNavBarIsVisible(this, false); // ⬅️ Това скрива горната лента навсякъде
            Shell.SetTabBarIsVisible(this, false);
        }

    }
}
