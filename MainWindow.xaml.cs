namespace OMineGuard
{
    public partial class MainWindow : System.Windows.Window
    {
        public const string Ver = "2.1";

        public MainWindow()
        {
            InitializeComponent();
            this.Title = $"OMineGuard v.{Ver}";

            Presenter.Content = new OMineGuardControlLibrary.View(
                new Backend.MainModel());
        }
    }
}
